using System.Reflection;
using libMidi.Messages;
using libMidi.Messages.enums;
using libMidi.SMF.interfaces;

namespace libMidi.SMF;

public static class SMFLoader
{
    private static readonly List<byte[]> HeaderIDList = new() { Chunk.HEADER_ID };

    private static readonly List<byte[]> TrackIDList = new() { Chunk.TRACK_ID, Chunk.XFID_ID, Chunk.XFKM_ID };

    public static MidiData Load(string fileName)
    {
        using ISMFReader reader = new SMFReader(fileName);

        MidiData midiData = CreateMidiData(reader);

        midiData.FilePath = fileName;

        try
        {
            while (!reader.EOF)
                midiData.AddTrack(TrackReader(reader, midiData));
        }
        catch
        {
            // リアルタイムメッセージが紛れ込んだ場合の対処未実装のため
            // 応急処置
        }

        foreach (ITrack track in midiData.Tracks) track.SetFilter(SMFConverter.Def.InitFilter.Select(x => x.Key).ToArray());

        midiData.Organize();

        return midiData;
    }

    public static bool Load(MidiData midiData)
    {
        try
        {
            using ISMFReader reader = new SMFReader(midiData.FilePath);
            MidiData result = CreateMidiData(reader);

            while (!reader.EOF)
                midiData.AddTrack(TrackReader(reader, midiData));

            midiData.Division = result.Division;
            foreach (var track in result.Tracks)
            {
                midiData.AddTrack(track);
            }
            midiData.Organize();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static MidiData CreateMidiData(ISMFReader reader)
    {
        FindChunkID(reader, HeaderIDList);

        _ = GetChunkDataLength(reader);

        _ = reader.ReadShort();
        _ = reader.ReadShort();
        short data3 = reader.ReadShort();

        return new MidiData(data3);
    }

    private static byte[] FindChunkID(ISMFReader reader, List<byte[]> idList)
    {
        byte[] compare = reader.ReadBytes(Chunk.ID_SIZE);

        foreach (var id in idList)
        {
            if (id.SequenceEqual(compare))
                return id;
        }

        throw new ArgumentException("Chunk ID not found");
    }

    private static int GetChunkDataLength(ISMFReader reader)
    {
        byte[] buff = reader.ReadBytes(Chunk.LENGTH_SIZE);

        if (BitConverter.IsLittleEndian)
            Array.Reverse(buff);

        return BitConverter.ToInt32(buff);
    }

    private static ITrack TrackReader(ISMFReader reader, MidiData midiData)
    {
        ITrack track = NewTrack(reader, midiData);
        int trackLenght = GetChunkDataLength(reader);

        long absoluteTick = 0;

        reader.TotalBytesRead = 0;

        while (MidiEventReader(reader, track, ref absoluteTick) is MidiEvent midiEvent)
        {
            track.EventAdd(midiEvent);
            if (reader.TotalBytesRead >= trackLenght) break;
        }

        return track;
    }

    private static ITrack NewTrack(ISMFReader reader, MidiData midiData)
    {
        byte[] id = FindChunkID(reader, TrackIDList);

        if (id.SequenceEqual(Chunk.XFID_ID))
            return midiData.NewTrack<XFInfomationHeader>();
        if (id.SequenceEqual(Chunk.XFKM_ID))
            return midiData.NewTrack<XFKaraokeMessage>();
        else
            return midiData.NewTrack<Track>();
    }

    private static MidiEvent MidiEventReader(ISMFReader reader, ITrack track, ref long absoluteTick)
    {
        (long Val, _) = reader.ReadVariant();

        absoluteTick += Val;

        var midiEvent = new MidiEvent(track) { AbsoluteTick = absoluteTick };

        midiEvent.Message = GetMidiMessage(reader, midiEvent);

        return midiEvent;
    }

    private static MidiMessage GetMidiMessage(ISMFReader reader, MidiEvent midiEvent)
    {
        // fix running status
        byte status =
            reader.ReadByte() is byte byte1st & byte1st.IsMSBOn()
            ? byte1st
            : LastStatusByte(midiEvent.Parent);

        // status != byte1stの場合はランニングステータス
        return ParseMidiMessage(reader, status, byte1st);
    }

    private static byte LastStatusByte(ITrack? track)
    {
        if (track?.Events.LastOrDefault() is not MidiEvent midiEvent ||
            midiEvent.Message is not MidiMessage message)
        {
            throw new ArgumentException($"{MethodBase.GetCurrentMethod()}");
        }

        return message.StatusByte;
    }

    #region Message Parser

    private static MidiMessage ParseMidiMessage(ISMFReader reader, byte status, byte byte1st)
    {
        // System Message
        if (status == 0xf0)
            return ParseSystemExMessage(reader);

        if (status == 0xf7)
            return ParseSystemExMessage(reader);

        if (status == 0xff)
            return ParseMetaMessage(reader);

        if (status.Upper4bit() == 0xf0)
            throw new NotImplementedException($"{MethodBase.GetCurrentMethod()}");

        // Channel Message
        return
            status.Upper4bit() switch
            {
                0x80 => ParseNoteOff(reader, status, byte1st),
                0x90 => ParseNoteOn(reader, status, byte1st),
                0xa0 => ParsePolyphonicKeyPressure(reader, status, byte1st),
                0xb0 => ParseControlChange(reader, status, byte1st),
                0xc0 => ParseProgramChange(reader, status, byte1st),
                0xd0 => ParseAfterTouch(reader, status, byte1st),
                0xe0 => ParsePitchBend(reader, status, byte1st),
                _ => throw new ArgumentException($"{MethodBase.GetCurrentMethod()}"),
            };
    }

    #region System Exclusive Message Parser

    private static MidiMessage ParseSystemExMessage(ISMFReader reader)
    {
        (long Val, _) = reader.ReadVariant();

        byte[] data = reader.ReadBytes((int)Val);
        return new SysExMessage(data);
    }

    #endregion

    #region Meta Message Parser

    private static MidiMessage ParseMetaMessage(ISMFReader reader)
    {
        MetaType metaType = (MetaType)reader.ReadByte();

        return
            metaType switch
            {
                // Text Base
                MetaType.MetaText => ParseTextBaseMessage<MetaText>(reader),
                MetaType.Copyright => ParseTextBaseMessage<CopyrightNotice>(reader),
                MetaType.SequenceTrackName => ParseTextBaseMessage<SequenceTrackName>(reader),
                MetaType.InstrumentName => ParseTextBaseMessage<InstrumentName>(reader),
                MetaType.Lyric => ParseTextBaseMessage<Lyric>(reader),
                MetaType.Marker => ParseTextBaseMessage<Marker>(reader),
                MetaType.CuePoint => ParseTextBaseMessage<CuePoint>(reader),
                MetaType.ProgramName => ParseTextBaseMessage<ProgramName>(reader),
                MetaType.DeviceName => ParseTextBaseMessage<DeviceName>(reader),
                MetaType.ChannelPrefix => ParseTextBaseMessage<ChannelPrefix>(reader),

                // Other
                MetaType.SequenceNumber => ParseSequenceNumber(reader),
                MetaType.PortPrefix => ParsePortPrefixEvent(reader),
                MetaType.EndOfTrack => ParseEndOfTrack(reader),
                MetaType.Tempo => ParseTempo(reader),
                MetaType.SmpteOffset => ParseSmpteOffset(reader),
                MetaType.TimeSignature => ParseTimeSignature(reader),
                MetaType.KeySignature => ParseKeySignature(reader),
                MetaType.SequencerSpecific => ParseSequencerSpecificEvent(reader),

                _ => throw new NotImplementedException($"{MethodBase.GetCurrentMethod()}, MetaType:{metaType}"),
            };
    }

    #region Text Base

    private static MidiMessage ParseTextBaseMessage<T>(ISMFReader reader)
        where T : MetaTextBase
    {
        (long Val, _) = reader.ReadVariant();

        byte[] data = reader.ReadBytes((int)Val);
        byte[] utf8 = data.EncodeUTF8();

        if (Activator.CreateInstance(typeof(T), [utf8]) is MidiMessage msg)
            return msg;

        throw new ArgumentException($"{MethodBase.GetCurrentMethod()}");
    }

    #endregion

    private static MidiMessage ParseSequenceNumber(ISMFReader reader)
    {
        _ = reader.ReadByte();

        var data1 = reader.ReadShort();

        return new SequenceNumber(data1);
    }

    private static MidiMessage ParseEndOfTrack(ISMFReader reader)
    {
        _ = reader.ReadByte();

        return new EndOfTrack();
    }

    private static MidiMessage ParseTempo(ISMFReader reader)
    {
        _ = reader.ReadByte();

        byte[] bytes = reader.ReadBytes(3);

        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);

        float val = BitConverter.ToInt32(bytes.Concat(new byte[] { 0 }).ToArray());

        if (val == 0)
            throw new DivideByZeroException($"{MethodBase.GetCurrentMethod()}");

        return new Tempo(60000000 / val);
    }

    private static MidiMessage ParseSmpteOffset(ISMFReader reader)
    {
        _ = reader.ReadByte();

        byte data1 = reader.ReadByte();
        byte data2 = reader.ReadByte();
        byte data3 = reader.ReadByte();
        byte data4 = reader.ReadByte();
        byte data5 = reader.ReadByte();

        return new SmpteOffset(data1, data2, data3, data4, data5);
    }

    private static MidiMessage ParseTimeSignature(ISMFReader reader)
    {
        _ = reader.ReadByte();

        byte data1 = reader.ReadByte();
        byte data2 = reader.ReadByte();
        _ = reader.ReadByte();
        _ = reader.ReadByte();

        return new TimeSignature(data1, data2);
    }

    private static MidiMessage ParseKeySignature(ISMFReader reader)
    {
        _ = reader.ReadByte();

        sbyte data1 = (sbyte)reader.ReadByte();
        byte data2 = reader.ReadByte();

        Key key = GetKey(data1, data2);

        return new KeySignature(key);
    }

    private static Key GetKey(sbyte sf, byte mi)
    {
        if (mi == 0)
        {
            return sf switch
            {
                -7 => Key.C_Flat_Major,
                -6 => Key.G_Flat_Major,
                -5 => Key.D_Flat_Major,
                -4 => Key.A_Flat_Major,
                -3 => Key.E_Flat_Major,
                -2 => Key.B_Flat_Major,
                -1 => Key.F_Major,
                0 => Key.C_Major,
                1 => Key.G_Major,
                2 => Key.D_Major,
                3 => Key.A_Major,
                4 => Key.E_Major,
                5 => Key.B_Major,
                6 => Key.F_Sharp_Major,
                7 => Key.C_Sharp_Major,
                _ => throw new ArgumentException($"{MethodBase.GetCurrentMethod()}"),
            };
        }
        else
        {
            return sf switch
            {
                -7 => Key.A_Flat_Minor,
                -6 => Key.E_Flat_Minor,
                -5 => Key.B_Flat_Minor,
                -4 => Key.F_Minor,
                -3 => Key.C_Minor,
                -2 => Key.G_Minor,
                -1 => Key.D_Minor,
                0 => Key.A_Minor,
                1 => Key.E_Minor,
                2 => Key.B_Minor,
                3 => Key.F_Sharp_Minor,
                4 => Key.C_Sharp_Minor,
                5 => Key.G_Sharp_Minor,
                6 => Key.D_Sharp_Minor,
                7 => Key.A_Sharp_Minor,
                _ => throw new ArgumentException($"{MethodBase.GetCurrentMethod()}"),
            };
        }
    }

    private static MidiMessage ParseSequencerSpecificEvent(ISMFReader reader)
    {
        (long Val, _) = reader.ReadVariant();

        byte[] data = reader.ReadBytes((int)Val);

        return SequencerSpecific.GetNew(data);
    }

    private static MidiMessage ParsePortPrefixEvent(ISMFReader reader)
    {
        _ = reader.ReadByte();

        byte data1 = reader.ReadByte();

        return new PortPrefix(data1);
    }

    #endregion

    #region Channel Voice Message Parser

    private static MidiMessage ParseNoteOff(ISMFReader reader, byte status, byte byte1st)
    {
        byte data1 = status == byte1st ? reader.ReadByte() : byte1st;
        byte data2 = reader.ReadByte();

        return new NoteOff(status.GetChannel(), data1, data2);
    }

    private static MidiMessage ParseNoteOn(ISMFReader reader, byte status, byte byte1st)
    {
        byte data1 = status == byte1st ? reader.ReadByte() : byte1st;
        byte data2 = reader.ReadByte();

        return new NoteOn(status.GetChannel(), data1, data2);
    }

    private static MidiMessage ParsePolyphonicKeyPressure(ISMFReader reader, byte status, byte byte1st)
    {
        byte data1 = status == byte1st ? reader.ReadByte() : byte1st;
        byte data2 = reader.ReadByte();

        return new PolyphonicKeyPressure(status.GetChannel(), data1, data2);
    }

    private static MidiMessage ParseControlChange(ISMFReader reader, byte status, byte byte1st)
    {
        byte data1 = status == byte1st ? reader.ReadByte() : byte1st;
        byte data2 = reader.ReadByte();

        return new ControlChange(status.GetChannel(), (CtrlType)data1, data2);
    }

    private static MidiMessage ParseProgramChange(ISMFReader reader, byte status, byte byte1st)
    {
        byte data1 = status == byte1st ? reader.ReadByte() : byte1st;

        return new ProgramChange(status.GetChannel(), data1);
    }

    private static MidiMessage ParseAfterTouch(ISMFReader reader, byte status, byte byte1st)
    {
        byte data1 = status == byte1st ? reader.ReadByte() : byte1st;

        return new AfterTouch(status.GetChannel(), data1);
    }

    private static MidiMessage ParsePitchBend(ISMFReader reader, byte status, byte byte1st)
    {
        byte data1 = status == byte1st ? reader.ReadByte() : byte1st;
        byte data2 = reader.ReadByte();

        return new PitchBend(status.GetChannel(), (short)(data2 * 128 + data1 - 8192));
    }

    #endregion

    #endregion
}