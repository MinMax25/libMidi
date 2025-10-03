using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using libMidi.Messages;

namespace libMidi.SMF;

public class XFKaraokeMessage
    : TrackBase
{
    public XFKaraokeMessage(MidiData midiData) : base(midiData) { }

    public override byte[] ChunkID => XFKM_ID;

    public string GetSRT(long offset, bool srtRemoveComment)
    {
        var data = new Dictionary<TimeSpan, string>();

        TimeSpan st = default;
        var text = new StringBuilder();

        foreach (var item in Events)
        {
            if (item.Message is Lyric lyric)
            {
                if (lyric.Text.StartsWith("<") || lyric.Text.StartsWith("/"))
                {
                    if (text.Length > 0)
                    {
                        AddData(data, st, text.ToString());
                        text.Clear();
                        st = default;
                    }
                    var str = lyric.Text.Substring(1);
                    if (str.Length > 0)
                    {
                        if (st == default)
                        {
                            st = item.Time + TimeSpan.FromMilliseconds(offset);
                        }
                        text.Append(str);
                    }
                }
                else
                {
                    if (st == default)
                    {
                        st = item.Time + TimeSpan.FromMilliseconds(offset);
                    }
                    text.Append(lyric.Text.Replace(">", "\t").Replace("^", " "));
                }
            }
            else if (item.Message is EndOfTrack eof)
            {
                AddData(data, st, string.Empty);
            }
        }

        if (text.Length > 0)
        {
            AddData(data, st, text.ToString());
        }

        var srt = new StringBuilder();
        int count = 1;

        var enumerator = data.GetEnumerator();

        var t1 = enumerator.Current;

        var sw = false;

        while (enumerator.MoveNext())
        {
            var t2 = enumerator.Current;
            if (t1.Value == null)
            {
                t1 = t2;
                continue;
            }

            if (sw)
            {
                srt.AppendLine(string.Empty);
            }
            else
            {
                sw = true;
            }

            srt.AppendLine(count.ToString());
            srt.Append(TimeSpanToSrtFormat(t1.Key));
            srt.Append(" --> ");
            srt.AppendLine(TimeSpanToSrtFormat(t2.Key - TimeSpan.FromMilliseconds(1)));
            srt.AppendLine(t1.Value);
            t1 = t2;
            count++;
        }

        if (srtRemoveComment)
        {
            return Regex.Replace(srt.ToString(), @"\[[^\]]*\]", "");
        }
        else
        {
            return srt.ToString();
        }
    }

    private static string TimeSpanToSrtFormat(TimeSpan time)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0:D2}:{1:D2}:{2:D2},{3:D3}", time.Hours, time.Minutes, time.Seconds, time.Milliseconds);
    }

    private static void AddData(Dictionary<TimeSpan, string> data, TimeSpan timeSpan, string text)
    {
        if (data.ContainsKey(timeSpan) == false)
        {
            data.Add(timeSpan, text.Trim());
        }
    }
}