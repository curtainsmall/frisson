using CoyoteStudio.Core.Control;

namespace CoyoteStudio.Core.Networking.Client.Scheme;

internal class ProtocolSchemeException : Exception
{
    public enum ErrorKind
    {
        InvalidFieldType,
        InvalidFieldValue,
    }

    public ErrorKind Kind { get; init; }
    public ProtocolSchemeException(ErrorKind kind, string message) : base(message)
    {
        Kind = kind;
    }
}

internal abstract class ProtocolScheme
{
    protected static readonly string[] _deviceIdCadidateNames = { "deviceId", "targetId" };
    protected static readonly string[] _remoteIdCadidateNames = { "remoteId", "clientId" };

    internal enum MessageOperationKind
    {
        StrengthIncrease,
        StrengthDecrease,
        StrengthSet,
        Pulse,

        Clear,

        Heartbeat,
    }

    internal enum MessageChannelKind
    {
        A,
        B,
    }

    internal abstract record MessageStrength;
    internal sealed record MessageStrengthValue(int Delta) : MessageStrength;
    internal sealed record MessageStrengthPulse(Pulse Pulse) : MessageStrength;


}



