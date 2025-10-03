using System.Reflection;
using libMidi.SMF.interfaces;

namespace libMidi.SMF;

public class SMFReader
    : ISMFReader
{
    public SMFReader(string fileName)
    {
        Stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    private FileStream Stream { get; set; }

    private int ReadPosition;

    public bool EOF => Stream.Length <= ReadPosition;

    public int TotalBytesRead { get; set; }

    public byte[] ReadBytes(int count)
    {
        byte[] buffer = new byte[count];
        int bytesRead = Stream.Read(buffer, 0, count);
        if (bytesRead != count)
            throw new EndOfStreamException($"Expected {count} bytes but got {bytesRead}.");

        ReadPosition += bytesRead;
        TotalBytesRead += bytesRead;
        return buffer;
    }

    public byte ReadByte()
    {
        int value = Stream.ReadByte();

        if (value < 0)
            throw new ArgumentException($"{MethodBase.GetCurrentMethod()}");

        ReadPosition++;
        TotalBytesRead++;

        return (byte)value;
    }

    public short ReadShort()
    {
        byte[] buff = ReadBytes(2);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(buff);
        return BitConverter.ToInt16(buff, 0);
    }

    public (long Val, int Len) ReadVariant()
    {
        int length = 1;
        long value = ReadByte();

        if ((value & 0x80) != 0)
        {
            value &= 0x7f;
            byte c;
            do
            {
                c = ReadByte();
                value = (value << 7) + (c & 0x7f);
                length++;
            } while ((c & 0x80) != 0);
        }

        return (value, length);
    }

    public void Dispose()
    {
        Stream.Dispose();
        GC.SuppressFinalize(this);
    }
}
