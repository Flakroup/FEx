using System.IO;

namespace FEx.Abstractions;

public interface IAppInfoProvider
{
    DirectoryInfo AppData { get; }
}