using System.ComponentModel;
using libMidi.Messages.enums;

namespace libMidi.Messages;

[DisplayName("インストゥルメント名")]
public record InstrumentName
    : MetaTextBase
{
    public InstrumentName(byte[] data) : base(data) { }

    public InstrumentName(string text) : base(text) { }

    public override MetaType MetaType => MetaType.InstrumentName;

    public override string ToString() => $"{base.ToString()}";
}
