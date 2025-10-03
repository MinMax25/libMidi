namespace libMidi.SMF;

public class Track
    : TrackBase
{
    public Track(MidiData midiData) : base(midiData) { }

    public override byte[] ChunkID => TRACK_ID;
}
