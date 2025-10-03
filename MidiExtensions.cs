using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using libMidi.Messages;
using libMidi.Messages.attributes;
using libMidi.Messages.enums;
using libMidi.Messages.interfaces;
using libMidi.SMF;
using libMidi.SMF.interfaces;

namespace libMidi;

public static class MidiExtensions
{
    public static void RangeEach(this int count, Action<int> action) => Enumerable.Range(0, count).ToList().ForEach(action);

    public static void RangeEach(this short count, Action<int> action) => RangeEach((int)count, action);

    public static ExclusiveAttribute? GetExclusiveAttribute<T>(this T target) where T : Enum
        => target.GetType().GetField(target.ToString())?.GetCustomAttribute<ExclusiveAttribute>();

    public static ExclusiveMaskAttribute? GetExclusiveMaskAttribute<T>(this T e) where T : Enum
        => e.GetType().GetField(e.ToString())?.GetCustomAttribute<ExclusiveMaskAttribute>();

    public static DisplayAttribute? GetDisplayAttribute<T>(this T e) where T : Enum
        => e.GetType().GetField(e.ToString())?.GetCustomAttribute<DisplayAttribute>();

    public static bool IsMSBOn(this byte target) => (target & 0x80) != 0;

    public static byte Upper4bit(this byte target) => (byte)(target & 0xf0);

    public static byte GetChannel(this byte target) => (byte)((target & 0xf) + 1);

    public static (long Value, int Length) GetVariant(this byte[] target)
    {
        int Length = 0;
        long Value = 0;

        do
        {
            Value *= 128;
            Value += target[Length] & 0x7F;
        } while (target[Length++].IsMSBOn());

        return (Value, Length);
    }

    public static byte[] EncodeUTF8(this byte[] target)
    {
        string txtSJIS = Encoding.GetEncoding(932).GetString(target);
        string txtUTF8 = Encoding.UTF8.GetString(target);

        byte[] bytesUTF8 = Encoding.UTF8.GetBytes(txtUTF8);

        return
            !target.SequenceEqual(bytesUTF8)
            ? Encoding.UTF8.GetBytes(txtSJIS)
            : bytesUTF8;
    }

    public static BreakedTime BreakedTime(this IMidiEvent target)
    {
        if (target.Parent == null || target.Parent.Parent == null)
        {
            throw new ArgumentException($"{MethodBase.GetCurrentMethod()}");
        }

        return BreakTime(target.Parent.Parent, target.AbsoluteTick);
    }

    public static BreakedTime BreakTime(MidiData midiData, long lTime)
    {
        long lastTick = 0;
        int lastNN = 4;
        int lastDD = 2;
        int lastCC = 24;
        int lastBB = 8;
        long sumMeasure = 0;
        long deltaMeasure;
        long deltaTime;
        long unitTick;

        foreach (var midiEvent in midiData.TimeSignatures)
        {
            if (midiEvent.AbsoluteTick >= lTime)
                break;

            if (midiEvent.Message is not TimeSignature timeSignature)
                continue;

            deltaTime = midiEvent.AbsoluteTick - lastTick;
            unitTick = midiData.Division * 4 / (1 << lastDD);
            deltaMeasure = deltaTime > 0 ? (deltaTime - 1) / (unitTick * lastNN) + 1 : 0;
            sumMeasure += deltaMeasure;

            lastTick = midiEvent.AbsoluteTick;
            lastNN = timeSignature.NN;
            lastDD = timeSignature.DD;
            lastCC = timeSignature.CC;
            lastBB = timeSignature.BB;
        }

        deltaTime = lTime - lastTick;
        unitTick = midiData.Division * 4 / (1 << lastDD);
        deltaMeasure = deltaTime >= 0 ? (deltaTime) / (unitTick * lastNN) : 0;

        return
            new BreakedTime()
            {
                Mesure = (sumMeasure + deltaMeasure) + 1,
                Beat = ((deltaTime % (unitTick * lastNN)) / unitTick) + 1,
                Tick = deltaTime % unitTick,
                NN = lastNN,
                DD = lastDD,
                CC = lastCC,
                BB = lastBB
            };
    }

    public static byte[] GetByte(this short target)
    {
        byte[] result = BitConverter.GetBytes(target);

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(result);
        }

        return result;
    }

    public static byte[] GetByte(this int target)
    {
        byte[] result = BitConverter.GetBytes(target);

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(result);
        }

        return result;
    }

    public static byte[] MidiVarToByte(this long value)
    {
        List<byte> result = new();

        long buffer = value & 0x7f;
        while ((value >>= 7) > 0)
        {
            buffer <<= 8;
            buffer |= 0x80;
            buffer += (value & 0x7f);
        }
        while (true)
        {
            result.Add((byte)buffer);
            if ((buffer & 0x80) != 0)
                buffer >>= 8;
            else
                break;
        }

        return result.ToArray();
    }

    public static byte[] MidiVarToByte(this int value) => ((long)value).MidiVarToByte();

    public static byte[] MidiVarDataToByte(this IVariantData target) =>
        target.Data.Length.MidiVarToByte().Concat(target.Data).ToArray();

    public static IEnumerable<MidiEvent> GetMidiEvents<T>(this ITrack target, Predicate<MidiEvent<T>>? predicate = null)
        where T : MidiMessage
    {
        return target.Events.Where(x => x.Message is T && (predicate?.Invoke((MidiEvent<T>)x) ?? true));
    }

    public static IEnumerable<T> GetMidiMessages<T>(this ITrack target)
        where T : MidiMessage
    {
        return target.Events.Select(x => x.Message).OfType<T>();
    }

    public static IEnumerable<InstInfo> GetInstruments(this ITrack target)
    {
        List<InstInfo> infos = new();

        target.GetMidiEvents<ProgramChange>().ToList().ForEach(x =>
        {
            if (x.InstrumentInfo != null &&
                !infos.Contains(x.InstrumentInfo))
                infos.Add(x.InstrumentInfo);
        });

        return infos;
    }

    public static bool WhichCtrlType(this MidiEvent target, CtrlType ctrlType)
    {
        if (target.Message is not ControlChange cc) return false;
        return cc.CtrlType == ctrlType;
    }

    public static bool IsKanji(char c)
    {
        return ('\u4E00' <= c && c <= '\u9FCF')
            || ('\uF900' <= c && c <= '\uFAFF')
            || ('\u3400' <= c && c <= '\u4DBF');
    }

    public static bool IsAlphaNumeric(string str)
    {
        if (str == null) return false;
        return new Regex("^[ -~]*$").IsMatch(str);
    }
}
