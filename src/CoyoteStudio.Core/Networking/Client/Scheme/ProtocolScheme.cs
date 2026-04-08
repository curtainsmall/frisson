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
}



