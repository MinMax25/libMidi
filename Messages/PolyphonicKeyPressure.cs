using System.ComponentModel;
using libMidi.Messages.enums;

namespace libMidi.Messages;

[DisplayName("ポリフォニックキープレッシャー")]
public record PolyphonicKeyPressure
    : ChannelNoteMessage
{
    public PolyphonicKeyPressure(byte channel, byte pitch, byte velocity)
        : base(channel, pitch, velocity)
    {
    }

    public override VoiceType VoiceType => VoiceType.PolyphonicKeyPressure;

    public override string ToString() => base.ToString();
}
