using System.ComponentModel;
using libMidi.Messages.enums;

namespace libMidi.Messages;

[DisplayName("プログラム名")]
public record ProgramName
    : MetaTextBase
{
    public ProgramName(byte[] data) : base(data) { }

    public ProgramName(string text) : base(text) { }

    public override MetaType MetaType => MetaType.ProgramName;

    public override string ToString() => $"{base.ToString()}";
}
