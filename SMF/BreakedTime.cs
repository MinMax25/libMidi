namespace libMidi.SMF;

public class BreakedTime
{
    public long Measure { get; set; }

    [System.Obsolete("Use Measure instead.")]
    public long Mesure
    {
        get => Measure;
        set => Measure = value;
    }

    public long Beat { get; set; }
    public long Tick { get; set; }

    public int NN { get; set; }
    public int BB { get; set; }
    public int CC { get; set; }
    public int DD { get; set; }

    public override string ToString() => $"{Measure:d5}:{Beat:d2}:{Tick:d3}";
}
