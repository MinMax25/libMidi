using System.Text;
using libMidi.Messages.interfaces;

namespace libMidi.Messages;

public abstract record MetaTextBase
    : MetaMessage
    , IVariantData
{
    public MetaTextBase(byte[] data)
    {
        Data = data;
    }

    public MetaTextBase(string text)
    {
        Data = Encoding.UTF8.GetBytes(text);
    }

    public string Text => Encoding.UTF8.GetString(Data).Replace("\0", string.Empty);

    public byte[] Data { get; init; }

    public override int Length => base.Length + Data.GetVariant().Length + Data.Length;

    public override byte[] GetByte() => base.GetByte().Concat(this.MidiVarDataToByte()).ToArray();

    public override string ToString() => $"{GetType().Name} '{Text}'";
}
