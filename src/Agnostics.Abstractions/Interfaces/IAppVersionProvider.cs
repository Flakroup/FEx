namespace FEx.Agnostics.Abstractions.Interfaces;

/// <summary>Provides the version of the running application.</summary>
public interface IAppVersionProvider
{
    /// <summary>Gets the application version.</summary>
    /// <returns>The application version as text.</returns>
    string GetAppVersion();
}