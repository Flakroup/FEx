namespace FEx.MVVM.Abstractions.Interfaces;

public interface IHasUIContext
{
    IUIContextAware UIContextHandler { get; }
}