using System.ComponentModel;
using libMidi.Messages.enums;

namespace libMidi.Messages;

[DisplayName("ピッチベンド")]
public record PitchBend
    : ChannelVoiceMessage
{
    public PitchBend(byte channel, short value)
        : base(channel)
    {
        Value = value;
    }

    public override VoiceType VoiceType => VoiceType.PitchBend;

    public override int Length => 3;

    public short Value { get; init; }

    public override byte[] GetByte()
    {
        int val = Value + 8192;
        int lsb = val % 128;
        int msb = (val - lsb) / 128;

        return [StatusByte, (byte)lsb, (byte)msb];
    }

    public override string ToString() => $"{GetType().Name} Value {Value}";
}