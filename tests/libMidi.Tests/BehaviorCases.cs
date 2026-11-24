using System.Text;
using System.Text.Json;
using libMidi;
using libMidi.Messages;
using libMidi.Messages.enums;
using libMidi.SMF;
using libMidi.SMF.enums;

namespace LibMidi.Tests;

internal static class BehaviorCases
{
    #region Methods

    internal static object Run(string name, string root) => name switch
    {
        "T01" => new[] { Describe(SMFLoader.Load(Fixtures.Format0(root))), Describe(SMFLoader.Load(Fixtures.Format1(root))) },
        "T02" => SaveRoundTrips(root),
        "T03" => new[]
        {
            ConvertMidi(ConvertType.Format1, Fixtures.Format0(root)),
            ConvertMidi(ConvertType.Format0, Fixtures.Format1(root)),
            ConvertMidi(ConvertType.Format0, Fixtures.Format0(root)),
            ConvertMidi(ConvertType.Format1, Fixtures.Format1(root))
        },
        "T04" => new[]
        {
            ConvertMidi(ConvertType.Instrument, InstrumentFixture(root)),
            ConvertMidi(ConvertType.MultiTimber, InstrumentFixture(root))
        },
        "T05" => Describe(SMFLoader.Load(Fixtures.Format0(root))),
        "T06" => TimeAndVlq(root),
        "T07" => TextEncodings(root),
        "T08" => InstrumentMaps(root),
        "T09" => Config(root),
        "T09-read" => DescribeConfig(),
        "T10" => Karaoke(root),
        "T11" => Paths(root),
        "T11-current" => Paths(root, verifySafeNames: true),
        "T12" => ApiSurface.Describe(typeof(MidiData).Assembly),
        "T13" => UnregisteredCodePages(root),
        "T13-current" => VerifyUnregisteredCodePages(root),
        "known-length" => new[]
        {
            RecordLength(new MetaText(Array.Empty<byte>())),
            RecordLength(new MetaText([0x80])),
            RecordLength(SequencerSpecific.GetNew([0x80, 0x01]))
        },
        "known-system" => new[]
        {
            Capture(() => Convert.ToHexString(new TuneRequest().GetByte())),
            Capture(() => Convert.ToHexString(new MidiStart().GetByte()))
        },
        "known-inst-map" => ReinitializeInstrumentMap(),
        _ => throw new ArgumentException($"Unknown case {name}.", nameof(name))
    };

    private static object Describe(MidiData midi) => new
    {
        Format = midi.Format.ToString(),
        midi.Division,
        midi.NumberOfTrack,
        MidiStd = midi.MidiStd.ToString(),
        Tracks = midi.Tracks.Select(track => new
        {
            Type = track.GetType().Name,
            track.Channels,
            track.IsDrum,
            Events = track.Events.Select(DescribeEvent).ToArray()
        }).ToArray()
    };

    private static object DescribeEvent(MidiEvent midiEvent) => new
    {
        Tick = midiEvent.AbsoluteTick,
        midiEvent.Channel,
        Type = midiEvent.Message.GetType().Name,
        Bytes = Convert.ToHexString(midiEvent.Message.GetByte()),
        Text = (midiEvent.Message as MetaTextBase)?.Text,
        Instrument = midiEvent.InstrumentInfo?.ToString()
    };

    private static object SaveRoundTrips(string root)
    {
        string input = Fixtures.Format1(root);
        return new[] { false, true }.Select(filter =>
        {
            MidiData midi = SMFLoader.Load(input);
            midi.FilePath = Path.Combine(root, filter ? "T02-filtered.mid" : "T02-full.mid");
            SMFConverter.SaveMidiData(midi, filter: filter);
            return Describe(SMFLoader.Load(midi.FilePath));
        }).ToArray();
    }

    private static object ConvertMidi(ConvertType type, string input)
    {
        var output = new MidiData();
        SMFConverter.Convert(type, SMFLoader.Load(input), output);
        return new { Conversion = type.ToString(), Result = Describe(output) };
    }

    private static string InstrumentFixture(string root) =>
        Fixtures.Write(root, "instruments.mid", 0, ("MTrk", $"""
            {Fixtures.Conductor}
            00 B0 00 01 00 B0 20 02 00 C0 05 00 90 3C 50 30 80 3C 40
            00 B0 00 03 00 B0 20 04 00 C0 0A 00 90 40 50 30 80 40 40
            00 C9 00 00 99 24 50 30 89 24 40 {Fixtures.End}
            """));

    private static object TimeAndVlq(string root)
    {
        string input = Fixtures.Write(root, "time.mid", 0, ("MTrk", """
            00 FF 51 03 07 A1 20 00 FF 58 04 04 02 18 08
            83 60 90 3C 40
            8B 20 FF 51 03 0F 42 40 00 FF 58 04 03 03 18 08
            81 70 80 3C 40 00 FF 2F 00
            """));
        MidiData midi = SMFLoader.Load(input);

        // One TimeSpan tick of tolerance accounts for floating-point conversion.
        if (midi.TempoMap.Count != 2 ||
            Math.Abs(midi.TempoMap[0].BPM - 120) > 0.0001 ||
            Math.Abs(midi.TempoMap[1].BPM - 60) > 0.0001 ||
            midi.GetAllEvents<NoteOff>().Single().Time.Subtract(TimeSpan.FromMilliseconds(2500)).Duration().Ticks > 1)
        {
            throw new InvalidDataException("Unexpected tempo or elapsed time.");
        }

        return new
        {
            Tempo = midi.TempoMap.Select(item => new { item.Tick, item.BPM, TimeTicks = item.Time.Ticks }).ToArray(),
            Positions = midi.GetAllEvents().Select(item =>
            {
                BreakedTime bar = item.BreakedTime();
                return new { item.AbsoluteTick, bar.Measure, bar.Beat, bar.Tick, bar.NN, bar.DD };
            }).ToArray(),
            Vlq = new[] { "7F", "81 00", "FF 7F", "81 80 00" }.Select(hex =>
            {
                byte[] bytes = Fixtures.Hex(hex);
                (long value, int length) = bytes.GetVariant();
                return new { Value = value, Length = length, Output = Convert.ToHexString(value.MidiVarToByte()) };
            }).ToArray()
        };
    }

    private static object TextEncodings(string root)
    {
        string input = Fixtures.Write(root, "encoding.mid", 0, ("MTrk", """
            00 FF 01 03 41 42 43
            00 FF 01 06 E6 97 A5 E6 9C AC
            00 FF 01 04 93 FA 96 7B
            00 FF 01 02 FA 40
            00 FF 01 02 C2 A9
            00 FF 01 00
            00 FF 01 01 82
            00 FF 01 04 F0 9F 98 80
            00 FF 2F 00
            """));
        MidiData midi = SMFLoader.Load(input);
        var read = midi.GetAllEvents<MetaText>().Select(e => new
        {
            e.Message.Text,
            Utf8 = Convert.ToHexString(e.Message.Data)
        }).ToArray();
        var roundTrips = new List<object>();
        foreach (SMFEncode encoding in new[] { SMFEncode.UTF8, SMFEncode.SJIS })
        {
            midi.FilePath = Path.Combine(root, $"T07-{encoding}.mid");
            SMFConverter.SaveMidiData(midi, encoding, filter: false);
            roundTrips.Add(new
            {
                Encoding = encoding.ToString(),
                Text = SMFLoader.Load(midi.FilePath).GetAllEvents<MetaText>().Select(e => e.Message.Text).ToArray()
            });
        }

        return new { Read = read, RoundTrips = roundTrips };
    }

    private static object InstrumentMaps(string root)
    {
        string defaultPath = Path.Combine(ConverterSetting.Root, "Config", "Instrument");
        string explicitPath = Path.Combine(root, "別の 定義");
        WriteInstrument(defaultPath, "GM", "General MIDI Level 1", "Acoustic Grand Piano");
        WriteInstrument(defaultPath, "GS", "Roland SC-8850", "GS Piano");
        object missing = new
        {
            Name = InstMap.GetInstName(new InstInfo(MidiStd.XG, 0, 0, 0), false),
            Definitions = Capture(() => InstMap.GetInstrumentDefinitions(MidiStd.XG).Count)
        };

        WriteInstrument(explicitPath, "XG", "YAMAHA MU1000/MU2000", "XG Piano");
        InstMap.Initialize(explicitPath);
        return new
        {
            GM = InstMap.GetInstName(new InstInfo(MidiStd.GM, 0, 0, 0), false),
            GS = InstMap.GetInstName(new InstInfo(MidiStd.GS, 0, 0, 0), false),
            XG = InstMap.GetInstName(new InstInfo(MidiStd.XG, 0, 0, 0), false),
            Missing = missing
        };
    }

    private static void WriteInstrument(string folder, string standard, string definition, string instrument)
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, standard + ".json"), JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["Instrument Definitions"] = new Dictionary<string, object>
            {
                [definition] = new Dictionary<string, string> { ["Patch[0]"] = "Fixture bank" }
            },
            ["Patch Names"] = new Dictionary<string, object>
            {
                ["Fixture bank"] = new Dictionary<string, string> { ["0"] = instrument }
            }
        }));
    }

    private static object Config(string root)
    {
        JsonElement defaults = JsonSerializer.SerializeToElement(SMFConverter.Def.Setting);
        string[] defaultFilter = SMFConverter.Def.Filter.Keys.ToArray();
        SMFConverter.Def.Setting.SRTOffset = 425;
        SMFConverter.Def.Setting.ChannelFix = false;
        SMFConverter.Def.Filter.Clear();
        SMFConverter.Def.Filter.Add("Expression", CtrlType.Expression);
        SMFConverter.SaveConfig();
        using JsonDocument saved = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(ConverterSetting.Root, "Config", "Config.json")));
        return new { Defaults = defaults, DefaultFilter = defaultFilter, Saved = saved.RootElement.Clone(), Root = Path.GetRelativePath(root, ConverterSetting.Root) };
    }

    private static object DescribeConfig() => new
    {
        SMFConverter.Def.Setting.SRTOffset,
        SMFConverter.Def.Setting.ChannelFix,
        Encode = SMFConverter.Def.Setting.Encode.ToString(),
        Filter = SMFConverter.Def.Filter.Keys.ToArray()
    };

    private static object Karaoke(string root)
    {
        string file = Fixtures.Write(root, "xf.mid", 1,
            ("MTrk", $"{Fixtures.Conductor} {Fixtures.End}"),
            ("XFIH", "00 FF 7F 07 43 7B 01 31 00 7F 7F 00 FF 2F 00"),
            ("XFKM", """
                83 60 FF 05 05 3C 41 5B 78 5D
                83 60 FF 05 04 42 5E 43 3E
                83 60 FF 05 02 2F 44
                83 60 FF 05 02 2F 45 00 FF 2F 00
                """));
        MidiData midi = SMFLoader.Load(file);
        XFKaraokeMessage track = midi.Tracks.OfType<XFKaraokeMessage>().Single();
        File.WriteAllText(Path.Combine(root, "T10-comments.srt"), track.GetSRT(0, false));
        File.WriteAllText(Path.Combine(root, "T10-clean.srt"), track.GetSRT(0, true));
        var converted = new MidiData();
        SMFConverter.Convert(ConvertType.Format1, midi, converted);
        SMFConverter.SaveSetMidiData(converted, root, "T10-export", mid: false, nopc: false, vocal: false);
        return new { Source = Describe(midi), Converted = Describe(converted), track.Lyric };
    }

    private static object Paths(string root, bool verifySafeNames = false)
    {
        string folder = Path.Combine(root, "日本語 と 空白");
        Directory.CreateDirectory(folder);
        MidiData normal = SMFLoader.Load(Fixtures.Format0(folder));
        SMFConverter.SaveSepalateMidiData(normal, folder);
        string split = Path.Combine(folder, "#0 Tune.mid");
        File.Copy(split, Path.Combine(root, "T11-split.mid"));
        if (verifySafeNames)
        {
            VerifySafeTrackNames(folder);
            return Describe(SMFLoader.Load(split));
        }

        string separator = Fixtures.Write(folder, "separator.mid", 0, ("MTrk", """
            00 FF 03 03 41 2F 42 00 90 3C 40 00 FF 2F 00
            """));
        object missingDirectory = Capture(() => SaveSplit(separator, folder));
        Directory.CreateDirectory(Path.Combine(folder, "#0 A"));
        object existingDirectory = Capture(() => SaveSplit(separator, folder));
        return new { Normal = Describe(SMFLoader.Load(split)), missingDirectory, existingDirectory };
    }

    private static void VerifySafeTrackNames(string folder)
    {
        string output = Path.Combine(folder, "separate");
        Directory.CreateDirectory(output);
        var midi = new MidiData(480);
        string[] names = ["A/B", @"A\B", "日本語 と 空白", "../逃走", "<>:\"|?*\u0001\u001f"];
        foreach (string name in names)
        {
            var track = midi.AddNewTrack<Track>();
            track.EventAdd(0, new SequenceTrackName(name));
            track.EventAdd(0, new NoteOn(1, 60, 64));
            track.EventAdd(480, new EndOfTrack());
        }

        SMFConverter.SaveSepalateMidiData(midi, output);
        string[] expected = ["#0 A_B.mid", "#1 A_B.mid", "#2 日本語 と 空白.mid", "#3 .._逃走.mid", "#4 _________.mid"];
        string?[] actual = Directory.EnumerateFileSystemEntries(output).Select(Path.GetFileName)
            .Order(StringComparer.Ordinal).ToArray();
        if (!actual.SequenceEqual(expected, StringComparer.Ordinal))
        {
            throw new InvalidDataException($"Unexpected split output: {string.Join(", ", actual)}");
        }
    }

    private static object SaveSplit(string input, string folder)
    {
        SMFConverter.SaveSepalateMidiData(SMFLoader.Load(input), folder);
        return File.Exists(Path.Combine(folder, "#0 A", "B.mid"));
    }

    private static object UnregisteredCodePages(string root)
    {
        string utf8 = Fixtures.Write(root, "utf8.mid", 0, ("MTrk", "00 FF 01 03 E6 97 A5 00 FF 2F 00"));
        string cp932 = Fixtures.Write(root, "cp932.mid", 0, ("MTrk", "00 FF 01 02 93 FA 00 FF 2F 00"));
        var midi = new MidiData(480) { FilePath = Path.Combine(root, "unregistered.mid") };
        midi.AddNewTrack<Track>().EventAdd(0, new MetaText("日"));
        return new
        {
            Provider = Capture(() => Encoding.GetEncoding(932).CodePage),
            Utf8Decode = Capture(() => Encoding.UTF8.GetString(Fixtures.Hex("E6 97 A5").EncodeUTF8())),
            Cp932Decode = Capture(() => Encoding.UTF8.GetString(Fixtures.Hex("93 FA").EncodeUTF8())),
            Utf8Load = Capture(() => Describe(SMFLoader.Load(utf8))),
            Cp932Load = Capture(() => Describe(SMFLoader.Load(cp932))),
            Cp932Save = Capture(() =>
            {
                SMFConverter.SaveMidiData(midi, SMFEncode.SJIS, false);
                return Convert.ToHexString(File.ReadAllBytes(midi.FilePath));
            })
        };
    }

    private static object VerifyUnregisteredCodePages(string root)
    {
        const string text = "日";
        const string cp932Track = "00 FF 01 02 93 FA 00 FF 2F 00";
        AssertProviderIsUnregistered();

        string utf8 = Fixtures.Write(root, "utf8.mid", 0, ("MTrk", "00 FF 01 03 E6 97 A5 00 FF 2F 00"));
        string cp932 = Fixtures.Write(root, "cp932.mid", 0, ("MTrk", cp932Track));
        var midi = new MidiData(480) { FilePath = Path.Combine(root, "unregistered.mid") };
        midi.AddNewTrack<Track>().EventAdd(0, new MetaText(text));
        SMFConverter.SaveMidiData(midi, SMFEncode.SJIS, false);

        foreach (string file in new[] { utf8, cp932, midi.FilePath })
        {
            MidiData loaded = SMFLoader.Load(file);
            if (loaded.NumberOfTrack != 1 || loaded.GetAllEvents<MetaText>().Single().Message.Text != text)
            {
                throw new InvalidDataException($"Unexpected decoded text in {Path.GetFileName(file)}.");
            }
        }

        // SaveMidiData copies events without EOT for this text-only track.
        string expected = Fixtures.Write(root, "expected.mid", 0, ("MTrk", "00 FF 01 02 93 FA"));
        if (!File.ReadAllBytes(expected).AsSpan().SequenceEqual(File.ReadAllBytes(midi.FilePath)))
        {
            throw new InvalidDataException("CP932 output differs from the hand-authored SMF fixture.");
        }

        AssertProviderIsUnregistered();
        return Describe(SMFLoader.Load(midi.FilePath));
    }

    private static void AssertProviderIsUnregistered()
    {
        try
        {
            _ = Encoding.GetEncoding(932);
        }
        catch (NotSupportedException)
        {
            return;
        }

        throw new InvalidOperationException("Code page 932 is registered globally in the probe.");
    }

    private static object Capture(Func<object> run)
    {
        try
        {
            return new { Value = run() };
        }
        catch (Exception error) when (error is NotSupportedException or ArgumentException or
                                      DirectoryNotFoundException or KeyNotFoundException or IndexOutOfRangeException or NotImplementedException)
        {
            return new { Exception = error.GetType().Name };
        }
    }

    private static object RecordLength(MidiMessage message) => new
    {
        Type = message.GetType().Name,
        Declared = Capture(() => message.Length),
        Bytes = message.GetByte().Length
    };

    private static object ReinitializeInstrumentMap()
    {
        WriteInstrument(Path.Combine(ConverterSetting.Root, "Config", "Instrument"),
            "GM", "General MIDI Level 1", "Acoustic Grand Piano");
        int count = InstMap.Map.Count;
        return new { Count = count, Repeat = Capture(() => { InstMap.Initialize(); return InstMap.Map.Count; }) };
    }

    #endregion
}
