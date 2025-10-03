using System.ComponentModel;
using libMidi.Messages.enums;

namespace libMidi.Messages;

[DisplayName("トラック終端")]
public record EndOfTrack
    : MetaMessage
{
    public override MetaType MetaType => MetaType.EndOfTrack;

    public override int Length => 3;

    public override byte[] GetByte() => base.GetByte().Concat(new byte[] { 0x00 }).ToArray();

    public override string ToString() => $"{base.ToString()}";
}
