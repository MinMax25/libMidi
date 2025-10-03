namespace libMidi.SMF.interfaces;

public interface ISMFReader
    : IDisposable
{
    bool EOF { get; }

    int TotalBytesRead { get; set; }

    //byte[] Read(byte[] buffer);

    byte[] ReadBytes(int count);

    byte ReadByte();

    short ReadShort();

    (long Val, int Len) ReadVariant();
}
