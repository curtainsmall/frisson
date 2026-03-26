namespace CoyoteStudio.Shared.Message;

public interface IMessager
{
    void Send<T>(T message) where T : class;
}