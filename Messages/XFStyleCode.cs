using System.ComponentModel;
using System.Text;
using libMidi.Messages.attributes;

namespace libMidi.Messages;

[DisplayName("XF Style コード名")]
[XFStyle([0x43, 0x7b, 0x01, 0x00, 0x00, 0x00, 0x00])]
[XFStyleMask([0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00])]
public record XFStyleCode
    : XFStyle
{
    public XFStyleCode(byte[] data)
        : base(data)
    {
        Data = data;
    }

    public override string ToString()
    {
        return $"XFStyle Code {BitConverter.ToString(Data).Replace("-", " ")} {GetCodeName()} ({string.Join(", ", GetCodeVoicing().ToArray())})";
    }

    public List<byte> GetCodeVoicing()
    {
        List<byte> result = new();

        if (Data[4] == code.Count - 1)
        {
            return result;
        }

        if (Data[5] != 0x7f)
        {   // Base Note
            result.Add((byte)(GetPitch(Data[5]) - 12));
        }

        byte offset = GetPitch(Data[3]);
        foreach (var p in code.Values.ToList()[Data[4]])
        {
            result.Add((byte)(offset + p - 1));
        }

        return result;
    }

    private byte GetPitch(byte cnote)
    {
        byte fff = (byte)(cnote >> 4);
        byte nnnn = (byte)(cnote & 0x0F);

        int pitch = 60;

        pitch += fff - 3;
        pitch += note.Values.ToList()[nnnn];

        return (byte)pitch;
    }

    public string GetCodeName()
    {
        return $"{GetCodeName(Data[3], Data[4])}{(Data[5] != 0x7f ? "/" + GetCodeName(Data[5], Data[6]) : string.Empty)}";
    }

    private string GetCodeName(byte cnote, byte ctype)
    {
        if (ctype == code.Count - 1)
        {
            return "NoCode";
        }

        byte fff = (byte)(cnote >> 4);
        byte nnnn = (byte)(cnote & 0x0F);
        StringBuilder codename = new StringBuilder();

        if (cnote == 0x7f) return string.Empty;

        codename.Append(note.Keys.ToList()[nnnn]);
        codename.Append(sf[fff]);
        if (ctype != 0x00 && ctype != 0x7f) codename.Append(code.Keys.ToList()[ctype]);

        return codename.ToString();
    }

    private static List<string> sf =
    [
        "bbb",
        "bb",
        "b",
        "",
        "#",
        "##",
        "###",
    ];

    private static Dictionary<string, byte> note = new()
    {
        { "", 0 },
        { "C", 0 },
        { "D", 2 },
        { "E", 4 },
        { "F", 5 },
        { "G", 7 },
        { "A", 9 },
        { "B", 11 },
    };

    private Dictionary<string, List<byte>> code = new()
    {
        { "", [ 1, 5, 8 ] },
        { "6", [ 1, 5, 8, 10 ] },
        { "M7", [ 1, 5, 8, 12 ] },
        { "M7(#11)", [ 1, 5, 8, 12, 19 ] },
        { "add9", [ 1, 5, 8, 15 ] },
        { "M7(9)", [ 1, 5, 8, 12, 15 ] },
        { "6(9)", [ 1, 5, 8, 10, 15 ] },
        { "aug", [ 1, 5, 7 ] },
        { "m", [ 1, 4, 8 ] },
        { "m6", [ 1, 4, 8, 10 ] },
        { "m7", [ 1, 4, 8, 11 ] },
        { "m7(-5)", [ 1, 4, 7, 11 ] },
        { "m(9)", [ 1, 4, 8, 15 ] },
        { "m7(9)", [ 1, 4, 8, 11, 15 ] },
        { "m7(11)", [ 1, 4, 8, 11, 15, 18 ] },
        { "m Maj7", [ 1, 4, 8, 12 ] },
        { "m Maj7(9)", [ 1, 4, 8, 12, 15 ] },
        { "dim", [ 1, 4, 7 ] },
        { "dim7", [ 1, 4, 7, 11 ] },
        { "7", [ 1, 5, 8, 11 ] },
        { "7sus4", [ 1, 6, 8, 11 ] },
        { "7b5", [ 1, 5, 7, 11 ] },
        { "7(9)", [ 1, 5, 8, 11, 15 ] },
        { "7(#11)", [ 1, 5, 8, 11, 19 ] },
        { "7(13)", [ 1, 5, 8, 11, 22 ] },
        { "7(b9)", [ 1, 5, 8, 11, 14 ] },
        { "7(b13)", [ 1, 5, 8, 11, 21 ] },
        { "7(#9)", [ 1, 5, 8, 11, 16 ] },
        { "M7aug", [ 1, 5, 9, 12 ] },
        { "7aug", [ 1, 5, 9, 11 ] },
        { "1+8", [ 1, 15 ] },
        { "1+5", [ 1, 8 ] },
        { "sus4", [ 1, 6, 8 ] },
        { "1+2+5", [ 1, 3, 8 ] },
        { "cc", [  ] },
    };
}
