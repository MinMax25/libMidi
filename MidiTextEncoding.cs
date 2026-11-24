using System.Text;

namespace libMidi;

internal static class MidiTextEncoding
{
    internal static Encoding Cp932 { get; } = CodePagesEncodingProvider.Instance.GetEncoding(932)
        ?? throw new NotSupportedException("Code page 932 is unavailable.");
}
