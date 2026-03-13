using EFCore.BulkExtensions;
using System;

namespace FEx.EFCore.Configuration;

public interface IBulkDbConfig
{
    Func<BulkConfig> Config { get; }
    int ParallelBulkOperations { get; }
}