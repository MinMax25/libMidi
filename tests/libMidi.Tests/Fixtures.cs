using System.Buffers.Binary;
using System.Text;

namespace LibMidi.Tests;

internal static class Fixtures
{
    internal const string Conductor = "00 FF 51 03 07 A1 20 00 FF 58 04 04 02 18 08 00 FF 59 02 FF 00";
    internal const string Music = """
        00 FF 03 04 54 75 6E 65
        00 B0 00 01 00 B0 20 02 00 C0 05
        00 90 3C 64 7F 3E 50 81 00 80 3C 40
        00 A0 3E 31 00 D0 32 00 E0 00 41
        00 B0 0B 60 00 B0 40 7F
        00 F0 05 7E 7F 09 01 F7
        FF 7F 89 24 50 81 80 00 99 26 50
        """;
    internal const string End = "00 FF 2F 00";

    internal static byte[] Hex(string text) =>
        Convert.FromHexString(string.Concat(text.Where(c => !char.IsWhiteSpace(c))));

    internal static string Format0(string root) =>
        Write(root, "曲 入力.mid", 0, ("MTrk", $"{Conductor} {Music} {End}"));

    internal static string Format1(string root) =>
        Write(root, "format1.mid", 1, ("MTrk", $"{Conductor} {End}"), ("MTrk", $"{Music} {End}"));

    internal static string Write(string root, string name, byte format, params (string Id, string Hex)[] tracks)
    {
        string path = Path.Combine(root, name);
        using var stream = File.Create(path);
        stream.Write("MThd"u8);
        stream.Write([0, 0, 0, 6, 0, format, 0, checked((byte)tracks.Length), 1, 0xE0]);
        Span<byte> length = stackalloc byte[4];
        foreach (var track in tracks)
        {
            byte[] data = Hex(track.Hex);
            stream.Write(Encoding.ASCII.GetBytes(track.Id));
            BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
            stream.Write(length);
            stream.Write(data);
        }

        return path;
    }
}
