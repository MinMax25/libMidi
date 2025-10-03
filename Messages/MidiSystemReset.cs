using System.ComponentModel;

namespace libMidi.Messages;

[DisplayName("システムリセット")]
public record MidiSystemReset
    : SystemRealTimeMessage
{
    public override byte StatusByte => 0xff;
}
