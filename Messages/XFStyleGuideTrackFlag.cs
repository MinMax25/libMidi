using System.ComponentModel;
using libMidi.Messages.attributes;

namespace libMidi.Messages;

[DisplayName("XF Style ガイドトラックフラグ")]
[XFStyle([0x43, 0x7b, 0x0c, 0x00, 0x00])]
[XFStyleMask([0xff, 0xff, 0xff, 0x00, 0x00])]
public record XFStyleGuideTrackFlag
    : XFStyle
{
    public XFStyleGuideTrackFlag(byte[] data)
        : base(data)
    {
        Data = data;
    }

    public byte Right => Data[3];

    public byte Left => Data[4];

    public override string ToString()
    {
        return $"XFStyle Guide Track {BitConverter.ToString(Data).Replace("-", " ")} Right={Right}{(Left > 0 ? $", Left={Left}" : string.Empty)}";
    }
}