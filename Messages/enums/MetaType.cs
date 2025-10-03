namespace libMidi.Messages.enums;

public enum MetaType
{
    SequenceNumber,
    MetaText,
    Copyright,
    SequenceTrackName,
    InstrumentName,
    Lyric,
    Marker,
    CuePoint,
    ProgramName,
    DeviceName,
    ChannelPrefix = 0x20,
    PortPrefix = 0x21,
    EndOfTrack = 0x2F,
    Tempo = 0x51,
    SmpteOffset = 0x54,
    TimeSignature = 0x58,
    KeySignature,
    SequencerSpecific = 0x7F
}
