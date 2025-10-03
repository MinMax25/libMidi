using System.ComponentModel;
using libMidi.Messages.enums;

namespace libMidi.Messages;

[DisplayName("調の設定")]
public record KeySignature
    : MetaMessage
{
    public KeySignature(Key key)
    {
        Key = key;
    }

    public override MetaType MetaType => MetaType.KeySignature;

    public Key Key { get; init; }

    public override int Length => base.Length + 1 + 2;  // base & datasize & data.length

    public override byte[] GetByte()
    {
        byte mi = (byte)((byte)Key > 0x10 ? 0 : 0xFF);
        sbyte sf = (sbyte)(mi == 0 ? (byte)Key - 0x17 : (byte)Key - 0x7);

        return base.GetByte().Concat(new byte[] { 0x02, (byte)sf, mi }).ToArray();
    }

    public override string ToString() => $"{base.ToString()} {Key.GetDisplayAttribute()?.Name}";
}
