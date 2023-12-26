using System;
using System.Threading.Tasks;

namespace FEx.MVVM.Abstractions.Interfaces;

public interface IUIContextExecutor
{
    bool CheckAccess(object sender = null);

    void ExecuteActionInIdleUIContext(Action action, object sender = null);
    T ExecuteActionInIdleUIContext<T>(Func<T> action, object sender = null);

    void ExecuteActionInUIContext(Action action, object sender = null);
    T ExecuteActionInUIContext<T>(Func<T> action, object sender = null);

    Task ExecuteActionInIdleUIContextAsync(Action action, object sender = null);
    Task<T> ExecuteActionInIdleUIContextAsync<T>(Func<T> action, object sender = null);

    Task ExecuteActionInUIContextAsync(Action action, object sender = null);
    Task<T> ExecuteActionInUIContextAsync<T>(Func<T> action, object sender = null);
}