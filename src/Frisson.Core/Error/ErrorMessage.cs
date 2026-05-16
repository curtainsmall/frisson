namespace Frisson.Core.Error;

public enum ErrorCode
{
    None,
    ConnectionFailed,
    Disconnected,

    InvalidJson,

    Unknown,
}

public class ErrorMessage
{
    public ErrorCode Code { get; init; }
    public string Message { get; init; }

    public ErrorMessage(ErrorCode code, string message)
    {
        Code = code;
        Message = message;
    }
}