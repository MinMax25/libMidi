using System.ComponentModel;

namespace libMidi.Messages;

[DisplayName("コンティニュー")]
public record MidiContinue
    : SystemRealTimeMessage
{
    public override byte StatusByte => 0xfb;
}
