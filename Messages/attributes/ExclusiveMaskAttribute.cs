namespace libMidi.Messages.attributes;

[AttributeUsage(AttributeTargets.Field)]
public class ExclusiveMaskAttribute
    : Attribute
{
    public ExclusiveMaskAttribute(byte[] data)
    {
        Data = data;
    }

    public byte[] Data { get; init; }
}