using System.ComponentModel;
using libMidi.Messages.enums;

namespace libMidi.Messages;

[DisplayName("ノート・オン")]
public record NoteOn
    : ChannelNoteMessage
{
    public NoteOn(byte channel, byte pitch, byte velocity)
        : base(channel, pitch, velocity)
    {
    }

    public override VoiceType VoiceType => VoiceType.NoteOn;

    public override string ToString() => base.ToString();
}
