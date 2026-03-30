namespace CoyoteStudio.Core.Error;

public class ErrorMessager : IDisposable
{
    private readonly Dictionary<Type, List<Action<object>>> _subcribers = new();

    public void Listen<TMessage>(Action<TMessage> action)
    {
        var type = typeof(TMessage);
        if (!_subcribers.ContainsKey(type))
        {
            _subcribers[type] = new List<Action<object>>();
        }
        _subcribers[type].Add(o => action((TMessage)o));
    }


    public void Send<TMessage>(TMessage message)
    {
        var type = typeof(TMessage);
        if (_subcribers.TryGetValue(type, out var actions))
        {
            foreach (var action in actions.ToList())
            {
                action(message);
            }
        }
    }

    public void Dispose()
    {

    }
}
