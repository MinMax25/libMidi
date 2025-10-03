namespace libMidi.SMF;

public class XFInfomationHeader
    : TrackBase
{
    public XFInfomationHeader(MidiData midiData) : base(midiData) { }

    public override byte[] ChunkID => XFID_ID;
}
