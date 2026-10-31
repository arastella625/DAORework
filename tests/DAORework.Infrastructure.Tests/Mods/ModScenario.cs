using System.IO.Abstractions.TestingHelpers;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using DAORework.Application.Abstractions;
using DAORework.Application.Common;
using DAORework.Application.Mods.Apply;
using DAORework.Application.Mods.Backups;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Planning;
using DAORework.Infrastructure.Mods;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace DAORework.Infrastructure.Tests.Mods;

/// <summary>
/// A documents folder with an earlier deployment (an old mod, a UI mod at v1), a user's own override file and add-in,
/// and a library holding the UI mod at v2, a hair mod and Qwinn's Fixpack. The plan switches to ui v2, hair and the
/// fixpack: a replace, three copies, a delete and an AddIns.xml edit. All real adapters over one mock file system.
/// </summary>
internal sealed class ModScenario
{
    public static readonly string AppData = MockUnixSupport.Path(@"C:\AppData\DAORework");
    public static readonly string Documents = MockUnixSupport.Path(@"C:\Users\Me\Documents\BioWare\Dragon Age");

    private const string Ui = "packages/core/override/DAORework/ui/ui.gda";
    private const string Old = "packages/core/override/DAORework/old/old.gda";
    private const string UserAddIns = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<AddInsList>\r\n  <AddInItem UID=\"users_own\" Name=\"Mine\" Enabled=\"1\" />\r\n</AddInsList>";
    private const string FixpackManifest = "<Manifest Type=\"AddIn\"><AddInsList><AddInItem UID=\"qwinn_fixpack_3\" Name=\"QwinnFixpack3\" Enabled=\"1\" /></AddInsList></Manifest>";

    public ModScenario()
    {
        var options = Options.Create(new DaoReworkOptions { AppDataDirectory = AppData });
        Records = new JsonDeploymentRecordStore(FileSystem, options);
        Journals = new FileApplyJournalStore(FileSystem, options);
        Writer = new DocumentsDeploymentWriter(FileSystem, options);
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero));
        ModLibrary = new FolderModLibrary(FileSystem, options, clock);
        Backups = new FolderDeploymentBackupStore(FileSystem, options, clock, ModLibrary);

        Write(Documents, Ui, "ui v1");
        Write(Documents, Old, "old mod");
        Write(Documents, "packages/core/override/My Mods/mine.dds", "the user's own");
        Write(Documents, "Settings/AddIns.xml", UserAddIns);
        Records.SaveAsync(Documents, new DeploymentRecord(["ui", "old"], [new RecordedFile(Ui, Sha("ui v1"), "ui"), new RecordedFile(Old, Sha("old mod"), "old")], [])).GetAwaiter().GetResult();

        Library("ui", "ui.gda", "ui v2");
        Library("hair", "textures/hair.dds", "hair");
        Library("hair", "eyes.dds", "eyes");
        Library("fixpack", "Manifest.xml", FixpackManifest);
        Library("fixpack", "Contents/addins/qwinn_fixpack_3/core/fix.ncs", "fix");
    }

    public MockFileSystem FileSystem { get; } = new();

    public JsonDeploymentRecordStore Records { get; }

    public FileApplyJournalStore Journals { get; }

    public DocumentsDeploymentWriter Writer { get; }

    public FolderDeploymentBackupStore Backups { get; }

    public FolderModLibrary ModLibrary { get; }

    public IReadOnlyList<LibraryMod> Mods { get; } =
    [
        new("ui", "UI", ModKind.Override, [File("ui.gda", "ui v2")]),
        new("hair", "Hair", ModKind.Override, [File("textures/hair.dds", "hair"), File("eyes.dds", "eyes")]),
        new("fixpack", "Fixpack", ModKind.AddIn, [File("Manifest.xml", FixpackManifest), File("Contents/addins/qwinn_fixpack_3/core/fix.ncs", "fix")], "qwinn_fixpack_3"),
    ];

    public static string Sha(string content) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(content)));

    public ApplyModPlan Apply(IDeploymentWriter writer) => new(new BackUpDeployment(Backups), Journals, new JournaledApplyRunner(writer, Journals, Records));

    public RecoverModPlan Recover() => new(Journals, new JournaledApplyRunner(Writer, Journals, Records), new RestoreDeployment(Backups), Records);

    /// <summary>Plans the switch from what is on disk now, as the activate command will.</summary>
    public async Task<ModActivationPlan> PlanAsync()
    {
        var record = (await Records.LoadAsync(Documents)).Value;
        var targets = Mods.SelectMany(m => ModLayout.Map(m).Value).Select(f => f.TargetPath).Concat(record.Files.Select(f => f.TargetPath));
        var disk = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var target in targets.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var path = Full(Documents, target);
            if (FileSystem.File.Exists(path))
            {
                disk[target] = Convert.ToHexStringLower(SHA256.HashData(FileSystem.File.ReadAllBytes(path)));
            }
        }

        var registered = XDocument.Parse(FileSystem.File.ReadAllText(Full(Documents, "Settings/AddIns.xml")))
            .Descendants("AddInItem").Select(e => (string)e.Attribute("UID")!).ToList();
        return ModActivationPlanner.Plan(new PlanInput(record, Mods, ["My Mods/mine.dds"], disk, registered)).Value;
    }

    /// <summary>Every file under the documents folder with its content, plus the deployment record's active mods.</summary>
    public async Task<List<string>> SnapshotAsync()
    {
        var files = FileSystem.Directory.EnumerateFiles(Documents, "*", SearchOption.AllDirectories)
            .Select(f => $"{Path.GetRelativePath(Documents, f).Replace('\\', '/')}={FileSystem.File.ReadAllText(f)}")
            .Order(StringComparer.Ordinal)
            .ToList();
        files.Add("record=" + string.Join(",", (await Records.LoadAsync(Documents)).Value.ActiveModIds));
        return files;
    }

    public void Write(string root, string relative, string content)
    {
        var path = Full(root, relative);
        FileSystem.Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        FileSystem.File.WriteAllText(path, content);
    }

    public string Full(string root, string relative) => Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));

    private static LibraryModFile File(string path, string content) => new(path, content.Length, Sha(content));

    private void Library(string modId, string relative, string content) => Write(Path.Combine(AppData, "library", modId), relative, content);
}
