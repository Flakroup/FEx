namespace FEx.Abstractions
{
    public interface IExceptionHandler
    {
        bool CanHandle(Exception exception);
        void Handle(Exception exception);
    }
}
