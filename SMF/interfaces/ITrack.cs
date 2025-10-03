using libMidi.Messages;

namespace libMidi.SMF.interfaces;

public interface ITrack
{
    #region Properties

    //
    IEnumerable<MidiEvent> Events { get; }

    IEnumerable<MidiEvent> FilterdEvents { get; }

    Dictionary<string, object> Filter { get; }

    bool FilterEnabled { get; set; }

    bool LyricMatched { get; set; }

    int Transpose { get; set; }

    bool Output { get; set; }

    MidiData Parent { get; init; }

    byte TrackNumber { get; }

    // organized
    IEnumerable<byte> Channels { get; }

    byte Channel { get; }

    InstInfo? InstInfo { get; }

    bool IsDrum { get; }

    bool IsCodeTrack { get; set; }

    bool IsPoly { get; }

    bool HasLyric { get; }

    string Lyric { get; }

    float LyricMatchRatio { get; }

    #endregion

    #region Method

    void EventAdd(MidiEvent ev);

    void EventAdd(long deltaTime, MidiMessage message);

    void EventAddRange(IEnumerable<MidiEvent> midiEvents, bool ignoreEOT = true);

    void EventInsertHead(MidiEvent ev);

    void SetFilter(IEnumerable<string> filterNames);

    void DoFilter();

    void Organize();

    byte[] GetByte();

    #endregion
}
