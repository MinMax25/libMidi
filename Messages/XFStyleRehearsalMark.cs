using System.ComponentModel;
using libMidi.Messages.attributes;

namespace libMidi.Messages;

[DisplayName("XF Style リハーサルマーク")]
[XFStyle([0x43, 0x7b, 0x02, 0x00])]
[XFStyleMask([0xff, 0xff, 0xff, 0x00])]
public record XFStyleRehearsalMark
    : XFStyle
{
    public XFStyleRehearsalMark(byte[] data)
        : base(data)
    {
        Data = data;
    }

    public string Section()
    {
        byte yyy = (byte)((Data[3] & 0x70) >> 4);
        byte xxxx = (byte)(Data[3] & 0x0F);

        return sect[xxxx].PadRight(yyy + 1, '\'');
    }

    public override string ToString()
    {
        return $"XFStyle Rehearsal Mark {BitConverter.ToString(Data).Replace("-", " ")} ({Section()})";
    }

    private static List<string> sect =
    [
        "Intro",
        "Ending",
        "Fill-in",
        "A",
        "B",
        "C",
        "D",
        "E",
        "F",
        "G",
        "H",
        "I",
        "J",
        "K",
        "L",
        "M",
    ];
}
