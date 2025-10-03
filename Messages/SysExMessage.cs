using System.ComponentModel;
using System.Reflection;
using libMidi.Messages.enums;
using libMidi.Messages.interfaces;

namespace libMidi.Messages;

[DisplayName("システムエクスクルーシヴメッセージ")]
public record SysExMessage
    : SystemMessage
    , IVariantData
{
    public SysExMessage(byte[] data)
    {
        Data = data;

        foreach (var t in Enum.GetValues<SysExType>().ToArray())
        {
            var sample = t.GetExclusiveAttribute();
            var mask = t.GetExclusiveMaskAttribute();

            if (sample == null || sample.Data.Length != data.Length) continue;

            if (mask == null)
            {
                if (sample.Data.SequenceEqual(data))
                {
                    ExclusiveType = t;
                    break;
                }
            }
            else
            {
                if (mask.Data.Length == sample.Data.Length)
                {
                    byte[] compare = data.Select((x, i) => (byte)(x & mask.Data.ToArray()[i])).ToArray();
                    if (sample.Data.SequenceEqual(compare))
                    {
                        ExclusiveType = t;
                        break;
                    }
                }
                else
                    throw new InvalidDataException($"{MethodBase.GetCurrentMethod()}");
            }
        }
    }

    public override byte StatusByte => 0xf7;

    public override int Length => base.Length + Data.GetVariant().Length + Data.Length;

    public SysExType ExclusiveType { get; init; }

    public byte[] Data { get; init; }

    public override string ToString() => $"{GetType().Name} {ExclusiveType} {BitConverter.ToString(Data).Replace("-", " ")}";

    public override byte[] GetByte() => new byte[] { StatusByte, }.Concat(this.MidiVarDataToByte()).ToArray();
}
