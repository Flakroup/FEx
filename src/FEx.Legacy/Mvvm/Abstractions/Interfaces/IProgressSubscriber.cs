namespace FEx.Legacy.Mvvm.Abstractions.Interfaces;

public interface IProgressSubscriber
{
    void Report(string propertyName, object value);
}