using System.ComponentModel;
using libMidi.Messages.enums;

namespace libMidi.Messages;

[DisplayName("ノート・オフ")]
public record NoteOff
    : ChannelNoteMessage
{
    public NoteOff(byte channel, byte pitch, byte velocity)
        : base(channel, pitch, velocity)
    {
    }

    public override VoiceType VoiceType => VoiceType.NoteOff;

    public override string ToString() => base.ToString();
}
