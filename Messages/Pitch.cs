using libMidi.SMF.enums;

namespace libMidi.Messages;

public static class Pitch
{
    public static PitchNoteMethod Method { get; set; }

    private static readonly List<string> NoteNames = new()
        { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

    public static string NoteName(this byte pitch)
    {
        if (pitch < 0 || pitch > 127) return string.Empty;
        int note = pitch % 12;
        int oct = (pitch - note) / 12 - (Method == PitchNoteMethod.YAMAHA ? 2 : 1);
        return $"{NoteNames[note]}{oct}";
    }

    public static string NoteName(this int pitch)
    {
        return ((byte)pitch).NoteName();
    }
}
