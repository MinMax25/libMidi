using System.ComponentModel;

namespace libMidi.Messages;

[DisplayName("ソングセレクター")]
public record SongSelect
    : SystemCommonMessage
{
    public override byte StatusByte => 0xf3;

    public override int Length => base.Length + 1;
}
