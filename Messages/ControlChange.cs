using System.ComponentModel;
using libMidi.Messages.enums;

namespace libMidi.Messages;

[DisplayName("コントロールチェンジ")]
public record ControlChange
    : ChannelVoiceMessage
{
    public ControlChange(byte channel, CtrlType ctrlType, byte value)
        : base(channel)
    {
        CtrlType = ctrlType;
        Value = value;
    }

    public override VoiceType VoiceType => VoiceType.ControlChange;

    public override int Length => 3;

    public CtrlType CtrlType { get; init; }

    public byte Value { get; init; }

    public override byte[] GetByte() => [StatusByte, (byte)CtrlType, Value];

    public override string ToString() => $"{GetType().Name} {CtrlType} {Value}";
}
