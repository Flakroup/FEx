using FEx.AppSettings.Abstractions.Interfaces;
using StrongInject;

namespace FEx.AppSettings;

public interface IFExAppSettingsModule : IContainer<FExAppSettings>, IContainer<IConfigurationService>
{
}