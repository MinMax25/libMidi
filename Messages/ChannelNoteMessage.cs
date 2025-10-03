using libMidi.Messages.interfaces;

namespace libMidi.Messages;

public abstract record ChannelNoteMessage
    : ChannelVoiceMessage
    , IPitch
    , IVelocity
{
    public ChannelNoteMessage(byte channel, byte pitch, byte velocity)
        : base(channel)
    {
        Pitch = pitch;
        Velocity = velocity;
    }

    public override int Length => 3;

    public byte Pitch { get; init; }
    public byte Velocity { get; init; }

    public MidiMessage ChangePitch(byte pitch) => this with { Pitch = pitch };

    public MidiMessage Transpose(int oct) => this with { Pitch = (byte)(Pitch + (oct * 12)) };

    public MidiMessage ChangeVelocity(byte vel) => this with { Velocity = vel };

    public override byte[] GetByte() => [StatusByte, Pitch, Velocity];

    public override string ToString() => $"{GetType().Name} Note {Pitch.NoteName(),-3}({Pitch,3}) Velocity {Velocity,3}";
}
