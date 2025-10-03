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
    #region Local Field

    public event PropertyChangedEventHandler? PropertyChanged;

    private long seqnum = 0;

    public long abstick { get => _abstick; set => _abstick = value; }
    private long _abstick = 0;

    #endregion

    #region Properties

    public IEnumerable<MidiEvent> Events => _Events;

    private readonly List<MidiEvent> _Events = new();

    public IEnumerable<MidiEvent> FilterdEvents => FilterEnabled ? _FilterdEvents : _Events;

    private readonly List<MidiEvent> _FilterdEvents = new();

    public Dictionary<string, object> Filter { get; } = new();

    public bool FilterEnabled { get; set; }

    public bool LyricMatched
    {
        get => _LyricMatched;
        set
        {
            SetProperty(ref _LyricMatched, value);
            DoFilter();
        }
    }
    private bool _LyricMatched;

    public int Transpose { get; set; }

    public bool Output { get; set; } = true;

    public MidiData Parent { get; init; }

    public byte TrackNumber => Parent.GetTrackNumber(this);

    public IEnumerable<byte> Channels { get; private set; } = null!;

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

    #region Method

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
        if (Parent?.LyricTrack == null) return null;

        var ly = Parent.LyricTrack.Events.FirstOrDefault(x => x.AbsoluteTick == ev.AbsoluteTick - 1 || x.AbsoluteTick == ev.AbsoluteTick)?.Message as Lyric;

        if (ly == null) return null;

        string val = ly.Text.Replace("<", string.Empty).Replace("/", string.Empty).Replace(">", string.Empty).Replace("^", string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(val)) return null;

        return ly with { Data = Encoding.UTF8.GetBytes(val) };
    }

    public void Organize()
    {
        //Channels
        Channels = Events.Where(x => x.Channel > 0).Select(x => x.Channel).Distinct().OrderBy(x => x).ToArray();

        //Channel
        Channel = (byte)(Channels.Count() == 1 ? Channels.FirstOrDefault() : 0);

        //InstInfo
        InstInfo =
            (Parent?.ConvertType == ConvertType.Instrument)
            ? Events.FirstOrDefault(x => x.InstrumentInfo != null)?.InstrumentInfo
            : null;

        //IsDrum
        IsDrum = (InstInfo != null) && (Parent?.DrumChannel.Any(x => x == Channel) ?? false);

        //HasLyric
        HasLyric = this.GetMidiMessages<Lyric>().Any();

        //IsPoly
        IsPoly = this.GetMidiEvents<NoteOn>(x => x.Message.Velocity != 0).GroupBy(x => x.AbsoluteTick).Where(x => x.Count() > 1).Any();

        //Lyric
        Lyric =
            string.Join("", this.GetMidiMessages<Lyric>().Select(x => x.Text).ToArray())
            .Replace("<", "\r\n" + "[Page]" + "\r\n")
            .Replace("/", "\r\n")
            .Replace(">", "\t")
            .Replace("^", " ")
            .TrimStart();

        //LyricMatchRatio
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

    public void EventClear() => _Events.Clear();

    public void EventAdd(MidiEvent ev)
    {
        seqnum++;
        if (ev.AbsoluteTick - abstick is long delta && delta < 0) throw new ArgumentException($"{MethodBase.GetCurrentMethod()}");

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

    public void EventAddRange(IEnumerable<MidiEvent> events, bool ignoreEOT = true) =>
        events.OrderBy(x => x.AbsoluteTick).ThenBy(x => x.Seqnum).ToList().ForEach(ev =>
        {
            if (!ignoreEOT | ev.Message is not EndOfTrack) EventAdd(ev);
        });

    public void EventInsertHead(MidiEvent ev) => _Events.Insert(0, ev with { Message = ev.Message with { } });

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

    public void DoFilter()
    {
        if (Parent is null) return;

        _FilterdEvents.Clear();

        // Option Insert TrackName
        if (SMFConverter.Def.Setting.InsertTrackName)
        {
            if (Events.FirstOrDefault(x => x.Message is SequenceTrackName) == null)
            {
                string trackName = $"{this}";

                if (!IsCodeTrack)
                {
                    if (Parent.ConvertType == ConvertType.Instrument &&
                        this.GetInstruments().Count() == 1 &&
                        this.GetInstruments().FirstOrDefault() is InstInfo info)
                    {
                        trackName = InstMap.GetInstName(info, IsDrum) ?? trackName;
                    }
                }

                var metaName = new MidiEvent(this) { AbsoluteTick = 0, Message = new SequenceTrackName(trackName) };

                _FilterdEvents.Add(metaName);
            }
        }

        bool hitWord = false;
        string lastWord = string.Empty;

        foreach (MidiEvent ev in Events.OrderBy(x => x.Seqnum).ToArray())
        {
            // Option Lyric Lyric Adjustment
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

            // Option Remove ProgramChange
            if (SMFConverter.Def.Setting.RemoveProgramChange)
            {
                if (ev.Message is ProgramChange) continue;

                if (ev.Message is ControlChange rmoveCC &&
                    rmoveCC.CtrlType is CtrlType.BankMSB or CtrlType.BankLSB)
                    continue;
            }

            //
            if (SMFConverter.Def.Setting.XFStyleConvert)
            {
                if (ev.Message is XFStyleRehearsalMark rehearsalMark)
                {
                    _FilterdEvents.Add(ev with { Message = new Marker(rehearsalMark.Section()) });
                }
            }

            // Filter(MetaMessage, ChannelVoiceMessage)
            if (Filter.ContainsValue(ev.Message.GetType()))
            {
                // Set Lyric
                if (LyricMatched && ev.Message is NoteOn lyricNote && lyricNote.Velocity != 0)
                {
                    if (MatchLyric(ev) is Lyric lyric)
                    {
                        string word = lyric.Text;
                        if (SMFConverter.Def.Setting.LyricPaddingPlus)
                        {
                            word = Regex.Replace(word, @"\[.+\]", string.Empty);
                            word = Regex.Replace(word, @"\[.+", string.Empty);
                            word = Regex.Replace(word, @".+\]", string.Empty);
                            if (word.Length == 0) word = "+";
                        }
                        _FilterdEvents.Add(ev with { Message = new Lyric(word) });
                        lastWord = word;
                        hitWord = true;
                    }
                    else if (SMFConverter.Def.Setting.LyricPaddingPlus)
                    {
                        string lyr = MidiExtensions.IsKanji(lastWord.FirstOrDefault()) || MidiExtensions.IsAlphaNumeric(lastWord) ? "+" : "-";
                        _FilterdEvents.Add(ev with { Message = new Lyric(Encoding.UTF8.GetBytes((hitWord ? lyr : "La"))) });
                        lastWord = "+";
                    }
                }

                MidiMessage? msg;

                if (ev.Message is ChannelNoteMessage note)
                {
                    // Option Replace NoteOn with NoteOff at velocity 0
                    if (SMFConverter.Def.Setting.ReplaceNoteOn && ev.Message is NoteOn repNote && repNote.Velocity == 0)
                    {
                        msg = new NoteOff(repNote.Ch, repNote.Pitch, 0).Transpose(Transpose);
                    }
                    else
                    {
                        msg = note.Transpose(Transpose);
                    }
                }
                else
                {
                    msg = ev.Message;
                }

                MidiEvent fltEvent = ev with { AbsoluteTick = ev.AbsoluteTick };

                if (msg != null)
                {
                    fltEvent = ev with { AbsoluteTick = ev.AbsoluteTick, Message = msg };
                }
                else
                {
                    throw new ArgumentException($"{MethodBase.GetCurrentMethod()}");
                }

                _FilterdEvents.Add(fltEvent);

                continue;
            }

            // Filter(SysExMessage)
            if (ev.Message is SysExMessage sysEx && sysEx.ExclusiveType.GetDisplayAttribute()?.Name is string name && Filter.ContainsKey(name))
            {
                _FilterdEvents.Add(ev with { Message = ev.Message with { } });
                continue;
            }

            // Filter(ControlChange)
            if (ev.Message is ControlChange cc && Filter.ContainsValue(cc.CtrlType))
            {
                _FilterdEvents.Add(ev with { Message = ev.Message with { } });
                continue;
            }
        }

        // Option Channel Fix
        if ((Parent.ConvertType == ConvertType.Instrument) & SMFConverter.Def.Setting.ChannelFix)
        {
            foreach (MidiEvent item in _FilterdEvents.Where(x => x.Message is ChannelMessage))
            {
                if (item.Message is ChannelMessage msg)
                {
                    item.Message = msg.ChangeChannel((byte)(IsDrum ? 10 : 1));
                }
            }
        }

        var eot = (_FilterdEvents.LastOrDefault() ?? new MidiEvent(this) { AbsoluteTick = 0 }) with { Message = new EndOfTrack() };
        _FilterdEvents.Add(eot);

        int seqnum = 1;
        _FilterdEvents.ForEach(ev => ev.Seqnum = seqnum++);
    }

    public byte[] GetByte()
    {
        List<byte> bytes = new();

        int size = Events.Select(x => x.GetByte().Length).Sum();

        bytes.AddRange(ChunkID);
        bytes.AddRange(size.GetByte());
        Events.ToList().ForEach(ev => bytes.AddRange(ev.GetByte()));

        return bytes.ToArray();
    }

    public override string ToString()
    {
        if (Parent == null)
        {
            return GetType().Name;
        }

        if (!IsCodeTrack && (Parent.ConvertType == ConvertType.Instrument) & this.GetInstruments().Count() == 1)
        {
            var info = Events.First(x => x.InstrumentInfo != null).InstrumentInfo;
            if (info != null)
            {
                var instName = InstMap.GetInstName(info, IsDrum) ?? $"[{info.BankMSB}, {info.BankLSB}, {info.PgNum}]";
                return instName;
            }
        }

        if (Events.FirstOrDefault(x => x.Message is SequenceTrackName)?.Message is SequenceTrackName trackName)
        {
            return trackName.Text;
        }
        else
        {
            return GetType().Name + (this is not Track ? string.Empty : (Parent.Tracks.ToList().IndexOf(this) + 1).ToString());
        }
    }

    #endregion
}
