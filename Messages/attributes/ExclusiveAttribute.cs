namespace libMidi.Messages.attributes;

[AttributeUsage(AttributeTargets.Field)]
public class ExclusiveAttribute
    : Attribute
{
    public ExclusiveAttribute(byte[] data)
    {
        Data = data;
    }

    public byte[] Data { get; init; }
}