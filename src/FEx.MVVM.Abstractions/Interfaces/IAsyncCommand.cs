using System.Threading.Tasks;
using System.Windows.Input;

namespace FEx.MVVM.Abstractions.Interfaces;

public interface IAsyncCommand : ICommand
{
    Task ExecuteAsync();
}

public interface IAsyncCommand<in T> : ICommand where T : class
{
    Task ExecuteAsync(T parameter);
}