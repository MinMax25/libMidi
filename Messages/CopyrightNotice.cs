using System.ComponentModel;
using libMidi.Messages.enums;

namespace libMidi.Messages;

[DisplayName("著作権表示")]
public record CopyrightNotice
    : MetaTextBase
{
    public CopyrightNotice(byte[] data) : base(data) { }

    public CopyrightNotice(string text) : base(text) { }

    public override MetaType MetaType => MetaType.Copyright;

    public override string ToString() => $"{base.ToString()}";
}
