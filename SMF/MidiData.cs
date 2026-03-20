using libMidi.Messages;
using libMidi.Messages.enums;
using libMidi.SMF.enums;
using libMidi.SMF.interfaces;

namespace libMidi.SMF;

public class MidiData
{
    #region Fields

    public EventHandler<EventArgs>? Converted;

    private readonly List<ITrack> _Tracks = new List<ITrack>();

    private readonly List<MidiEvent> _TimeSignatures = new List<MidiEvent>();

    private readonly List<int> _DrumChannel = new List<int>();

    #endregion

    #region Properties

    #region Header Information

    public DataFormat Format
    {
        get
        {
            return NumberOfTrack == 1 ? DataFormat.Format0 : DataFormat.Format1;
        }
    }

    public short Division { get; set; }

    public short NumberOfTrack
    {
        get
        {
            return (short)Tracks.OfType<Track>().Count();
        }
    }

    #endregion

    #region Another Information

    public string SequenceName
    {
        get
        {
            return GetAllEvents<SequenceTrackName>().FirstOrDefault()?.Message?.Text ?? "********";
        }
    }

    public MidiStd MidiStd { get; set; }

    public int UsedChannelCount
    {
        get
        {
            return GetAllEvents<ChannelMessage>()
                   .Where(x => x.Message.Ch > 0)
                   .Select(x => x.Message.Ch)
                   .Distinct()
                   .Count();
        }
    }

    #endregion

    public MidiData? Origin { get; set; }

    public IEnumerable<ITrack> Tracks
    {
        get
        {
            return _Tracks;
        }
    }

    public ITrack? LyricTrack
    {
        get
        {
            return Origin?.Tracks.FirstOrDefault(x => x.HasLyric);
        }
    }

    public string FilePath { get; set; } = string.Empty;

    public IEnumerable<MidiEvent> TimeSignatures
    {
        get
        {
            return _TimeSignatures;
        }
    }

    public ConvertType ConvertType { get; set; }

    public IEnumerable<int> DrumChannel
    {
        get
        {
            return _DrumChannel;
        }
    }

    public List<TempoItem> TempoMap { get; init; } = new List<TempoItem>();

    public bool IsMultiTimber
    {
        get
        {
            return Format != DataFormat.Format0 && Tracks.Any(x => x.Channels.Count() > 1);
        }
    }

    #endregion

    #region ctor

    public MidiData()
    {
        Division = 480;
    }

    public MidiData(short division) : this()
    {
        Division = division;
    }

    #endregion

    #region Methods

    #region General

    public void Initialize()
    {
        Division = 480;
        _Tracks.Clear();
    }

    public IEnumerable<MidiEvent> GetAllEvents()
    {
        return Tracks.SelectMany(t => t.Events);
    }

    public IEnumerable<MidiEvent<T>> GetAllEvents<T>() where T : MidiMessage
    {
        return GetAllEvents().Where(x => x.Message is T).Select(x => (MidiEvent<T>)x);
    }

    public IEnumerable<MidiEvent<SysExMessage>> GetAllExclusiveEvents(SysExType type)
    {
        return GetAllEvents<SysExMessage>().Where(x => x.Message?.ExclusiveType == type);
    }

    public ITrack NewTrack(Type type)
    {
        return TrackBase.NewTrack(type, this);
    }

    public ITrack NewTrack<T>() where T : TrackBase
    {
        return NewTrack(typeof(T));
    }

    public ITrack AddNewTrack<T>() where T : TrackBase
    {
        ITrack track = NewTrack<T>();
        _Tracks.Add(track);
        return track;
    }

    public void AddTrack(ITrack track)
    {
        _Tracks.Add(track);
    }

    public void RemoveTrack(ITrack track)
    {
        _Tracks.Remove(track);
    }

    public void ClearTracks()
    {
        _Tracks.Clear();
    }

    public ITrack GetTrack(int i)
    {
        return _Tracks[i];
    }

    public byte GetTrackNumber(ITrack track)
    {
        return (byte)_Tracks.IndexOf(track);
    }

    public bool IsDrum(MidiEvent ev)
    {
        return DrumChannel.Contains(ev.Channel);
    }

    public void AnalyzeMidiStandard()
    {
        if (GetAllExclusiveEvents(SysExType.XGSystemOn).Any())
        {
            MidiStd = MidiStd.XG;
        }
        else if (GetAllExclusiveEvents(SysExType.GSReset).Any())
        {
            MidiStd = MidiStd.GS;
        }
        else if (GetAllExclusiveEvents(SysExType.GM2SytemOn).Any())
        {
            MidiStd = MidiStd.GM2;
        }
        else
        {
            MidiStd = MidiStd.GM;
        }
    }

    private void CollectTimeSignatures()
    {
        _TimeSignatures.Clear();

        var events = Tracks.SelectMany(x => x.Events)
                           .Where(x => x.Message is TimeSignature)
                           .OrderBy(x => x.AbsoluteTick)
                           .ThenBy(x => x.Seqnum)
                           .ToArray();

        _TimeSignatures.AddRange(events);
    }

    private void AnalyzeInstrument()
    {
        foreach (Track tr in Tracks.OfType<Track>().ToArray())
        {
            var msbList = tr.Events.Where(x => x.Message is ControlChange cc && cc.CtrlType == CtrlType.BankMSB)
                                   .Select(x => (MidiEvent<ControlChange>)x)
                                   .OrderByDescending(x => x.Seqnum)
                                   .ToArray();

            var lsbList = tr.Events.Where(x => x.Message is ControlChange cc && cc.CtrlType == CtrlType.BankLSB)
                                   .Select(x => (MidiEvent<ControlChange>)x)
                                   .OrderByDescending(x => x.Seqnum)
                                   .ToArray();

            foreach (MidiEvent midiEvent in tr.GetMidiEvents<ProgramChange>().ToArray())
            {
                byte msb = msbList.FirstOrDefault(x => x.Channel == midiEvent.Channel && x.Seqnum < midiEvent.Seqnum)?.Message.Value ?? 0;
                byte lsb = lsbList.FirstOrDefault(x => x.Channel == midiEvent.Channel && x.Seqnum < midiEvent.Seqnum)?.Message.Value ?? 0;
                byte pgn = (byte)((midiEvent.Message is ProgramChange pch) ? pch.PgNum : 0);

                midiEvent.InstrumentInfo = new InstInfo(MidiStd, msb, lsb, pgn);
            }
        }

        _DrumChannel.Clear();
        _DrumChannel.Add(10);

        if (MidiStd == MidiStd.XG)
        {
            foreach (Track tr in Tracks.OfType<Track>().ToArray())
            {
                var drumChannels = tr.GetMidiEvents<ProgramChange>(x =>
                        x.Channel != 10 && x.InstrumentInfo is InstInfo info &&
                        (info.BankMSB == 126 || info.BankMSB == 127)
                    )
                    .Select(x => x.Channel)
                    .Distinct();

                foreach (var ch in drumChannels)
                {
                    _DrumChannel.Add(ch);
                }
            }
        }

        if (MidiStd == MidiStd.GS)
        {
            foreach (var e in GetAllExclusiveEvents(SysExType.GSUseForRhythmPart).ToArray())
            {
                int part = e.Message.Data[5] & 0xf;

                if (part == 0)
                {
                    continue;
                }

                if (part > 10)
                {
                    part++;
                }

                if (!DrumChannel.Contains(part))
                {
                    _DrumChannel.Add(part);
                }
            }
        }
    }

    private void OrganizeTracks(bool thruFilter)
    {
        foreach (var tr in Tracks)
        {
            tr.FilterEnabled = !thruFilter;
            tr.Organize();
        }
    }

    private void CreateTempoMap()
    {
        TempoMap.Clear();

        foreach (var item in GetAllEvents<Tempo>())
        {
            if (item.Message is not Tempo tempo)
            {
                continue;
            }

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
        List<byte> bytes = new List<byte>();

        // Header
        bytes.AddRange(Chunk.HEADER_ID);
        bytes.AddRange(((int)0x06).GetByte());
        bytes.AddRange(((short)Format).GetByte());
        bytes.AddRange(NumberOfTrack.GetByte());
        bytes.AddRange(Division.GetByte());

        // Tracks
        foreach (var tr in Tracks)
        {
            bytes.AddRange(tr.GetByte());
        }

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

    #endregion
}