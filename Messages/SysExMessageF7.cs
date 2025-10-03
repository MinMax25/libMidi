using System.ComponentModel;
using libMidi.Messages.enums;
using libMidi.Messages.interfaces;

namespace libMidi.Messages;

[DisplayName("システムエクスクルーシヴメッセージ(F7)")]
public record SysExMessageF7
    : SystemMessage
    , IVariantData
{
    public SysExMessageF7(byte[] data)
    {
        Data = data;
    }

    public override byte StatusByte => 0xf7;

    public override int Length => base.Length + Data.GetVariant().Length + Data.Length;

    public SysExType ExclusiveType => SysExType.Undefined;

    public byte[] Data { get; init; }

    public override string ToString() => $"{GetType().Name} {ExclusiveType} {BitConverter.ToString(Data).Replace("-", " ")}";

    public override byte[] GetByte() => new byte[] { StatusByte, }.Concat(this.MidiVarDataToByte()).ToArray();
}
