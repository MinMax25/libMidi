using libMidi.SMF.enums;

namespace libMidi.SMF;

public record InstInfo
{
    public InstInfo(MidiStd midiStd, byte bankMSB, byte bankLSB, byte pgNum)
    {
        MidiStd = midiStd;
        BankMSB = bankMSB;
        BankLSB = bankLSB;
        PgNum = pgNum;
    }

    public MidiStd MidiStd { get; init; }

    public byte BankMSB { get; init; }

    public byte BankLSB { get; init; }

    public byte PgNum { get; init; }

    public override string ToString() => $"#{MidiStd}, {BankMSB}, {BankLSB}, {PgNum}#";
}
