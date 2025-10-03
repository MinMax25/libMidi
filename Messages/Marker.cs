using System.ComponentModel;
using libMidi.Messages.enums;

namespace libMidi.Messages;

[DisplayName("マーカー")]
public record Marker
    : MetaTextBase
{
    public Marker(byte[] data) : base(data) { }

    public Marker(string text) : base(text) { }

    public override MetaType MetaType => MetaType.Marker;

    public override string ToString() => $"{base.ToString()}";
}
