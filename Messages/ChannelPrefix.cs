using System.ComponentModel;
using libMidi.Messages.enums;

namespace libMidi.Messages;

[DisplayName("MIDIチャンネルプリフィックス")]
public record ChannelPrefix
    : MetaTextBase
{
    public ChannelPrefix(byte[] data) : base(data) { }

    public ChannelPrefix(string text) : base(text) { }

    public override MetaType MetaType => MetaType.ChannelPrefix;

    public override string ToString() => $"{base.ToString()}";
}
