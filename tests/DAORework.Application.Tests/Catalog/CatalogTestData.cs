using System.Buffers.Binary;
using System.Text;
using DAORework.Application.Tests.Fakes;
using DAORework.Core.Catalog;
using DAORework.Core.Diagnostics;
using DAORework.Core.Formats;
using DAORework.Core.Profiles;

namespace DAORework.Application.Tests.Catalog;

/// <summary>A test install and catalog entries shaped like the real ones in catalog/known-items.json.</summary>
internal static class CatalogTestData
{
    public static readonly string Install = TestPaths.Root("Games", "DAO");

    public static readonly string DocumentsDirectory = TestPaths.Root("Users", "amber", "Documents", "BioWare", "Dragon Age");

    public static readonly string D3D9 = Path.Combine(Install, "bin_ship", "d3d9.dll");

    public static readonly string GameExecutable = Path.Combine(Install, "bin_ship", "daorigins.exe");

    public static readonly KnownItem Dxvk = new(
        "dxvk",
        "DXVK",
        KnownItemKind.Fix,
        "https://github.com/doitsujin/dxvk",
        "zlib/libpng",
        [new KnownItemFingerprint(FingerprintKind.GameFile, "bin_ship/d3d9.dll", "DXVK: ")],
        "DXVK avoids the memory-related crashes.");

    public static readonly KnownItem Fixpack = Mod("fixpack", "Fixpack", FingerprintKind.AddInUid, "fixpack_uid");

    public static readonly KnownItem DialogPatch = Mod("dialog-patch", "Dialog Patch", FingerprintKind.OverrideFile, "dialog_patch.dlg");

    public static readonly KnownItem LargeAddressAware = Mod("laa", "4 GB patch", FingerprintKind.LargeAddressAware, "bin_ship/daorigins.exe");

    public static readonly KnownConflict FixpackVersusDialogPatch =
        new("fixpack", "dialog-patch", DiagnosticSeverity.Warning, "The patch undoes the fixpack's dialogue fixes.", "https://example.org/fixpack#compatibility");

    public static RuntimeProfile Profile => new()
    {
        InstallDirectory = Install,
        GameExecutablePath = GameExecutable,
        UserDocumentsDirectory = DocumentsDirectory,
    };

    public static KnownItem Mod(string id, string name, FingerprintKind kind, string value) =>
        new(id, name, KnownItemKind.Mod, $"https://example.org/{id}", "Author's permission", [new KnownItemFingerprint(kind, value)]);

    /// <summary>A PE file for <paramref name="machine"/> with DXVK's strings after the header, laid out as in the releases.</summary>
    public static byte[] DxvkDll(string version, ushort machine = PortableExecutableMachine.X86)
    {
        var strings = Encoding.ASCII.GetBytes($"\0Game: \0DXVK: \0{version}\0Build: \0");
        return [.. Pe(machine, characteristics: 0x2102), .. strings];
    }

    public static byte[] Pe(ushort machine, ushort characteristics)
    {
        var bytes = new byte[512];
        bytes[0] = (byte)'M';
        bytes[1] = (byte)'Z';
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0x3C), 0x80);
        "PE\0\0"u8.CopyTo(bytes.AsSpan(0x80));
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0x84), machine);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(0x80 + 22), characteristics);
        return bytes;
    }

    public static string AddInsXml(params (string Uid, bool Enabled)[] addIns) =>
        "<?xml version=\"1.0\" encoding=\"utf-8\"?><AddInsList>"
        + string.Concat(addIns.Select(a => $"<AddInItem UID=\"{a.Uid}\" Name=\"{a.Uid}\" Enabled=\"{(a.Enabled ? 1 : 0)}\" />"))
        + "</AddInsList>";
}
