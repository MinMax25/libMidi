namespace libMidi.Messages.interfaces;

public interface IVelocity
{
    byte Velocity { get; init; }

    MidiMessage ChangeVelocity(byte vel);
}