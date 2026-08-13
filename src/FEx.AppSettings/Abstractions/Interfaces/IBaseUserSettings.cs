using System.ComponentModel;

namespace FEx.AppSettings.Abstractions.Interfaces;

public interface IBaseUserSettings : INotifyPropertyChanged
{
    string? PersistencePath { get; }
}