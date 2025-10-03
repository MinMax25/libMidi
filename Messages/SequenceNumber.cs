using System.ComponentModel;
using libMidi.Messages.enums;

namespace libMidi.Messages;

[DisplayName("シーケンス番号")]
public record SequenceNumber
    : MetaMessage
{
    public SequenceNumber(short number)
    {
        Number = number;
    }

    public override MetaType MetaType => MetaType.SequenceNumber;

    public override int Length => 5;

    public short Number { get; init; }

    public override byte[] GetByte() => base.GetByte().Concat(new byte[] { 0x01 }).Concat(Number.GetByte()).ToArray();

    public override string ToString() => $"{base.ToString()} {Number}";
}
