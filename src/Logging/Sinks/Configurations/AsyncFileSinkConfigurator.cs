using FEx.Logging.Abstractions;
using FEx.Logging.Abstractions.Enums;
using FEx.Logging.Abstractions.Interfaces;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;

namespace FEx.Logging.Sinks.Configurations;

public class AsyncFileSinkConfigurator : SinkConfiguratorBase, IFileSinkConfigurator
{
    private const int OneHundredMegabytesLimit = 104857600;
    private const string DefaultLogFileName = "Log.txt";

    private readonly DirectoryInfo _logsDirectory;

    public string OutputTemplate { get; set; } =
        "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}|{Level:u3}] <s:{SourceContext}>{NewLine}   {Message:lj}{NewLine}{Exception}{NewLine}    [Properties:{Properties}]";

    public int FileSizeLimitBytes { get; set; } = OneHundredMegabytesLimit;
    public RollingInterval RollingInterval { get; set; } = RollingInterval.Hour;
    public int RetainedFileCountLimit { get; set; } = 48;
    public TimeSpan RetainedFileTimeLimit { get; set; } = TimeSpan.FromDays(2);
    public LogEventLevel MinimumLevel { get; set; } = LogEventLevel.Verbose;
    public string LogFileName { get; set; } = DefaultLogFileName;
    public string CustomLogFilePath { get; set; }

    public AsyncFileSinkConfigurator(ILoggingConfiguration loggingConfiguration)
        : base(loggingConfiguration, LoggingOptions.File)
    {
        _logsDirectory = new(Environment.GetFolderPath(Environment.SpecialFolder.Personal));
        _logsDirectory.Create();
    }

    public IEnumerable<FileInfo> GetLogFiles() => _logsDirectory.GetFiles("Log*.txt");

    public override LoggerConfiguration ConfigureSink(LoggerSinkConfiguration writeTo)
    {
        string logFilePath = !string.IsNullOrEmpty(CustomLogFilePath)
            ? CustomLogFilePath
            : Path.Combine(_logsDirectory.FullName, LogFileName);

        return writeTo.Async(asyncConfiguration => asyncConfiguration.File(logFilePath,
            outputTemplate: OutputTemplate,
            fileSizeLimitBytes: FileSizeLimitBytes,
            rollingInterval: RollingInterval,
            retainedFileCountLimit: RetainedFileCountLimit,
            retainedFileTimeLimit: RetainedFileTimeLimit,
            restrictedToMinimumLevel: MinimumLevel));
    }
}