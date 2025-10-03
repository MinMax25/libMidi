using System.ComponentModel;

namespace libMidi.Messages;

[DisplayName("スタート")]
public record MidiStart
    : SystemRealTimeMessage
{
    public override byte StatusByte => 0xfa;
}
