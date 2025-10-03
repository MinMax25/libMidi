using System.ComponentModel;
using libMidi.Messages.enums;

namespace libMidi.Messages;

[DisplayName("シーケンス名 (曲タイトル) /トラック名")]
public record SequenceTrackName
    : MetaTextBase
{
    public SequenceTrackName(byte[] data) : base(data) { }

    public SequenceTrackName(string text) : base(text) { }

    public override MetaType MetaType => MetaType.SequenceTrackName;

    public override string ToString() => $"{base.ToString()}";
}
