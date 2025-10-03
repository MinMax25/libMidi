using System.ComponentModel;
using libMidi.Messages.enums;
using libMidi.Messages.interfaces;

namespace libMidi.Messages;

[DisplayName("シーケンサ特定メタイベント")]
public record SequencerSpecific
    : MetaMessage
    , IVariantData
{
    protected SequencerSpecific(byte[] data)
    {
        Data = data;
    }

    public static SequencerSpecific GetNew(byte[] data)
    {
        SequencerSpecific? result = XFStyle.GetNew(data);
        return result ?? new SequencerSpecific(data);
    }

    public override MetaType MetaType => MetaType.SequencerSpecific;

    public byte[] Data { get; init; }

    public override int Length => base.Length + Data.GetVariant().Length + Data.Length;

    public override string ToString()
    {
        return $"{base.ToString()} {BitConverter.ToString(Data).Replace("-", " ")}";
    }

    public override byte[] GetByte()
    {
        var data1 = base.GetByte();
        var data2 = this.MidiVarDataToByte();
        return data1.Concat(data2).ToArray();
    }
}
