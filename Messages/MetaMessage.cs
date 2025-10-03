using libMidi.Messages.enums;

namespace libMidi.Messages;

public abstract record MetaMessage
    : SystemMessage
{
    public override byte StatusByte => 0xff;

    public abstract MetaType MetaType { get; }

    public override int Length => 2;    // StatusByte & MetaType

    public override byte[] GetByte() => [StatusByte, (byte)MetaType];

    public override string ToString() => $"{GetType().Name}";
}
