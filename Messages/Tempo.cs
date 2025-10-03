using System.ComponentModel;
using libMidi.Messages.enums;

namespace libMidi.Messages;

[DisplayName("テンポ")]
public record Tempo
    : MetaMessage
{
    public Tempo(float bpm)
    {
        BPM = bpm;
    }

    public override MetaType MetaType => MetaType.Tempo;

    public override int Length => 6;

    public float BPM { get; init; }

    public override byte[] GetByte()
    {
        var val = ((int)(60000000 / BPM)).GetByte().Skip(1);
        return base.GetByte().Concat(new byte[] { 0x03 }).Concat(val).ToArray();
    }

    public override string ToString() => $"{base.ToString()} bpm={BPM:f2}";
}
