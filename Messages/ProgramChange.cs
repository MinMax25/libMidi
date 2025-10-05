using System.ComponentModel;
using libMidi.Messages.enums;

namespace libMidi.Messages;

[DisplayName("プログラムチェンジ")]
public record ProgramChange
    : ChannelVoiceMessage
{
    public ProgramChange(byte channel, byte pgNum)
        : base(channel)
    {
        PgNum = pgNum;
    }

    public override VoiceType VoiceType => VoiceType.ProgramChange;

    public override int Length => 2;

    public byte PgNum { get; init; }

    public override byte[] GetByte() => [StatusByte, PgNum];

    public override string ToString() => $"{GetType().Name} №{PgNum +1 }";
}
