namespace libMidi.SMF;

public class BreakedTime
{
    public long Mesure { get; set; }
    public long Beat { get; set; }
    public long Tick { get; set; }

    public int NN { get; set; }
    public int BB { get; set; }
    public int CC { get; set; }
    public int DD { get; set; }

    public override string ToString() => $"{Mesure:d5}:{Beat:d2}:{Tick:d3}";
}
