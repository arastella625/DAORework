using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace DAORework.FaultInjection.Tests.Gate;

/// <summary>
/// The starting point of every run: a documents folder with the user's own AddIns.xml entry and override file, and
/// mods installed by hand for adoption (an add-in, two override folders sharing a file name, loose override files); an
/// install folder holding a made-up 32-bit exe, three made-up mods imported into the library, and a profile. Nothing
/// of the game's is used, and the same bytes are written every time, so every run starts from the same state.
/// </summary>
internal static partial class GateFixtures
{
    public const string Profile = "Gate";

    private static readonly DateTimeOffset _entryTime = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>Writes the fixtures and imports the mods; returns each mod's library id by its placeholder ("{addin}").</summary>
    public static IReadOnlyDictionary<string, string> Prepare(GateFolders folders)
    {
        Write(Path.Combine(folders.Documents, "Settings", "AddIns.xml"), Encoding.UTF8.GetBytes(
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<AddInsList>\r\n  <AddInItem UID=\"users_own\" />\r\n"
            + "  <AddInItem UID=\"hand_addin\" Name=\"Hand AddIn\" Enabled=\"1\"><Title DefaultText=\"Hand AddIn\" /></AddInItem>\r\n</AddInsList>"));
        Write(Path.Combine(folders.Documents, "packages", "core", "override", "My Mods", "mine.dds"), Encoding.UTF8.GetBytes("the user's own"));
        WriteHandInstalled(folders.Documents);
        Write(Path.Combine(folders.Install, "bin_ship", "daorigins.exe"), Executable());
        Write(Path.Combine(folders.Downloads, "addin.dazip"), Zip(
            ("Manifest.xml", Encoding.UTF8.GetBytes("<Manifest Type=\"AddIn\"><AddInsList><AddInItem UID=\"gate_addin\" Name=\"Gate AddIn\" /></AddInsList></Manifest>")),
            ("Contents/addins/gate_addin/a0.gda", Content(1, 70_000)),
            ("Contents/addins/gate_addin/a1.gda", Content(2, 70_000)),
            ("Contents/addins/gate_addin/a2.gda", Content(3, 1_000))));
        Write(Path.Combine(folders.Downloads, "Gate Textures.zip"), Zip(("textures/t0.dds", Content(4, 90_000)), ("textures/t1.dds", Content(5, 2_000))));
        Write(Path.Combine(folders.Downloads, "Gate Textures HD.zip"), Zip(("textures/t0.dds", Content(6, 120_000))));
        foreach (var version in new[] { "2.2", "2.3" })
        {
            Write(Path.Combine(folders.Downloads, $"dxvk-{version}.zip"), Zip(
                ($"dxvk-{version}/x64/d3d9.dll", DxvkDll(version, machine: 0x8664)),
                ($"dxvk-{version}/x32/d3d9.dll", DxvkDll(version, machine: 0x014C))));
        }

        Cli(folders, "init", Profile);
        Cli(folders, "profile", "set-path", Profile, "documents", folders.Documents);
        Cli(folders, "profile", "set-path", Profile, "install", folders.Install);
        return new Dictionary<string, string>
        {
            ["{addin}"] = Import(folders, "addin.dazip"),
            ["{override}"] = Import(folders, "Gate Textures.zip"),
            ["{override-hd}"] = Import(folders, "Gate Textures HD.zip"),
            ["{dxvk-2.2}"] = Path.Combine(folders.Downloads, "dxvk-2.2.zip"),
            ["{dxvk-2.3}"] = Path.Combine(folders.Downloads, "dxvk-2.3.zip"),
        };
    }

    public static string[] Resolve(IEnumerable<string> arguments, IReadOnlyDictionary<string, string> ids) =>
        [.. arguments.Select(a => ids.TryGetValue(a, out var id) ? id : a)];

    public static CliOutcome Cli(GateFolders folders, params string[] arguments)
    {
        var outcome = InProcessCli.Run(folders.AppData, arguments);
        return outcome.ExitCode == 0 ? outcome : throw new InvalidOperationException($"Setting up the gate failed at '{string.Join(' ', arguments)}': {outcome.Output}");
    }

    [GeneratedRegex(@" as (?<id>[a-z0-9-]+), \d+ file")]
    private static partial Regex ImportedId();

    private static string Import(GateFolders folders, string download) =>
        ImportedId().Match(Cli(folders, "mods", "import", Path.Combine(folders.Downloads, download)).Output).Groups["id"].Value;

    /// <summary>Mods installed by hand, for the adoption scenarios (ADR-0015).</summary>
    private static void WriteHandInstalled(string documents)
    {
        var overrideFolder = Path.Combine(documents, "packages", "core", "override");
        Write(Path.Combine(documents, "AddIns", "hand_addin", "module", "h0.gda"), Content(10, 60_000));
        Write(Path.Combine(documents, "AddIns", "hand_addin", "module", "h1.gda"), Content(11, 1_000));
        Write(Path.Combine(overrideFolder, "Hand Hair", "textures", "hair.dds"), Content(12, 80_000));
        Write(Path.Combine(overrideFolder, "Hand Hair", "brow.dds"), Content(13, 2_000));
        Write(Path.Combine(overrideFolder, "Hand Hair Alt", "hair.dds"), Content(14, 50_000));
        Write(Path.Combine(overrideFolder, "loose0.uti"), Content(15, 3_000));
        Write(Path.Combine(overrideFolder, "loose1.uti"), Content(16, 500));
    }

    private static void Write(string path, byte[] content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, content);
    }

    /// <summary>The same made-up bytes for the same seed, so ids and hashes never change between runs.</summary>
    private static byte[] Content(int seed, int length)
    {
        var bytes = new byte[length];
        new Random(seed).NextBytes(bytes);
        return bytes;
    }

    private static byte[] Zip(params (string Name, byte[] Content)[] entries)
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in entries)
            {
                var entry = zip.CreateEntry(name);
                entry.LastWriteTime = _entryTime;
                using var stream = entry.Open();
                stream.Write(content);
            }
        }

        return buffer.ToArray();
    }

    /// <summary>
    /// A made-up d3d9.dll the catalog recognizes as DXVK: a PE header for <paramref name="machine"/>, filler, then
    /// DXVK's version marker as the real DLL carries it ("DXVK: ", a NUL, then "v2.3"). Nothing of DXVK's own code.
    /// </summary>
    private static byte[] DxvkDll(string version, ushort machine)
    {
        var bytes = Content(version == "2.2" ? 8 : 9, 40_000);
        bytes[0] = (byte)'M';
        bytes[1] = (byte)'Z';
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x3C), 0x80);
        "PE\0\0"u8.CopyTo(bytes.AsSpan(0x80));
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0x84), machine);
        return [.. bytes, .. Encoding.ASCII.GetBytes($"\0DXVK: \0v{version}\0")];
    }

    /// <summary>A made-up 32-bit PE32 image the 4 GB patch accepts: headers, then filler; nothing of the game's.</summary>
    private static byte[] Executable()
    {
        var bytes = Content(7, 150_000);
        bytes[0] = (byte)'M';
        bytes[1] = (byte)'Z';
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x3C), 0x80);
        "PE\0\0"u8.CopyTo(bytes.AsSpan(0x80));
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0x84), 0x014C);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0x84 + 16), 224);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0x84 + 18), 0x0102);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0x98), 0x010B);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x98 + 92), 16);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(0x98 + 132), 0);
        return bytes;
    }
}
