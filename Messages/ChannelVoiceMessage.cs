namespace libMidi.Messages;

public abstract record ChannelVoiceMessage
    : ChannelMessage
{
    public ChannelVoiceMessage(byte ch)
    {
        Ch = ch;
    }
}
