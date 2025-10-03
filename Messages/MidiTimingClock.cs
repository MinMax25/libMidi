using System.ComponentModel;

namespace libMidi.Messages;

[DisplayName("MIDIクロック")]
public record MidiTimingClock
    : SystemRealTimeMessage
{
    public override byte StatusByte => 0xf8;
}
