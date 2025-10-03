namespace libMidi.SMF;

public abstract class Chunk
{
    public static byte[] HEADER_ID => "MThd"u8.ToArray();
    public static byte[] TRACK_ID => "MTrk"u8.ToArray();
    public static byte[] XFID_ID => "XFIH"u8.ToArray();
    public static byte[] XFKM_ID => "XFKM"u8.ToArray();

    public const int ID_SIZE = 4;

    public const int LENGTH_SIZE = 4;

    public abstract byte[] ChunkID { get; }
}
