using System.ComponentModel;
using System.Threading;

namespace FEx.AppSettings.Abstractions.Interfaces;

public interface IBaseUserSettings : INotifyPropertyChanged
{
    string PersistencePath { get; }
    SemaphoreSlim SettingsLock { get; }
}