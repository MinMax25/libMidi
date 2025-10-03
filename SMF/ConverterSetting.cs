using libMidi.SMF.enums;

namespace libMidi.SMF;

public class ConverterSetting
{
    public static string Root
    {
        get => _Root == null ? AppDomain.CurrentDomain.BaseDirectory : _Root;
        set => _Root = value;
    }
    private static string? _Root;

    public bool ChannelFix { get; set; }

    public bool InsertTrackName { get; set; }

    public bool RemoveProgramChange { get; set; }

    public bool ReplaceNoteOn { get; set; }

    public bool CreateCodeTrack { get; set; }

    public bool LyricAdustment { get; set; }

    public bool LyricPaddingPlus { get; set; }

    public int SRTOffset { get; set; }

    public bool SRTRemoveComment { get; set; }

    public bool XFStyleConvert { get; set; }

    public SMFEncode Encode { get; set; }

    public List<string> FilterKeys { get; set; } = new();
}
