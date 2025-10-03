using System.Reflection;

namespace libMidi.Messages;

public abstract record SystemCommonMessage
    : SystemMessage
{
    public override byte[] GetByte() => throw new NotImplementedException($"{MethodBase.GetCurrentMethod()}");
}
