using System.ComponentModel;
using libMidi.Messages.enums;

namespace libMidi.Messages;

[DisplayName("歌詞")]
public record Lyric
    : MetaTextBase
{
    public Lyric(byte[] data) : base(data) { }

    public Lyric(string text) : base(text) { }

    public override MetaType MetaType => MetaType.Lyric;

    public override string ToString() => $"{base.ToString()}";
}
