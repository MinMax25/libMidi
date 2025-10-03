using libMidi.Messages;
using libMidi.Messages.enums;
using libMidi.SMF.enums;
using libMidi.SMF.interfaces;

namespace libMidi.SMF;

public class MidiData
{
    public EventHandler<EventArgs>? Converted;

    #region Header Infomation

    public DataFormat Format => NumberOfTrack == 1 ? DataFormat.Format0 : DataFormat.Format1;

    public short Division { get; set; }

    public short NumberOfTrack => (short)Tracks.OfType<Track>().Count();

    #endregion

    #region Another Infomation

    public string SequenceName => GetAllEvents<SequenceTrackName>().FirstOrDefault()?.Message?.Text ?? "********";

    public MidiStd MidiStd { get; set; }

    public int UsedChannelCount => GetAllEvents<ChannelMessage>().Where(x => x.Message.Ch > 0).Select(x => x.Message.Ch).Distinct().Count();

    #endregion

    #region Properties

    public MidiData? Origine { get; set; }

    public IEnumerable<ITrack> Tracks => _Tracks;
    private readonly List<ITrack> _Tracks;

    public ITrack? LyricTrack => Origine?.Tracks.FirstOrDefault(x => x.HasLyric);

    public string FilePath { get; set; } = string.Empty;

    public IEnumerable<MidiEvent> TimeSignatures => _TimeSignatures;
    private readonly List<MidiEvent> _TimeSignatures = new();

    public ConvertType ConvertType { get; set; }

    public IEnumerable<int> DrumChannel => _DrumChannel;
    private readonly List<int> _DrumChannel = new();

    public List<TempoItem> TempoMap { get; init; } = new();

    #endregion

    #region ctor

    public MidiData()
    {
        Division = 480;
        _Tracks = new List<ITrack>();
    }

    public MidiData(short division)
        : this()
    {
        Division = division;
    }

    #endregion

    #region Method

    public void Initialize()
    {
        Division = 480;
        _Tracks.Clear();
    }

    public IEnumerable<MidiEvent> GetAllEvents() => Tracks.SelectMany(t => t.Events);

    public IEnumerable<MidiEvent<T>> GetAllEvents<T>() where T : MidiMessage =>
        GetAllEvents().Where(x => x.Message is T).Select(x => (MidiEvent<T>)x);

    public IEnumerable<MidiEvent<SysExMessage>> GetAllExclusiveEvents(SysExType type) =>
        GetAllEvents<SysExMessage>().Where(x => x.Message?.ExclusiveType == type);

    public ITrack NewTrack(Type type) => TrackBase.NewTrack(type, this);

    public ITrack NewTrack<T>() where T : TrackBase => NewTrack(typeof(T));

    public ITrack AddNewTrack<T>() where T : TrackBase
    {
        ITrack track = NewTrack<T>();
        _Tracks.Add(track);
        return track;
    }

    public void AddTrack(ITrack track) => _Tracks.Add(track);

    public void RemoveTrack(ITrack track) => _Tracks.Remove(track);

    public void ClearTracks() => _Tracks.Clear();

    public ITrack GetTrack(int i) => _Tracks[i];

    public byte GetTrackNumber(ITrack track) => (byte)_Tracks.IndexOf(track);

    public bool IsDrum(MidiEvent ev) => DrumChannel.Contains(ev.Channel);

    public void AnalyzeMidiStandard()
    {
        if (GetAllExclusiveEvents(SysExType.XGSystemOn).Any())
            MidiStd = MidiStd.XG;
        else if (GetAllExclusiveEvents(SysExType.GSReset).Any())
            MidiStd = MidiStd.GS;
        else if (GetAllExclusiveEvents(SysExType.GM2SytemOn).Any())
            MidiStd = MidiStd.GM2;
        else
            MidiStd = MidiStd.GM;
    }

    private void CollectTimeSignatures()
    {
        _TimeSignatures.Clear();

        _TimeSignatures.AddRange(
            Tracks.SelectMany(x => x.Events)
            .Where(x => x.Message is TimeSignature)
            .OrderBy(x => x.AbsoluteTick).ThenBy(x => x.Seqnum)
            .ToArray()
        );
    }

    private void AnalyzeInstrument()
    {
        IEnumerable<MidiEvent<ControlChange>> GetAllControlChangeEvents(ITrack track, CtrlType type) =>
            track.Events.Where(x => x.Message is ControlChange).Select(x => (MidiEvent<ControlChange>)x).Where(x => x.Message?.CtrlType == type);

        foreach (Track tr in Tracks.OfType<Track>().ToArray())
        {
            IEnumerable<MidiEvent<ControlChange>> msbList = GetAllControlChangeEvents(tr, CtrlType.BankMSB).OrderByDescending(x => x.Seqnum).ToArray();
            IEnumerable<MidiEvent<ControlChange>> lsbList = GetAllControlChangeEvents(tr, CtrlType.BankLSB).OrderByDescending(x => x.Seqnum).ToArray();

            foreach (MidiEvent midiEvent in tr.GetMidiEvents<ProgramChange>().ToArray())
            {
                byte? msb = msbList.FirstOrDefault(x => x.Channel == midiEvent.Channel && x.Seqnum < midiEvent.Seqnum)?.Message.Value ?? 0;
                byte? lsb = lsbList.FirstOrDefault(x => x.Channel == midiEvent.Channel && x.Seqnum < midiEvent.Seqnum)?.Message.Value ?? 0;
                byte pgn = (byte)((midiEvent.Message is ProgramChange pch) ? pch.PgNum : 0);
                midiEvent.InstrumentInfo = new InstInfo(MidiStd, msb.Value, lsb.Value, pgn);
            }
        }

        _DrumChannel.Clear();
        _DrumChannel.Add(10);

        if (MidiStd == MidiStd.XG)
        {
            foreach (Track tr in Tracks.OfType<Track>().ToArray())
            {
                tr.GetMidiEvents<ProgramChange>(x =>
                    x.Channel != 10 && x.InstrumentInfo is InstInfo info &&
                    (info.BankMSB == 126 | info.BankMSB == 127)
                ).Select(x => x.Channel).Distinct().ToList().ForEach(x => _DrumChannel.Add(x));
            }
        }

        if (MidiStd == MidiStd.GS)
        {
            foreach (var e in GetAllExclusiveEvents(SysExType.GSUseForRhythmPart).ToArray())
            {
                int part = e.Message.Data[5] & 0xf;
                if (part == 0) continue;
                if (part > 10) part++;
                if (!DrumChannel.Contains(part)) _DrumChannel.Add(part);
            }
        }
    }

    private void OrganizeTracks(bool thruFilter)
    {
        Tracks.ToList().ForEach(tr =>
        {
            tr.FilterEnabled = !thruFilter;
            tr.Organize();
        });
    }

    private void CreateTempoMap()
    {
        TempoMap.Clear();

        foreach (var item in GetAllEvents<Tempo>())
        {
            if (item.Message is not Tempo tempo) continue;
            TempoMap.Add(new TempoItem { Tick = item.AbsoluteTick, BPM = tempo.BPM });
        }

        long tick = 0;
        double bpm = 120;

        foreach (var item in TempoMap)
        {
            var t = item.Tick - tick;
            var s = 60 / bpm / Division;

            item.Time = TimeSpan.FromSeconds(s * t);

            bpm = item.BPM;
            tick = item.Tick;
        }
    }

    public void Organize(bool thruFilter = false)
    {
        AnalyzeMidiStandard();
        CollectTimeSignatures();
        CreateTempoMap();
        AnalyzeInstrument();
        OrganizeTracks(thruFilter);
    }

    public override string ToString()
    {
        return $"{GetType().Name} Format = {Format}, NumberOfTrack = {NumberOfTrack} , Division = {Division}";
    }

    public byte[] GetByte()
    {
        List<byte> bytes =
        [
            // Header
            .. Chunk.HEADER_ID,
            .. (0x06).GetByte(),
            .. ((short)Format).GetByte(),
            .. NumberOfTrack.GetByte(),
            .. Division.GetByte(),
        ];

        // Tracks
        Tracks.Select(tr => tr.GetByte()).ToList().ForEach(bytes.AddRange);

        return bytes.ToArray();
    }

    internal TimeSpan GetTime(long tick)
    {
        TimeSpan time = default;

        double bpm = 120;
        long t = 0;

        foreach (var item in TempoMap.Where(x => x.Tick < tick))
        {
            time += item.Time;
            t = item.Tick;
            bpm = item.BPM;
        }

        time += TimeSpan.FromSeconds((60 / bpm / Division) * (tick - t));

        return time;
    }

    #endregion
}
