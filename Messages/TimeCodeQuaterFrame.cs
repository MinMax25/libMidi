using System.ComponentModel;

namespace libMidi.Messages;

[DisplayName("MIDIタイムコードクォーターフレーム")]
public record TimeCodeQuaterFrame
    : SystemCommonMessage
{
    public override byte StatusByte => 0xf1;

    public override int Length => base.Length + 1;
}
