using System.Text;
using libMidi.Messages;
using libMidi.SMF.enums;
using libMidi.SMF.interfaces;

namespace libMidi.SMF;

public record MidiEvent<T>
    : IMidiEvent
    where T : MidiMessage
{
    public MidiEvent(ITrack parent)
    {
        Parent = parent;
    }

    public required long AbsoluteTick { get; set; }

    public long DeltaTick { get; set; }

    public T Message { get; set; } = null!;

    public ITrack Parent { get; init; }

    public long Seqnum { get; set; }

    public byte Channel => (Message as ChannelMessage)?.Ch ?? 0;

    public TimeSpan Time => Parent.Parent.GetTime(AbsoluteTick);

    public InstInfo? InstrumentInfo { get; set; }

    public byte[] GetByte()
    {
        if (Message is MetaTextBase text && SMFConverter.Def.Setting.Encode == SMFEncode.SJIS)
            return DeltaTick.MidiVarToByte().Concat((text with { Data = Encoding.GetEncoding(932).GetBytes(text.Text) }).GetByte()).ToArray();
        else
            return DeltaTick.MidiVarToByte().Concat(Message.GetByte()).ToArray();
    }

    // MidiEventからのCast
    public static explicit operator MidiEvent<T>(MidiEvent target)
    {
        return
            new MidiEvent<T>(target.Parent)
            {
                AbsoluteTick = target.AbsoluteTick,
                DeltaTick = target.DeltaTick,
                Seqnum = target.Seqnum,
                InstrumentInfo = target.InstrumentInfo,
                Message = (T)target.Message with { }
            };
    }
}

public record MidiEvent
    : MidiEvent<MidiMessage>
{
    public MidiEvent(ITrack parent)
        : base(parent)
    {
        Parent = parent;
    }

    public override string ToString() =>
        $"{Seqnum,7} {this.BreakedTime()} Ch {(Channel == 0 ? "--" : $"{Channel}".PadLeft(2))} {Message}" +
        (
            Message is ProgramChange && InstrumentInfo != null
            ? $" ({InstMap.GetInstName(InstrumentInfo, Parent?.Parent?.IsDrum(this) ?? false)})"
            : string.Empty
        );
}
