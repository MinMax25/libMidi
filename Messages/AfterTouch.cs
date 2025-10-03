using System.ComponentModel;
using libMidi.Messages.enums;
using libMidi.Messages.interfaces;

namespace libMidi.Messages;

[DisplayName("アフタータッチ")]
public record AfterTouch
    : ChannelVoiceMessage
    , IVelocity
{
    public AfterTouch(byte channel, byte velocity)
        : base(channel)
    {
        Velocity = velocity;
    }

    public override VoiceType VoiceType => VoiceType.AfterTouch;

    public override int Length => 2;

    public byte Velocity { get; init; }

    public MidiMessage ChangeVelocity(byte vel) => this with { Velocity = vel };

    public override byte[] GetByte() => [StatusByte, Velocity];

    public override string ToString() => $"{GetType().Name} Velocity {Velocity}";
}
