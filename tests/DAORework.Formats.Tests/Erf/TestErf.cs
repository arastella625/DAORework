using System.Text;

namespace DAORework.Formats.Tests.Erf;

/// <summary>Builds small ERF archives from the specification, so no game file is ever needed.</summary>
internal static class TestErf
{
    public static byte[] Build(string version, uint flags = 0, params (string Name, byte[] Data)[] files)
    {
        var isV22 = version == "V2.2";
        var header = isV22 ? 56 : 32;
        var entry = isV22 ? 76 : 72;
        using var buffer = new MemoryStream();
        using var writer = new BinaryWriter(buffer);
        writer.Write(Encoding.Unicode.GetBytes("ERF " + version));
        writer.Write((uint)files.Length);
        writer.Write(109u);
        writer.Write(300u);
        writer.Write(uint.MaxValue);
        if (isV22)
        {
            writer.Write(flags);
            writer.Write(0u);
            writer.Write(new byte[16]);
        }

        var offset = (uint)(header + (entry * files.Length));
        foreach (var (name, data) in files)
        {
            var bytes = new byte[64];
            Encoding.Unicode.GetBytes(name).CopyTo(bytes, 0);
            writer.Write(bytes);
            writer.Write(offset);
            writer.Write((uint)data.Length);
            if (isV22)
            {
                writer.Write((uint)data.Length * 2);
            }

            offset += (uint)data.Length;
        }

        foreach (var (_, data) in files)
        {
            writer.Write(data);
        }

        return buffer.ToArray();
    }
}
