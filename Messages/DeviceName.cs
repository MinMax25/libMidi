using System.ComponentModel;
using libMidi.Messages.enums;

namespace libMidi.Messages;

[DisplayName("デバイス名")]
public record DeviceName
    : MetaTextBase
{
    public DeviceName(byte[] data) : base(data) { }

    public DeviceName(string text) : base(text) { }

    public override MetaType MetaType => MetaType.DeviceName;

    public override string ToString() => $"{base.ToString()}";
}
