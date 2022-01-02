using FEx.Abstractions;

namespace FEx.Utilities;

public class ExceptionHandlersRegistry : IExceptionHandlersRegistry
{
    private readonly List<IExceptionHandler> _registry;

    public ExceptionHandlersRegistry()
    {
        _registry = new List<IExceptionHandler>();
    }

    public void Register(IExceptionHandler handler)
    {
        Type type = handler.GetType();

        if (_registry.Any(x => x.GetType() == type))
        {
            throw new InvalidOperationException($"There is already registered handler of type: {type}");
        }

        _registry.Add(handler);
    }

    public void Handle(Exception exception)
    {
        foreach (IExceptionHandler handler in _registry.Where(x => x.CanHandle(exception)))
        {
            handler.Handle(exception);
        }
    }
}