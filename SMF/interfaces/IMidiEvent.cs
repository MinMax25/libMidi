namespace libMidi.SMF.interfaces;

public interface IMidiEvent
{
    long AbsoluteTick { get; set; }

    long DeltaTick { get; set; }

    long Seqnum { get; set; }

    byte Channel { get; }

    InstInfo? InstrumentInfo { get; set; }

    ITrack Parent { get; init; }

    byte[] GetByte();
}
