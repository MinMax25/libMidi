using System.ComponentModel;

namespace libMidi.Messages;

[DisplayName("ストップ")]
public record MidiStop
    : SystemRealTimeMessage
{
    public override byte StatusByte => 0xfc;
}
