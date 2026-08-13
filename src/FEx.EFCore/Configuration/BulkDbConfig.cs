using EFCore.BulkExtensions;
using System;

namespace FEx.EFCore.Configuration;

public class BulkDbConfig : IBulkDbConfig
{
    public Func<BulkConfig> Config { get; }
    public int ParallelBulkOperations { get; }

    public BulkDbConfig(Func<BulkConfig> config, int parallelBulkOperations)
    {
        Config = config;
        ParallelBulkOperations = parallelBulkOperations;
    }
}