using System.Collections.Generic;
using System.IO;

namespace FEx.Logging.Abstractions.Interfaces;

public interface IFileSinkConfigurator : ISinkConfigurator
{
    IEnumerable<FileInfo> GetLogFiles();
}