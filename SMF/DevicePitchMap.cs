using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using libMidi.SMF.interfaces;

namespace libMidi.SMF;

public class DevicePitchMap
{
    public Dictionary<byte, Dictionary<string, byte>> Map { get; set; } = new();

    [JsonIgnore()]
    private static string FileName =>
        Path.Combine(ConverterSetting.Root, @"Config\DevicePitchMap.json");

    [JsonIgnore()]
    private static DevicePitchMap Def
    {
        get
        {
            if (_Def == null) Initialize();
            return _Def ?? throw new ArgumentException($"{MethodBase.GetCurrentMethod()}");
        }
    }

    [JsonIgnore()]
    private static DevicePitchMap _Def = null!;

    private static void Initialize()
    {
        if (File.Exists(FileName))
        {
            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() },
            };

            _Def =
                JsonSerializer.Deserialize<DevicePitchMap>(File.ReadAllText(FileName), options)
                ?? throw new ArgumentException($"{MethodBase.GetCurrentMethod()}");
        }
        _Def ??= new();
    }

    public static void Regist()
    {
        var options = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() },
        };

        var json = JsonSerializer.Serialize(Def, options);

        File.WriteAllText(FileName, json);
    }

    public static void Regist(ITrack track)
    {
        foreach (var item in track.Drum)
        {
            if (item.Value.DevicePitch != null)
                SetDevicePitch(item.Key, item.Value.DeviceName, item.Value.DevicePitch);
        }
        Regist();
    }

    private static void SetDevicePitch(byte key, string deviceName, byte? devicePitch)
    {
        if (Def.Map.ContainsKey(key)) Def.Map[key].Remove(deviceName);
        if (devicePitch == null) return;
        if (!Def.Map.ContainsKey(key)) Def.Map.Add(key, new Dictionary<string, byte>());
        Def.Map[key].Add(deviceName, (byte)devicePitch);
    }

    public static byte? GetDevicePitch(byte instPitch, string devName)
    {
        if (!Def.Map.ContainsKey(instPitch)) return null;
        return Def.Map[instPitch].TryGetValue(devName, out byte result) ? result : null;
    }
}
