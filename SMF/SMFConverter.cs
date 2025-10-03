using System.ComponentModel;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using libMidi.Messages;
using libMidi.Messages.enums;
using libMidi.SMF.enums;
using libMidi.SMF.interfaces;

namespace libMidi.SMF;

public class SMFConverter
{
    const string CONFIG_ROOT = @"Config";
    const string CONFIG_NAME = @"Config.json";

    #region Properties

    public static SMFConverter Def
    {
        get
        {
            if (_Def == null)
            {
                _Def ??= new();
                LoadConfig();
            }
            return _Def;
        }
    }

    private static SMFConverter _Def = null!;

    public ConverterSetting Setting { get; private set; } = new();

    #region Filter

    public Dictionary<string, object> Filter { get; } = new();

    public Dictionary<string, object> FilterTargetList { get; } = new();

    public Dictionary<string, object> InitFilter { get; } = new();

    public Dictionary<string, object> InitMeta =>
        FilterTargetList.Where(x => x.Value is Type tp && tp.IsSubclassOf(typeof(MetaMessage)))
        .ToDictionary(x => x.Key, x => x.Value);

    public Dictionary<string, object> InitChannelVoice =>
        FilterTargetList.Where(x => x.Value is Type tp && tp.IsSubclassOf(typeof(ChannelVoiceMessage)))
        .ToDictionary(x => x.Key, x => x.Value);

    public Dictionary<string, object> InitSysEx =>
        FilterTargetList.Where(x => x.Value is SysExType)
        .ToDictionary(x => x.Key, x => x.Value);

    public Dictionary<string, object> InitControlChange =>
        FilterTargetList.Where(x => x.Value is CtrlType)
        .ToDictionary(x => x.Key, x => x.Value);

    public Dictionary<string, object> FilterMeta =>
        Filter.Where(x => x.Value is Type tp && tp.IsSubclassOf(typeof(MetaMessage)))
        .ToDictionary(x => x.Key, x => x.Value);

    public Dictionary<string, object> FilterChannelVoice =>
        Filter.Where(x => x.Value is Type tp && tp.IsSubclassOf(typeof(ChannelVoiceMessage)))
        .ToDictionary(x => x.Key, x => x.Value);

    public Dictionary<string, object> FilterSysEx =>
        Filter.Where(x => x.Value is SysExType)
        .ToDictionary(x => x.Key, x => x.Value);

    public Dictionary<string, object> FilterControlChange =>
        Filter.Where(x => x.Value is CtrlType)
        .ToDictionary(x => x.Key, x => x.Value);

    #endregion

    #endregion

    #region Method

    public static MidiData Convert(ConvertType convertType, MidiData source)
    {
        MidiData result = new();
        Convert(convertType, result);
        return result;
    }

    public static void Convert(ConvertType convertType, MidiData source, MidiData result)
    {
        result.Initialize();
        result.FilePath = source.FilePath;
        result.Division = source.Division;
        result.ConvertType = convertType;

        result.Origine = new MidiData(source.Division);
        foreach (var track in source.Tracks)
        {
            result.Origine.AddTrack(track);
        }
        result.Origine.Organize();

        if (convertType == ConvertType.Instrument)
        {
            SplitChannelInstrment(source, ref result);
        }
        else
        {

            if (source.Format == DataFormat.Format0 & convertType == ConvertType.Format0)
                Copy(source, ref result);
            else if (source.Format == DataFormat.Format0 & convertType == ConvertType.Format1)
                ChannelSeparate(source, ref result);
            else if (source.Format == DataFormat.Format1 & convertType == ConvertType.Format0)
                MergeTrack(source, ref result);
            else if (source.Format == DataFormat.Format1 & convertType == ConvertType.Format1)
                Copy(source, ref result);
            else
                throw new ArgumentException($"{MethodBase.GetCurrentMethod()}");

            result.Organize();
            result.Tracks.ToList().ForEach(tr => tr.SetFilter(Def.InitFilter.Select(x => x.Key).ToArray()));
        }
    }

    public static void SaveConfig()
    {
        Def.Setting.FilterKeys.Clear();
        Def.Setting.FilterKeys.AddRange(Def.Filter.Keys);

        var rootPath = Path.Combine(ConverterSetting.Root, CONFIG_ROOT);
        if (!Directory.Exists(rootPath)) Directory.CreateDirectory(rootPath);

        var options = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            Converters = { new JsonStringEnumConverter() },
            WriteIndented = true
        };

        string? jsonString = JsonSerializer.Serialize(Def.Setting, options);

        var filePath = Path.Combine(rootPath, CONFIG_NAME);
        File.WriteAllText(filePath, jsonString);
    }

    private static void LoadConfig()
    {
        var filePath = Path.Combine(ConverterSetting.Root, CONFIG_ROOT, CONFIG_NAME);

        if (File.Exists(filePath))
        {
            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() },
            };

            Def.Setting =
                JsonSerializer.Deserialize<ConverterSetting>(File.ReadAllText(filePath), options)
                ?? throw new ArgumentException($"{MethodBase.GetCurrentMethod()}");
        }

        SetFilters();
    }

    private static void SetFilters()
    {
        Def.FilterTargetList.Clear();

        typeof(MidiMessage).Assembly.GetTypes()
            .Where(x =>
                !x.IsAbstract &&
                (
                    (x.IsSubclassOf(typeof(MetaMessage)) && !x.Equals(typeof(EndOfTrack))) ||
                    (x.IsSubclassOf(typeof(ChannelVoiceMessage)) && !x.Equals(typeof(ControlChange)))
                )
            )
            .ToList().ForEach(x =>
            {
                string? name = x.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName;
                if (name != null) Def.FilterTargetList.Add(name, x);
            });

        Enum.GetValues<SysExType>()
            .ToList().ForEach(x =>
            {
                string? name = x.GetDisplayAttribute()?.Name ?? $"{x}";
                if (name != null) Def.FilterTargetList.Add(name, x);
            });

        Enum.GetValues<CtrlType>()
            .ToList().ForEach(x =>
            {
                string? name = Enum.GetName(x);
                if (name != null) Def.FilterTargetList.Add(name, x);
            });

        Def.InitFilter.Clear();
        Def.Filter.Clear();
        foreach (string name in Def.Setting.FilterKeys)
        {
            if (Def.FilterTargetList.TryGetValue(name, out var msg))
            {
                Def.InitFilter.Add(name, msg);
                Def.Filter.Add(name, msg);
            }
        }
    }

    private static void Copy(MidiData source, ref MidiData midiData)
    {
        foreach (ITrack tr in source.Tracks)
        {
            ITrack track = midiData.NewTrack(tr.GetType());
            track.EventAddRange(tr.Events);
            track.EventAdd(0, new EndOfTrack());
            midiData.AddTrack(track);
        }
    }

    private static void ChannelSeparate(MidiData midiData, ref MidiData result)
    {
        foreach (var item in midiData.GetAllEvents().OrderBy(x => x.Channel).ThenBy(x => x.AbsoluteTick).ThenBy(x => x.Seqnum).GroupBy(x => x.Channel))
        {
            ITrack track = result.NewTrack<Track>();
            track.EventAddRange(item);
            track.EventAdd(0, new EndOfTrack());
            result.AddTrack(track);
        }
    }

    private static void MergeTrack(MidiData midiData, ref MidiData result)
    {
        ITrack track = result.NewTrack<Track>();

        foreach (var ev in midiData.GetAllEvents().OrderBy(x => x.AbsoluteTick).ThenBy(x => x.Channel).ThenBy(x => x.Seqnum))
        {
            if (ev.Message is EndOfTrack) continue;
            track.EventAdd(ev with { Parent = track, Message = ev.Message with { } });
        }
        track.EventAdd(0, new EndOfTrack());
        result.AddTrack(track);
    }

    private static void SplitChannelInstrment(MidiData midiData, ref MidiData result)
    {
        // 全トラックをチャンンネル別に振り分ける
        var byChannel =
            midiData.Tracks.Where(x => x is Track)
            .SelectMany(tr => tr.Events)
            .OrderBy(ev => ev.AbsoluteTick).ThenBy(ev => ev.Seqnum)
            .GroupBy(ev => ev.Channel)
            .ToArray();

        if (Def.Setting.CreateCodeTrack && midiData.GetTrack(0).Events.Any(x => x.Message is XFStyleCode))
        {
            result.AddTrack(GetCodeTrack(midiData, ref result, midiData.GetTrack(0).Events.ToList()));
        }

        foreach (var events in byChannel.OrderBy(x => x.Key))
        {
            bool needQueue = false;
            Queue<MidiEvent> buff = new();

            ITrack track = result.NewTrack<Track>();

            foreach (var ev in events)
            {
                bool isNoteEvent = ev.Message is NoteOn || ev.Message is NoteOff;

                needQueue |=
                    (ev.Message is ControlChange msb && msb.CtrlType == CtrlType.BankMSB) ||
                    (ev.Message is ControlChange lsb && lsb.CtrlType == CtrlType.BankLSB) ||
                    (ev.Message is ProgramChange);

                if (isNoteEvent == false && needQueue)
                {
                    buff.Enqueue(ev);
                    continue;
                }

                if (isNoteEvent && buff.Any())
                {
                    if (track.Events.Any())
                    {
                        result.AddTrack(track);
                        track = result.NewTrack<Track>();
                    }
                    while (buff.Any()) track.EventAdd(buff.Dequeue());
                    needQueue = false;
                    buff.Clear();
                }

                track.EventAdd(ev);
            }

            if (buff.Any(x => x.Message is ProgramChange))
            {
                while (buff.Any()) track.EventAdd(buff.Dequeue());
            }
            buff.Clear();

            if (track.Events.Any())
            {
                result.AddTrack(track);
            }
        }

        result.Organize();
        result.Tracks.ToList().ForEach(tr => tr.SetFilter(Def.InitFilter.Select(x => x.Key).ToArray()));

        foreach (var track in result.Tracks.ToList())
        {
            if (track.Channel == 0 || track.Events.Any(x => x.Message is NoteOn))
            {
                continue;
            }
            result.RemoveTrack(track);
        }

        var tracks = result.Tracks.ToList();

        result.ClearTracks();

        foreach (var channel in tracks.OrderBy(x => x.Channel).GroupBy(x => x.Channel))
        {
            if (channel.Key == 0 || channel.Count() == 1)
            {
                result.AddTrack(channel.First());
                continue;
            }

            foreach (var inst in channel.GroupBy(x => x.InstInfo))
            {
                if (inst.Count() == 1)
                {
                    result.AddTrack(inst.First());
                    continue;
                }

                ITrack newTrack = result.NewTrack<Track>();
                ;
                foreach (var item in inst)
                {
                    newTrack.EventAddRange(item.Events);
                }
                result.AddTrack(newTrack);
            }
        }

        result.Organize();
        result.Tracks.ToList().ForEach(tr => tr.SetFilter(Def.InitFilter.Select(x => x.Key).ToArray()));
    }

    private static ITrack GetCodeTrack(MidiData midiData, ref MidiData result, List<MidiEvent> events)
    {
        ITrack track = result.NewTrack<Track>();
        List<byte> buff = new();
        long abstick = 0;

        track.IsCodeTrack = true;

        track.EventAdd(new MidiEvent(track) { AbsoluteTick = 0, Message = new SequenceTrackName("#Code Track#") });

        foreach (var ev in events)
        {
            if (ev.Message is XFStyleCode code)
            {
                if (buff.Any())
                {
                    foreach (var v in buff)
                    {
                        track.EventAdd(ev with { AbsoluteTick = ev.AbsoluteTick - 1, Message = new NoteOff(1, v, 0) });
                    }
                }
                buff.Clear();
                buff.AddRange(code.GetCodeVoicing());
                foreach (var v in buff)
                {
                    track.EventAdd(ev with { AbsoluteTick = ev.AbsoluteTick, Message = new NoteOn(1, v, 100) });
                }
            }
            abstick = ev.AbsoluteTick;
        }

        if (buff.Any())
        {
            if (midiData.Tracks.Select(x => x.Events.LastOrDefault()).OrderBy(x => x?.AbsoluteTick).LastOrDefault() is MidiEvent last)
            {
                foreach (var v in buff)
                {
                    track.EventAdd(new MidiEvent(track) { AbsoluteTick = last.AbsoluteTick, Message = new NoteOff(1, v, 0) });
                }
            }
        }

        return track;
    }

    public static void SaveMidiData(MidiData midiData, SMFEncode encode = SMFEncode.UTF8, bool filter = true)
    {
        Def.Setting.Encode = encode;

        var tracks = midiData.Tracks.Where(x => x.Output).ToArray();

        if (!tracks.Any()) throw new ArgumentException($"{MethodBase.GetCurrentMethod()}");

        MidiData result = new()
        {
            FilePath = midiData.FilePath,
            Division = midiData.Division,
        };

        foreach (ITrack tr in tracks)
        {
            if (filter)
            {
                tr.DoFilter();
                ITrack track = result.NewTrack(tr.GetType());
                track.EventAddRange(tr.FilterdEvents);
                result.AddTrack(track);
            }
            else
            {
                ITrack track = result.NewTrack(tr.GetType());
                track.EventAddRange(tr.Events);
                result.AddTrack(track);
            }
        }

        result.Organize();

        File.WriteAllBytes(result.FilePath, result.GetByte());
    }

    public static void SaveSetMidiData(MidiData midiData, string folderPath)
    {
        SaveConfig();

        string folderName = Path.GetFileNameWithoutExtension(folderPath);

        List<int> output = new();
        midiData.NumberOfTrack.RangeEach(x => { if (midiData.GetTrack(x).Output) output.Add(x); });

        string pathMID = Path.Combine(folderPath, $"{folderName}.mid");
        string pathNOPC = Path.Combine(folderPath, $"{folderName}_NOPC.mid");
        string pathVocal = Path.Combine(folderPath, $"{folderName}_Vocal.mid");
        string pathSRT = Path.Combine(folderPath, $"{folderName}_Lyric.srt");
        string pathTEXT = Path.Combine(folderPath, $"{folderName}_Lyric.txt");

        // .mid
        midiData.Tracks.ToList().ForEach(tr => tr.Output = true);

        midiData.FilePath = pathMID;
        SaveMidiData(midiData, Def.Setting.Encode, false);

        // _NOPC.mid
        midiData.Tracks.ToList().ForEach(tr => tr.Output = true);

        Def.Setting.LyricAdustment = true;
        Def.Setting.LyricPaddingPlus = false;
        Def.Setting.RemoveProgramChange = true;

        midiData.FilePath = pathNOPC;
        SaveMidiData(midiData);

        // _Vocal.mid
        midiData.Tracks.ToList().ForEach(tr => tr.Output = tr.LyricMatched);
        midiData.GetTrack(0).Output = true;

        Def.Setting.LyricAdustment = true;
        Def.Setting.LyricPaddingPlus = true;
        Def.Setting.RemoveProgramChange = false;

        midiData.FilePath = pathVocal;
        SaveMidiData(midiData);

        // .srt, .txt
        if (midiData.Origine?.Tracks.FirstOrDefault(x => x is XFKaraokeMessage) is XFKaraokeMessage karaoke)
        {
            var srt = karaoke.GetSRT(Def.Setting.SRTOffset, Def.Setting.SRTRemoveComment);
            if (srt.Length > 0)
            {
                File.WriteAllText(pathSRT, srt);
            }

            var txt = karaoke.Lyric;
            if (txt.Length > 0)
            {
                File.WriteAllText(pathTEXT, txt);
            }
        }

        //
        midiData.NumberOfTrack.RangeEach(x => midiData.GetTrack(x).Output = output.Contains(x));
        LoadConfig();
    }

    public static void SaveSepalateMidiData(MidiData midiData, string folderPath)
    {
        SaveConfig();

        int num = 0;
        foreach (var track in midiData.Tracks)
        {
            MidiData sepalated = new() { MidiStd = midiData.MidiStd };

            sepalated.FilePath = Path.Combine(folderPath, $"#{num} {track}.mid");
            sepalated.ConvertType = midiData.ConvertType;

            ITrack newTrack = sepalated.AddNewTrack<Track>();
            Def.Filter.ToList().ForEach(x => newTrack.Filter.Add(x.Key, x.Value));

            track.Events.ToList().ForEach(e => newTrack.EventAdd(e with { AbsoluteTick = e.AbsoluteTick }));

            sepalated.Organize();

            SaveMidiData(sepalated, SMFEncode.SJIS);

            num++;
        }

        LoadConfig();
    }

    #endregion
}
