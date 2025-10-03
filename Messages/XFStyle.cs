using System.Reflection;
using libMidi.Messages.attributes;

namespace libMidi.Messages;

public abstract record XFStyle
    : SequencerSpecific
{
    protected XFStyle(byte[] data)
        : base(data)
    {
        Data = data;
    }

    public static new XFStyle? GetNew(byte[] data)
    {
        XFStyle? result = null;

        var types = typeof(XFStyle).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(XFStyle))).ToArray();

        foreach (var t in types)
        {
            var sample = t.GetCustomAttribute<XFStyleAttribute>();
            var mask = t.GetCustomAttribute<XFStyleMaskAttribute>();

            if (sample == null || sample.Data.Length != data.Length) continue;

            if (mask == null)
            {
                if (sample.Data.SequenceEqual(data))
                {
                    result = (XFStyle?)Activator.CreateInstance(t, [data]);
                    break;
                }
            }
            else
            {
                if (mask.Data.Length == sample.Data.Length)
                {
                    byte[] compare = data.Select((x, i) => (byte)(x & mask.Data.ToArray()[i])).ToArray();
                    if (sample.Data.SequenceEqual(compare))
                    {
                        result = (XFStyle?)Activator.CreateInstance(t, [data]);
                        break;
                    }
                }
                else
                    throw new InvalidDataException($"{MethodBase.GetCurrentMethod()}");
            }
        }

        return result;
    }
}
