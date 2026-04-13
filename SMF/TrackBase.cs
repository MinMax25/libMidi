using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using libMidi.Messages;
using libMidi.Messages.enums;
using libMidi.Messages.interfaces;
using libMidi.SMF.enums;
using libMidi.SMF.interfaces;

namespace libMidi.SMF;

public abstract class TrackBase
    : Chunk
    , ITrack
    , INotifyPropertyChanged
{
    #region Fields

    public event PropertyChangedEventHandler? PropertyChanged;

    private long seqnum = 0;

    private long _abstick = 0;

    private bool _LyricMatched;

    private readonly List<MidiEvent> _Events = [];

    private readonly List<MidiEvent> _FilteredEvents = [];

    #endregion

    #region Properties

    public long abstick
    {
        get
        {
            return _abstick;
        }
        set
        {
            _abstick = value;
        }
    }

    public IEnumerable<MidiEvent> Events
    {
        get
        {
            return _Events;
        }
    }

    public IEnumerable<MidiEvent> FilteredEvents
    {
        get
        {
            return FilterEnabled ? _FilteredEvents : _Events;
        }
    }

    public Dictionary<string, object> Filter { get; } = [];

    public bool FilterEnabled { get; set; }

    public bool LyricMatched
    {
        get
        {
            return _LyricMatched;
        }
        set
        {
            if (SetProperty(ref _LyricMatched, value))
            {
                DoFilter();
            }
        }
    }

    public int Transpose { get; set; }

    public bool Output { get; set; } = true;

    public MidiData Parent { get; init; }

    public byte TrackNumber
    {
        get
        {
            return Parent.GetTrackNumber(this);
        }
    }

    public IEnumerable<byte> Channels { get; private set; } = [];

    public byte Channel { get; private set; }

    public InstInfo? InstInfo { get; private set; }

    public bool IsDrum { get; private set; }

    public bool IsCodeTrack { get; set; } = false;

    public bool HasLyric { get; private set; }

    public bool IsPoly { get; private set; }

    public string Lyric { get; private set; } = string.Empty;

    public float LyricMatchRatio { get; private set; }

    #endregion

    #region ctor

    public TrackBase(MidiData midiData)
    {
        Parent = midiData;
    }

    #endregion

    #region Methods

    #region Property Change Handler

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(storage, value))
        {
            return false;
        }

        storage = value;
        RaisePropertyChanged(propertyName);
        return true;
    }

    protected virtual void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    #region General

    public void Organize()
    {
        // Channels / Channel
        Channels = _Events.Where(x => x.Channel > 0).Select(x => x.Channel).Distinct().OrderBy(x => x).ToArray();
        Channel = (byte)(Channels.Count() == 1 ? Channels.FirstOrDefault() : 0);

        // InstInfo
        InstInfo = (Parent?.ConvertType == ConvertType.Instrument || Parent?.ConvertType == ConvertType.MultiTimber)
            ? _Events.FirstOrDefault(x => x.InstrumentInfo != null)?.InstrumentInfo
            : null;

        if (Channel == 10 && Parent != null && InstInfo == null)
        {
            InstInfo = new InstInfo(Parent.MidiStd, 0, 0, 0);
            EventAdd(0, new ProgramChange(10, 0));
        }

        // Flags
        IsDrum = (InstInfo != null) && (Parent?.DrumChannel.Any(x => x == Channel) ?? false);
        HasLyric = this.GetMidiMessages<Lyric>().Any();
        IsPoly = this.GetMidiEvents<NoteOn>(x => x.Message.Velocity != 0).GroupBy(x => x.AbsoluteTick).Where(x => x.Count() > 1).Any();

        // Lyric String
        Lyric = string.Join("", this.GetMidiMessages<Lyric>().Select(x => x.Text).ToArray())
            .Replace("<", "\r\n" + "[Page]" + "\r\n")
            .Replace("/", "\r\n")
            .Replace(">", "\t")
            .Replace("^", " ")
            .TrimStart();

        // Ratio
        if (Parent?.ConvertType != ConvertType.Instrument)
        {
            LyricMatchRatio = 0;
        }
        else
        {
            int count = Parent?.LyricTrack?.GetMidiMessages<Lyric>().Count() ?? 0;
            var mt = this.GetMidiEvents<NoteOn>(x => x.Message.Velocity != 0).GroupBy(x => x.AbsoluteTick).Select(x => x.FirstOrDefault()).ToArray();
            int match = mt.Where(x => x != null && MatchLyric(x) != null).Count();
            LyricMatchRatio = count != 0 ? ((float)match / count * 100) : 0;
        }
    }

    public void DoFilter()
    {
        if (Parent is null)
        {
            return;
        }

        _FilteredEvents.Clear();

        // Option: Insert TrackName
        if (SMFConverter.Def.Setting.InsertTrackName)
        {
            if (_Events.FirstOrDefault(x => x.Message is SequenceTrackName) == null)
            {
                string trackName = GetDefaultTrackName();
                var metaName = new MidiEvent(this) { AbsoluteTick = 0, Message = new SequenceTrackName(trackName) };
                _FilteredEvents.Add(metaName);
            }
        }

        bool hitWord = false;
        string lastWord = string.Empty;

        foreach (MidiEvent ev in _Events.OrderBy(x => x.Seqnum))
        {
            // Option: Lyric Adjustment
            if (SMFConverter.Def.Setting.InsertTrackName && LyricMatched)
            {
                if (!ev.WhichCtrlType(CtrlType.BankMSB) &
                    !ev.WhichCtrlType(CtrlType.BankLSB) &
                    ev.Message is not IPitch &
                    ev.Message is not ProgramChange)
                {
                    continue;
                }
            }

            // Option: Remove ProgramChange
            if (SMFConverter.Def.Setting.RemoveProgramChange)
            {
                if (ev.Message is ProgramChange)
                {
                    continue;
                }

                if (ev.Message is ControlChange rmoveCC && (rmoveCC.CtrlType is CtrlType.BankMSB or CtrlType.BankLSB))
                {
                    continue;
                }
            }

            // Option: XF Style Convert
            if (SMFConverter.Def.Setting.XFStyleConvert)
            {
                if (ev.Message is XFStyleRehearsalMark rehearsalMark && Filter.ContainsValue(typeof(libMidi.Messages.Marker)))
                {
                    _FilteredEvents.Add(ev with { Message = new Marker(rehearsalMark.Section()) });
                }
            }

            // Filter (Meta, Channel, CC, SysEx)
            if (IsEventTarget(ev))
            {
                MidiMessage msg = ev.Message;

                // Set Lyric
                if (LyricMatched && msg is NoteOn lyricNote && lyricNote.Velocity != 0)
                {
                    if (ProcessLyricMatching(ev, ref lastWord, ref hitWord) is Lyric lyric)
                    {
                        _FilteredEvents.Add(ev with { Message = lyric });
                    }
                }

                // Note Transpose / NoteOn to NoteOff conversion
                if (msg is ChannelNoteMessage note)
                {
                    if (SMFConverter.Def.Setting.ReplaceNoteOn && note is NoteOn repNote && repNote.Velocity == 0)
                    {
                        msg = new NoteOff(repNote.Ch, repNote.Pitch, 0).Transpose(Transpose);
                    }
                    else
                    {
                        msg = note.Transpose(Transpose);
                    }
                }

                _FilteredEvents.Add(ev with { Message = msg });
            }
        }

        // Option: Channel Fix
        if ((Parent.ConvertType == ConvertType.Instrument) & SMFConverter.Def.Setting.ChannelFix)
        {
            ApplyChannelFix();
        }

        // Finalize (EOT & Seqnum)
        AddEndOfTrack();
    }

    public void EventClear()
    {
        _Events.Clear();
    }

    public void EventAdd(MidiEvent ev)
    {
        seqnum++;
        long delta = ev.AbsoluteTick - abstick;
        if (delta < 0)
        {
            throw new ArgumentException($"{MethodBase.GetCurrentMethod()}");
        }

        var addev = ev with { Parent = this, Seqnum = seqnum, DeltaTick = delta, Message = ev.Message with { } };
        _Events.Add(addev);
        abstick = ev.AbsoluteTick;
    }

    public void EventAdd(long deltaTime, MidiMessage message)
    {
        seqnum++;
        abstick += deltaTime;
        var addev = new MidiEvent(this) { Seqnum = seqnum, AbsoluteTick = abstick, DeltaTick = deltaTime, Message = message };
        _Events.Add(addev);
    }

    public void EventAddRange(IEnumerable<MidiEvent> events, bool ignoreEOT = true)
    {
        foreach (var ev in events.OrderBy(x => x.AbsoluteTick).ThenBy(x => x.Seqnum))
        {
            if (!ignoreEOT | ev.Message is not EndOfTrack)
            {
                EventAdd(ev);
            }
        }
    }

    public void EventInsertHead(MidiEvent ev)
    {
        _Events.Insert(0, ev with { Message = ev.Message with { } });
    }

    public void SetFilter(IEnumerable<string> filterNames)
    {
        Filter.Clear();
        foreach (string name in filterNames)
        {
            if (SMFConverter.Def.FilterTargetList.TryGetValue(name, out object? value))
            {
                Filter.Add(name, value);
            }
        }

        DoFilter();
    }

    public byte[] GetByte()
    {
        List<byte> bytes = [];
        int size = _Events.Select(x => x.GetByte().Length).Sum();

        bytes.AddRange(ChunkID);
        bytes.AddRange(size.GetByte());
        foreach (var ev in _Events)
        {
            bytes.AddRange(ev.GetByte());
        }

        return bytes.ToArray();
    }

    internal static ITrack NewTrack(Type type, MidiData midiData)
    {
        if (Activator.CreateInstance(type, [midiData]) is not ITrack track)
        {
            throw new ArgumentException($"{MethodBase.GetCurrentMethod()}");
        }

        return track;
    }

    private Lyric? MatchLyric(IMidiEvent ev)
    {
        if (Parent?.LyricTrack == null)
        {
            return null;
        }

        var ly = Parent.LyricTrack.Events.FirstOrDefault(x => x.AbsoluteTick == ev.AbsoluteTick - 1 || x.AbsoluteTick == ev.AbsoluteTick)?.Message as Lyric;
        if (ly == null)
        {
            return null;
        }

        string val = ly.Text.Replace("<", string.Empty).Replace("/", string.Empty).Replace(">", string.Empty).Replace("^", string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(val))
        {
            return null;
        }

        return ly with { Data = Encoding.UTF8.GetBytes(val) };
    }

    private string GetDefaultTrackName()
    {
        string trackName = $"{this}";
        if (!IsCodeTrack && Parent.ConvertType == ConvertType.Instrument && this.GetInstruments().Count() == 1 && this.GetInstruments().FirstOrDefault() is InstInfo info)
        {
            trackName = InstMap.GetInstName(info, IsDrum) ?? trackName;
        }

        return trackName;
    }

    private bool IsEventTarget(MidiEvent ev)
    {
        if (Filter.ContainsValue(ev.Message.GetType()))
        {
            return true;
        }

        if (ev.Message is SysExMessage sysEx && sysEx.ExclusiveType.GetDisplayAttribute()?.Name is string name && Filter.ContainsKey(name))
        {
            return true;
        }

        if (ev.Message is ControlChange cc && Filter.ContainsValue(cc.CtrlType))
        {
            return true;
        }

        return false;
    }

    private MidiMessage ProcessLyricMatching(MidiEvent ev, ref string lastWord, ref bool hitWord)
    {
        if (MatchLyric(ev) is Lyric lyric)
        {
            string word = lyric.Text;
            if (SMFConverter.Def.Setting.LyricPaddingPlus)
            {
                word = Regex.Replace(word, @"\[.+?\]", string.Empty);
                if (word.Length == 0)
                {
                    word = "+";
                }
            }

            lastWord = word;
            hitWord = true;
            return new Lyric(word);
        }
        else if (SMFConverter.Def.Setting.LyricPaddingPlus)
        {
            string lyr = (MidiExtensions.IsKanji(lastWord.FirstOrDefault()) || MidiExtensions.IsAlphaNumeric(lastWord)) ? "+" : "-";
            lastWord = "+";
            return new Lyric(Encoding.UTF8.GetBytes(hitWord ? lyr : "La"));
        }

        return ev.Message;
    }

    private void ApplyChannelFix()
    {
        foreach (MidiEvent item in _FilteredEvents.Where(x => x.Message is ChannelMessage))
        {
            if (item.Message is ChannelMessage msg)
            {
                item.Message = msg.ChangeChannel((byte)(IsDrum ? 10 : 1));
            }
        }
    }

    private void AddEndOfTrack()
    {
        var eotEvent = (_FilteredEvents.LastOrDefault() ?? new MidiEvent(this) { AbsoluteTick = 0 }) with { Message = new EndOfTrack() };
        _FilteredEvents.Add(eotEvent);

        int seq = 1;
        foreach (var ev in _FilteredEvents)
        {
            ev.Seqnum = seq++;
        }
    }

    public override string ToString()
    {
        if (Parent == null)
        {
            return GetType().Name;
        }

        if (!IsCodeTrack && (Parent.ConvertType == ConvertType.Instrument) & this.GetInstruments().Count() == 1)
        {
            var info = _Events.FirstOrDefault(x => x.InstrumentInfo != null)?.InstrumentInfo;
            if (info != null)
            {
                return InstMap.GetInstName(info, IsDrum) ?? $"[{info.BankMSB}, {info.BankLSB}, {info.PgNum}]";
            }
        }

        if (_Events.FirstOrDefault(x => x.Message is SequenceTrackName)?.Message is SequenceTrackName trackName)
        {
            return trackName.Text;
        }

        return GetType().Name + (this is not Track ? string.Empty : (Parent.Tracks.ToList().IndexOf(this) + 1).ToString());
    }

    #endregion

    #endregion
}