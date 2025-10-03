using libMidi.SMF.interfaces;

namespace libMidi.Messages;

public abstract record MidiMessage
{
    public abstract byte StatusByte { get; }

    public abstract int Length { get; }

    public abstract byte[] GetByte();

    public virtual IMidiEvent? Parent { get; set; }
}
