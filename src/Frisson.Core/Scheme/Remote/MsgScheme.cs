using System.Text.Json;
using SchemeBase = Frisson.Core.Scheme.Scheme;

namespace Frisson.Core.Scheme.Remote;

/// <summary>
/// Remote "msg" type — valid but ignored by Frisson (protocol compatibility).
/// No additional fields required.
/// </summary>
public sealed class MsgScheme : SchemeBase
{
    public override string Type => "msg";

    public override string ToJson()
    {
        return JsonSerializer.Serialize(new { type = Type });
    }
}
