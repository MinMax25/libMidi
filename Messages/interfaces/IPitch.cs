namespace libMidi.Messages.interfaces;

public interface IPitch
{
    byte Pitch { get; init; }

    MidiMessage ChangePitch(byte pitch);

    MidiMessage Transpose(int oct);
}
