using System.Reflection;
using libMidi.Messages.enums;

namespace libMidi.Messages;

public abstract record ChannelMessage
    : MidiMessage
{
    public abstract VoiceType VoiceType { get; }

    public byte Ch
    {
        get => _Ch;
        init
        {
            if (value < 0 | value > 16)
            {
                throw new ArgumentOutOfRangeException($"{MethodBase.GetCurrentMethod()}");
            }
            _Ch = value;
        }
    }
    private byte _Ch = 1;

    public override byte StatusByte => (byte)((byte)VoiceType | Ch - 1);

    public ChannelMessage ChangeChannel(byte ch) => this with { Ch = ch };
}
