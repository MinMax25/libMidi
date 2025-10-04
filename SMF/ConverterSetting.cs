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

    public bool ChannelFix { get; set; } = true;

    public bool InsertTrackName { get; set; } = true;

    public bool RemoveProgramChange { get; set; } = true;

    public bool ReplaceNoteOn { get; set; } = true;

    public bool CreateCodeTrack { get; set; } = true;

    public bool LyricAdustment { get; set; } = true;

    public bool LyricPaddingPlus { get; set; } = true;

    public int SRTOffset { get; set; } = -300;

    public bool SRTRemoveComment { get; set; } = true;

    public bool XFStyleConvert { get; set; } = true;

    public SMFEncode Encode { get; set; } = SMFEncode.UTF8;

    public List<string> FilterKeys { get; set; } = [
        "調の設定",
        "歌詞",
        "マーカー",
        "シーケンス名 (曲タイトル) /トラック名",
        "テンポ",
        "拍子の設定",
        "ノート・オフ",
        "ノート・オン",
        "ピッチベンド",
        "Expression",
        "Hold1"
  ];
}
