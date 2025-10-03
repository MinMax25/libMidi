using System.ComponentModel;
using libMidi.Messages.enums;

namespace libMidi.Messages;

[DisplayName("テキスト")]
public record MetaText
    : MetaTextBase
{
    public MetaText(byte[] data) : base(data) { }

    public MetaText(string text) : base(text) { }

    public override MetaType MetaType => MetaType.MetaText;

    public override string ToString() => $"{base.ToString()}";
}
