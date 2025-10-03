namespace libMidi.SMF;

public class DrumPitch
{
    public const string DRUM_NULL = "...";

    public string DeviceName { get; set; } = DRUM_NULL;
    public byte? DevicePitch { get; set; }
}
