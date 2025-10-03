using System.ComponentModel;
using libMidi.Messages.enums;

namespace libMidi.Messages;

[DisplayName("キューポイント")]
public record CuePoint
    : MetaTextBase
{
    public CuePoint(byte[] data) : base(data) { }

    public CuePoint(string text) : base(text) { }

    public override MetaType MetaType => MetaType.CuePoint;

    public override string ToString() => $"{base.ToString()}";
}
