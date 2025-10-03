using System.ComponentModel;

namespace libMidi.Messages;

[DisplayName("アクティブセンシング")]
public record MidiActivesensing
    : SystemRealTimeMessage
{
    public override byte StatusByte => 0xfe;
}
