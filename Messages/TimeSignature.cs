using System.ComponentModel;
using System.Reflection;
using libMidi.Messages.enums;

namespace libMidi.Messages;

[DisplayName("拍子の設定")]
public record TimeSignature
    : MetaMessage
{
    public TimeSignature(byte nn, byte dd)
    {
        if (nn == 0) throw new ArgumentException($"{MethodBase.GetCurrentMethod()}");
        if (dd == 0) throw new ArgumentException($"{MethodBase.GetCurrentMethod()}");

        NN = nn;
        DD = dd;
    }

    public override MetaType MetaType => MetaType.TimeSignature;

    public override int Length => base.Length + 1 + 4;  // base & datasize & data.length

    public byte NN { get; init; }
    public byte DD { get; init; }
    public byte CC { get; init; } = 24;
    public byte BB { get; init; } = 8;

    public override byte[] GetByte() => base.GetByte().Concat(new byte[] { 0x04, NN, DD, CC, BB }).ToArray();

    public override string ToString() => $"{base.ToString()} {NN}/{Math.Pow(2, DD)} : {CC} : {BB}";
}
