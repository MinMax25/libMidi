namespace libMidi.Messages.attributes;

[AttributeUsage(AttributeTargets.Class)]
public class XFStyleAttribute
    : Attribute
{
    public XFStyleAttribute(byte[] data)
    {
        Data = data;
    }

    public byte[] Data { get; init; }
}
