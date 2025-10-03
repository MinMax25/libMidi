using System.ComponentModel;
using libMidi.Messages.enums;

namespace libMidi.Messages;

[DisplayName("SMPTEオフセット")]
public record SmpteOffset
    : MetaMessage
{
    public SmpteOffset(byte hr, byte mn, byte se, byte fr, byte ff)
    {
        HR = hr;
        MN = mn;
        SE = se;
        FR = fr;
        FF = ff;
    }

    public override MetaType MetaType => MetaType.SmpteOffset;

    public override int Length => base.Length + 1 + 5;  // base & datasize & data.length

    public byte HR { get; init; }
    public byte MN { get; init; }
    public byte SE { get; init; }
    public byte FR { get; init; }
    public byte FF { get; init; }

    public override byte[] GetByte() => base.GetByte().Concat(new byte[] { 0x05, HR, MN, SE, FR, FF }).ToArray();

    public override string ToString() => $"{base.ToString()}";
}
