namespace libMidi.Messages;

public abstract record SystemMessage
    : MidiMessage
{
    public override int Length => 1;
}
