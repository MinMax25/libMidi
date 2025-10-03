using System.ComponentModel;

namespace libMidi.Messages;

[DisplayName("チューンリクエスト")]
public record TuneRequest
    : SystemCommonMessage
{
    public override byte StatusByte => 0xf6;
}
