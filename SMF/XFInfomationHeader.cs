namespace libMidi.SMF;

public class XFInformationHeader
    : TrackBase
{
    public XFInformationHeader(MidiData midiData) : base(midiData) { }

    public override byte[] ChunkID => XFID_ID;
}

[System.Obsolete("Use XFInformationHeader instead.")]
public class XFInfomationHeader : XFInformationHeader
{
    public XFInfomationHeader(MidiData midiData) : base(midiData) { }
}
