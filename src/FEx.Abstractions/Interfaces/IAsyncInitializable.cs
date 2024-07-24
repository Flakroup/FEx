using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace FEx.Abstractions.Interfaces;

public interface IAsyncInitializable : IDisposable, INotifyPropertyChanged
{
    bool IsInitialized { get; }
    string TypeName { get; }
    string TypeFullName { get; }

    Task InitializeAsync();
}