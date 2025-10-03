namespace libMidi.Messages;

public abstract record SystemRealTimeMessage
    : SystemMessage
{
    public override int Length => 1;

    public override byte[] GetByte() => throw new NotImplementedException();
}
