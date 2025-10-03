using System.ComponentModel;

namespace libMidi.Messages;

[DisplayName("ソングポジションポインター")]
public record SongPositionPointer
    : SystemCommonMessage
{
    public override byte StatusByte => 0xf2;

    public override int Length => base.Length + 2;
}
