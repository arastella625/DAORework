using System.IO.Abstractions.TestingHelpers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DAORework.Core.Common;
using DAORework.Core.Mods.Library;
using DAORework.Core.Mods.Safety;
using DAORework.Infrastructure.Persistence;

namespace DAORework.App.Tests;

/// <summary>
/// A made-up library the size and shape of a real one: 70 mods, over 8,000 files, with the names, Vortex markers, Mac
/// leftovers, readmes and shared textures a library moved over from Vortex really has. Two made-up mods hid the preview's
/// wall of text until a player with 70 mods saw it (REVIEWING.md), so every window change is checked against this.
/// None of the mods are real.
/// </summary>
internal static class RealScaleLibrary
{
    public const string BlockedProgram = "Upscaler Overlay";
    public const string BlockedRegistered = "Party Slots v2";
    public const string BlockedInTheWay = "Extra Dog Slot v1.41";
    public const string PartSameName = "Better Eyes Silver";
    public const string PartOfAModThatIsOn = "Leftover Sky";

    private const int MarkersPerMod = 14;
    private const int CopySets = 51;
    private const int TwiceCopied = 16;
    private const long CopySizeBytes = 94_000_000;

    /// <summary>Each file's text by its hash, so a change applied here finds what the library says it holds.</summary>
    private static readonly Dictionary<string, string> _texts = [];

    /// <summary>Gets the mods that are on before the player does anything, as in the library this is modelled on.</summary>
    public static IReadOnlyList<string> AlreadyOn { get; } = ["Item Pack", "Companion Gifts", "Dye Kit", "Tactics Slots", "Better Skies", "Forest Retexture", "Mage Staves"];

    /// <summary>Gets the mods whose preview stops until they are left out.</summary>
    public static IReadOnlyList<string> Blocked { get; } = [BlockedProgram, BlockedRegistered, BlockedInTheWay];

    public static IReadOnlyList<LibraryEntry> Entries { get; } = Build();

    /// <summary>Gets the same library with what a real one gathers: 139 entries, 51 sets, about 6 GB of extras, and two parts of mods.</summary>
    public static IReadOnlyList<LibraryEntry> WithCopies { get; } = [.. Entries, .. BuildCopies()];

    /// <summary>Writes every mod into the library, and what blocks the two add-ins into the game.</summary>
    public static void Seed(AppTestHost host, IReadOnlyList<LibraryEntry>? entries = null)
    {
        foreach (var entry in entries ?? Entries)
        {
            foreach (var file in entry.Mod.Files)
            {
                host.FileSystem.AddFile(Path.Combine(host.LibraryDirectory, entry.Mod.Id, file.RelativePath.Replace('/', Path.DirectorySeparatorChar)), new MockFileData(_texts[file.Sha256]));
            }

            host.FileSystem.AddFile(Path.Combine(host.LibraryDirectory, entry.Mod.Id + ".mod.json"), new MockFileData(JsonSerializer.Serialize(entry, JsonDefaults.Options)));
        }

        var documents = GamePaths.DefaultDocumentsDirectory(host.KnownFolders.MyDocuments);
        host.FileSystem.AddFile(GamePaths.AddInsListFile(documents), new MockFileData($"""<AddInsList><AddInItem UID="{Uid(BlockedRegistered)}"><Title DefaultText="Party Slots" /></AddInItem></AddInsList>"""));
        foreach (var name in new[] { "core/data/slots.erf", "module/data/slots_module.erf" })
        {
            host.FileSystem.AddFile(Path.Combine(GamePaths.AddInsDirectory(documents), Uid(BlockedInTheWay), name.Replace('/', Path.DirectorySeparatorChar)), new MockFileData("placed by hand"));
        }
    }

    private static List<LibraryEntry> Build()
    {
        var mods = new List<LibraryMod>();
        var pack = Override("Big Texture Pack").Looks("pack", 1240).Looks("pf_cth", 120).Looks("pf_arm", 240);
        mods.Add(pack.Build());
        mods.Add(Override("commoner--a little leg").Looks("pf_cth", 120).Looks("commoner", 20).Build());
        foreach (var (name, offset) in new[] { ("heavy armor a little leg", 0), ("medium armors--a little leg", 40), ("light armor-a little leg-mainv2", 80), ("female enchanter robes--little leg", 120), ("replacement apprentice robes a little leg", 160), ("mage_robes--a little leg", 200) })
        {
            mods.Add(Override(name).Looks("pf_arm", 40, offset).Looks("armor_" + offset, 60).Readme().Build());
        }

        foreach (var name in new[] { "Additional Hairs (Part 1)", "Additional Hairs (Part 2)", "Ren Hairstyles", "Ren Hairstyles2", "More_Hairstyles", "HAIRSTYLE PACK NEW", "HAIRSTYLE PACK Vol" })
        {
            mods.Add(Override(name).Looks("hf_har", 12).Looks(name.Replace(" ", string.Empty, StringComparison.Ordinal), 180).Readme().MacJunk().Build());
        }

        foreach (var name in new[] { "Haircolors", "colorful hair basic-reworked" })
        {
            mods.Add(Override(name).Same("t1_stb_bac.tnt", "t1_stb_cld.tnt", "t3_har_bac.tnt", "t3_har_cld.tnt").Own("list.txt").Looks(name[..4], 40).Readme().Build());
        }

        foreach (var name in new[] { "rules_core", "talent_singletarget" })
        {
            mods.Add(Override(name).Own("abi_base.gda", "ability_core.ncs").Looks(name[..5], 6).Build());
        }

        foreach (var name in new[] { "tsm_one_jowan", "Sleep_until_Dawn_bugfix2_2" })
        {
            mods.Add(Override(name).Own("gen00pt_party.ncs").Looks(name[..3], 8).Readme().Build());
        }

        var names = "Better Skies|Forest Retexture|Mage Staves|Clouds Remade|arr-presets|BDM|Better Eyes - Silver|Cosmetics Eyes v1|Cosmetics Presets v1|Cosmetics Skins OLD|CBG|celtic skin|Chargenmorphcfg|Companion Gifts|companion talk everytime|Faces by Someone|Fantasy|Fantasy Sun|Eyeshadow Collection|Eyes DAO|headdress and more ALL-IN-ONE|JB9|Lord Subtle|LOTC's Eye Textures|Makeup|Meine DAO Skins|My TINTS|NEW SKINS 1|NEW SKINS 2|NewTex by MB|next part|Fix for Oghren|Presets with Longer Lashes|Replace Default Colors|RINGMOD|Schwarzalbenheim|skin mask|Videos|ZDF|Better Lashes|Stud Armour".Split('|');
        foreach (var name in names)
        {
            mods.Add(Override(name).Looks(name.Replace(" ", string.Empty, StringComparison.Ordinal), 62).Own("zevran_kiss_dm_new.cut").Readme().Build());
        }

        mods.Add(Override("__MACOSX").Own("._.DS_Store", "._hair.dds", "._eye.dds", "._skin.dds", "._lash.dds", "._brow.dds").Build());
        mods.Add(Override("loose").Own("readme.txt", "for chargenmorphcfg.txt").Looks("loose", 28).Build());
        foreach (var name in new[] { "Item Pack", "Dye Kit", "Tactics Slots", BlockedRegistered, BlockedInTheWay })
        {
            mods.Add(AddIn(name));
        }

        mods.Add(new LibraryMod(ModIds.Create(BlockedProgram, Sha(BlockedProgram)), BlockedProgram, ModKind.GameProgram, [new LibraryModFile("bin_ship/dinput8.dll", 90_000, Sha("dll")) { Kind = ModFileKind.Program }]));
        return [.. mods.Select(mod => new LibraryEntry(mod, mod.Name + ".zip", Sha(mod.Name), new DateTimeOffset(2026, 11, 4, 12, 0, 0, TimeSpan.Zero)) { Scan = new MalwareScan(MalwareScanStatus.NoThreatFound, "No threats found.") })];
    }

    /// <summary>A copy of 51 mods, a second of 16, all older than the original, and two parts. The three add-ins' copies aren't the same.</summary>
    private static List<LibraryEntry> BuildCopies()
    {
        var addIns = Entries.Where(e => AlreadyOn.Contains(e.Mod.Name) && e.Mod.Kind == ModKind.AddIn);
        var overrides = Entries.Where(e => e.Mod.Kind == ModKind.Override).Take(CopySets - 3);
        var copies = new List<LibraryEntry>();
        foreach (var (entry, place) in addIns.Concat(overrides).Select((e, i) => (e, i)))
        {
            copies.Add(CopyOf(entry, 1));
            if (place < TwiceCopied)
            {
                copies.Add(CopyOf(entry, 2));
            }
        }

        copies.Add(PartOf(PartSameName, "Better Eyes - Silver"));
        copies.Add(PartOf(PartOfAModThatIsOn, "Better Skies"));
        return copies;
    }

    /// <summary>One file of a mod, as moving from Vortex can leave behind.</summary>
    private static LibraryEntry PartOf(string name, string whole)
    {
        var entry = Entries.Single(e => e.Mod.Name == whole);
        var file = entry.Mod.Files.First(f => f.RelativePath.EndsWith(".dds", StringComparison.Ordinal)) with { SizeBytes = 487 };
        return entry with
        {
            Mod = entry.Mod with { Id = ModIds.Create(name, Sha(name)), Name = name, Files = [file] },
            SourceName = name,
        };
    }

    private static LibraryEntry CopyOf(LibraryEntry entry, int number)
    {
        var name = entry.Mod.Name;
        var files = entry.Mod.Files.Select(f => f.RelativePath.EndsWith("/slots.erf", StringComparison.Ordinal) ? f with { Sha256 = Sha($"{name} older {f.RelativePath}") } : f).ToList();
        files[0] = files[0] with { SizeBytes = CopySizeBytes };
        return entry with
        {
            Mod = entry.Mod with { Id = ModIds.Create(name, Sha($"{name} copy {number}")), Files = files },
            SourceName = $"{name} ({number}).zip",
            ImportedAtUtc = entry.ImportedAtUtc.AddDays(-number),
        };
    }

    private static string Uid(string name) => name.Replace(" ", "_", StringComparison.Ordinal).Replace(".", "_", StringComparison.Ordinal).ToLowerInvariant();

    private static LibraryMod AddIn(string name)
    {
        var folder = $"Contents/addins/{Uid(name)}";
        var names = new[] { "Manifest.xml", $"{folder}/Manifest.xml", $"{folder}/core/data/slots.erf", $"{folder}/module/data/slots_module.erf" };
        var markers = Enumerable.Range(0, MarkersPerMod).Select(i => $"{folder}/{(i == 0 ? string.Empty : $"part{i}/")}__folder_managed_by_vortex");
        var manifest = $"""<AddInManifest><AddInsList><AddInItem UID="{Uid(name)}"><Title DefaultText="{name}" /></AddInItem></AddInsList></AddInManifest>""";
        var files = names.Concat(markers).Select(f => new LibraryModFile(f, 64, Sha(f.EndsWith("Manifest.xml", StringComparison.Ordinal) ? manifest : name + f)) { Kind = ModFileKind.Data }).ToList();
        return new LibraryMod(ModIds.Create(name, Sha(name)), name, ModKind.AddIn, files, Uid(name));
    }

    private static ModBuilder Override(string name) => new(name);

    private static string Sha(string text)
    {
        var sha = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
        _texts[sha] = text;
        return sha;
    }

    /// <summary>One override mod's files. Its name goes into each hash, so copies differ unless <see cref="Same"/> says otherwise.</summary>
    private sealed class ModBuilder(string name)
    {
        private readonly List<LibraryModFile> _files = [.. Enumerable.Range(0, MarkersPerMod).Select(i => File($"{(i == 0 ? string.Empty : $"folder{i}/")}__folder_managed_by_vortex", "marker"))];

        public ModBuilder Looks(string stem, int count, int from = 0)
        {
            _files.AddRange(Enumerable.Range(from, count).Select(i => File($"textures/{stem}_{i}_0d.dds", name)));
            return this;
        }

        public ModBuilder Own(params string[] files)
        {
            _files.AddRange(files.Select(f => File(f, name)));
            return this;
        }

        public ModBuilder Same(params string[] files)
        {
            _files.AddRange(files.Select(f => File(f, "same in each")));
            return this;
        }

        public ModBuilder Readme() => Own("readme.txt");

        public ModBuilder MacJunk()
        {
            _files.Add(File(".DS_Store", name));
            return this;
        }

        public LibraryMod Build() => new(ModIds.Create(name, Sha(name)), name, ModKind.Override, _files);

        private static LibraryModFile File(string path, string content) => new(path, 64, Sha(content + path)) { Kind = ModFileKind.Data };
    }
}
