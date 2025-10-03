using System.ComponentModel;
using libMidi.Messages.enums;

namespace libMidi.Messages;

[DisplayName("ポート指定")]
public record PortPrefix
    : MetaMessage
{
    public PortPrefix(byte value)
    {
        Value = value;
    }

    public override MetaType MetaType => MetaType.PortPrefix;

    public override int Length => 4;

    public byte Value { get; init; }

    public override byte[] GetByte() => base.GetByte().Concat(new byte[] { 0x01, Value }).ToArray();

    public override string ToString() => $"{base.ToString()} {Value}";
}
