namespace libMidi.Messages.attributes;

[AttributeUsage(AttributeTargets.Class)]
public class XFStyleMaskAttribute
    : Attribute
{
    public XFStyleMaskAttribute(byte[] data)
    {
        Data = data;
    }

    public byte[] Data { get; init; }
}