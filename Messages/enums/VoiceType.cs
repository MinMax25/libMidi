namespace libMidi.Messages.enums;

public enum VoiceType
{
    NoteOff = 0x80,
    NoteOn = 0x90,
    PolyphonicKeyPressure = 0xa0,
    ControlChange = 0xb0,
    ProgramChange = 0xc0,
    AfterTouch = 0xd0,
    PitchBend = 0xe0
}