using System.Reflection;
using System.Text.Json;
using libMidi.SMF.enums;

namespace libMidi.SMF;

public static class InstMap
{
    const string INSTFILE_ROOT = @"Config\Instrument";

    public static Dictionary<MidiStd, Dictionary<string, Dictionary<string, Dictionary<string, string>>>> Map
    {
        get
        {
            if (_Map == null)
            {
                _Map ??= new();
                Initialize();
            }
            return _Map;
        }
    }

    private static Dictionary<MidiStd, Dictionary<string, Dictionary<string, Dictionary<string, string>>>> _Map = null!;

    public static void Initialize(string? path = null)
    {
        string rootPath = path ?? Path.Combine(ConverterSetting.Root, INSTFILE_ROOT);

        if (!Directory.Exists(rootPath)) Directory.CreateDirectory(rootPath);

        Dictionary<MidiStd, string> files = new()
        {
            { MidiStd.GM, @"GM.json" },
            { MidiStd.GS, @"GS.json" },
            { MidiStd.XG, @"XG.json" },
        };

        foreach (var file in files)
        {
            string filePath = Path.Combine(rootPath, file.Value);
            if (File.Exists(filePath))
            {
                var obj = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(File.ReadAllText(filePath));
                if (obj != null) Map.Add(file.Key, obj);
            }
        }
    }

    public static Dictionary<string, Dictionary<string, string>> GetInstrumentDefinitions(MidiStd std)
    {
        return Map[std == MidiStd.GM2 ? MidiStd.GM : std]["Instrument Definitions"];
    }

    public static Dictionary<string, string> GetPatchNames(MidiStd std, string patchNameKey)
    {
        return Map[std == MidiStd.GM2 ? MidiStd.GM : std]["Patch Names"][patchNameKey];
    }

    public static Dictionary<string, string> GetNoteNames(MidiStd std, string noteNameKey)
    {
        var c = new Dictionary<string, string>();
        foreach (var item in Map[std == MidiStd.GM2 ? MidiStd.GM : std]["Note Names"][noteNameKey])
        {
            if (item.Key != "BasedOn")
            {
                c.Add(item.Key, item.Value);
            }
        }

        if (Map[std == MidiStd.GM2 ? MidiStd.GM : std]["Note Names"][noteNameKey].TryGetValue("BasedOn", out string? value))
        {
            var b = Map[std == MidiStd.GM2 ? MidiStd.GM : std]["Note Names"][noteNameKey]["BasedOn"] ?? string.Empty;
            foreach (var item in GetNoteNames(std, b))
            {
                if (!c.ContainsKey(item.Key))
                {
                    c.Add(item.Key, item.Value);
                }
            }
            ;
        }

        return c;
    }

    public static Dictionary<string, string> GetDrumPatchs(MidiStd std)
    {
        return
            GetInstrumentDefinitions(std)[GetDrumDefKey(std)]
            .Where(x => x.Key.StartsWith("Patch[") & !x.Key.StartsWith("Patch[*]")).ToDictionary(x => x.Key, x => x.Value);
    }

    private static string GetDrumDefKey(MidiStd std)
    {
        return
            std switch
            {
                MidiStd.GM => "General MIDI Level 2 Drumsets",
                MidiStd.GM2 => "General MIDI Level 2 Drumsets",
                MidiStd.GS => "Roland SC-8850 Drumsets",
                MidiStd.XG => "YAMAHA MU1000/MU2000 Drumsets",
                _ => throw new NotImplementedException(),
            };
    }

    public static string? GetDrumKeyName(MidiStd std, string msblsb, string pc)
    {
        var key = std == MidiStd.GM ? "120" : msblsb;

        return
            GetInstrumentDefinitions(std)[GetDrumDefKey(std)]
            .FirstOrDefault(x => x.Key == $"Key[{key},{pc}]").Value;
    }

    #region インストゥルメント名取得

    public static string GetInstName(InstInfo instrument, bool isDrum)
    {
        return instrument.MidiStd switch
        {
            MidiStd.GM => GetInstNameGM(instrument, isDrum),
            MidiStd.GM2 => GetInstNameGM(instrument, isDrum),
            MidiStd.GS => GetInstNameGS(instrument, isDrum),
            MidiStd.XG => GetInstNameXG(instrument, isDrum),
            _ => throw new ArgumentException($"{MethodBase.GetCurrentMethod()}"),
        };
    }

    private static string GetInstNameGM(InstInfo inst, bool isDrum)
    {
        if (!Map.ContainsKey(MidiStd.GM))
        {
            return inst.ToString();
        }

        if (!isDrum)
        {
            if (Map[MidiStd.GM]["Instrument Definitions"]["General MIDI Level 1"].TryGetValue($"Patch[0]", out string? bankname))
            {
                if (Map[MidiStd.GM]["Patch Names"].TryGetValue(bankname, out Dictionary<string, string>? patchNames))
                {
                    if (patchNames.TryGetValue($"{inst.PgNum}", out string? instrumentname))
                        return instrumentname;
                }
            }
            else
            {
                if (Map[MidiStd.GM]["Patch Names"]["General MIDI Level 2 Var #00"].TryGetValue($"{inst.PgNum}", out string? instrumentname))
                    return instrumentname;
            }
        }
        else
        {
            if (Map[MidiStd.GM]["Instrument Definitions"]["General MIDI Level 1 Drumsets"].TryGetValue($"Patch[{inst.BankLSB}]", out string? bankname))
            {
                if (Map[MidiStd.GM]["Patch Names"][bankname].TryGetValue($"{inst.PgNum}", out string? kitname))
                    return $"GM {kitname}";
            }
        }

        return inst.ToString();
    }

    private static string GetInstNameGS(InstInfo inst, bool isDrum)
    {
        if (!Map.ContainsKey(MidiStd.GS))
        {
            return inst.ToString();
        }

        if (!isDrum)
        {
            if (Map[MidiStd.GS]["Instrument Definitions"]["Roland SC-8850"].TryGetValue($"Patch[{inst.BankMSB * 128 + inst.BankLSB}]", out string? bankname))
            {
                if (Map[MidiStd.GS]["Patch Names"].TryGetValue(bankname, out Dictionary<string, string>? patchNames))
                {
                    if (patchNames.TryGetValue($"{inst.PgNum}", out string? instrumentname))
                    {
                        return instrumentname;
                    }
                }
            }
            else
            {
                if (Map[MidiStd.GS]["Patch Names"]["Roland SC-8850 Capital Tones"].TryGetValue($"{inst.PgNum}", out string? instrumentname))
                {
                    return instrumentname;
                }
            }
        }
        else
        {
            if (Map[MidiStd.GS]["Instrument Definitions"]["Roland SC-8850 Drumsets"].TryGetValue($"Patch[{inst.BankLSB}]", out string? bankname))
            {
                if (Map[MidiStd.GS]["Patch Names"][bankname].TryGetValue($"{inst.PgNum}", out string? kitname))
                {
                    return $"GS {kitname}";
                }
            }
        }

        return inst.ToString();
    }

    private static string GetInstNameXG(InstInfo inst, bool isDrum)
    {
        if (!Map.ContainsKey(MidiStd.XG))
        {
            return inst.ToString();
        }

        if (!isDrum)
        {
            if (Map[MidiStd.XG]["Instrument Definitions"]["YAMAHA MU1000/MU2000"].TryGetValue($"Patch[{inst.BankMSB * 128 + inst.BankLSB}]", out string? bankname))
            {
                if (Map[MidiStd.XG]["Patch Names"].TryGetValue(bankname, out Dictionary<string, string>? patchNames))
                {
                    if (patchNames.TryGetValue($"{inst.PgNum}", out string? instrumentname))
                        return instrumentname;
                }
            }
            else
            {
                if (Map[MidiStd.XG]["Patch Names"]["YAMAHA MU1000/MU2000 #000 MU100 Native"].TryGetValue($"{inst.PgNum}", out string? instrumentname))
                {
                    return instrumentname;
                }
            }
        }
        else
        {
            byte msb = (byte)(inst.BankMSB == 0 ? 127 : inst.BankMSB);
            if (Map[MidiStd.XG]["Instrument Definitions"]["YAMAHA MU1000/MU2000 Drumsets"].TryGetValue($"Patch[{msb * 128}]", out string? bankname))
            {
                if (Map[MidiStd.XG]["Patch Names"][bankname].TryGetValue($"{inst.PgNum}", out string? kitname))
                    return $"XG {kitname}";
            }
        }

        return inst.ToString();
    }

    #endregion

    #region ドラムノート名取得

    public static string? GetDrumNoteName(InstInfo inst, byte pitch)
    {
        return inst.MidiStd switch
        {
            MidiStd.GM => GetDrumNoteNameGM(inst, pitch),
            MidiStd.GM2 => GetDrumNoteNameGM(inst, pitch),
            MidiStd.GS => GetDrumNoteNameGS(inst, pitch),
            MidiStd.XG => GetDrumNoteNameXG(inst, pitch),
            _ => throw new ArgumentException($"{MethodBase.GetCurrentMethod()}"),
        };
    }

    private static string? GetDrumNoteNameGM(InstInfo inst, byte pitch)
    {
        if (!Map.ContainsKey(MidiStd.GM)) return null;

        var map = Map[MidiStd.GM];

        string bank = map["Instrument Definitions"]["General MIDI Level 2 Drumsets"].TryGetValue($"Key[120,{inst.BankLSB}]", out string? bnk) ? bnk : "General MIDI Level 2 STANDARD Set";

        if (map["Note Names"][bank].TryGetValue($"{pitch}", out string? note1))
        {
            return note1;
        }

        if (map["Note Names"]["General MIDI Level 2 STANDARD Set"].TryGetValue($"{pitch}", out string? note2))
        {
            return note2;
        }

        return null;
    }

    private static string? GetDrumNoteNameGS(InstInfo inst, byte pitch)
    {
        if (!Map.ContainsKey(MidiStd.GS)) return null;

        var map = Map[MidiStd.GS];

        string bank = map["Instrument Definitions"]["Roland SC-8850 Drumsets"].TryGetValue($"Key[{inst.BankMSB},{inst.BankLSB}]", out string? bnk) ? bnk : "Roland SC-8850 STANDARD 1 Set";

        if (map["Note Names"][bank].TryGetValue($"{pitch}", out string? note1))
        {
            return note1;
        }

        if (map["Note Names"]["Roland SC-8850 STANDARD 1 Set"].TryGetValue($"{pitch}", out string? note2))
        {
            return note2;
        }

        return null;
    }

    private static string? GetDrumNoteNameXG(InstInfo inst, byte pitch)
    {
        if (!Map.ContainsKey(MidiStd.XG)) return null;

        var map = Map[MidiStd.XG];

        string bank = map["Instrument Definitions"]["YAMAHA MU1000/MU2000 Drumsets"].TryGetValue($"Key[{inst.BankMSB * 128},{inst.BankLSB}]", out string? bnk) ? bnk : "YAMAHA MU1000/MU2000 #128 StandKit Mu Basic";

        if (map["Note Names"][bank].TryGetValue($"{pitch}", out string? note1))
        {
            return note1;
        }

        if (map["Note Names"]["YAMAHA MU1000/MU2000 #128 StandKit Mu Basic"].TryGetValue($"{pitch}", out string? note2))
        {
            return note2;
        }

        return null;
    }

    #endregion
}
