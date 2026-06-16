using FEx.Agnostics.Abstractions.Enums;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Utilities;
using Microsoft.Azure.Storage.DataMovement;
using System;
using System.Diagnostics;

namespace FEx.AzureStorage;

public class ProgressState
{
    private readonly IProgress<string> _progress;

    public long LoggedProgress { get; set; }
    public double LoggedPercentage { get; set; }
    public double TotalSize { get; private set; }
    public string Name { get; private set; }
    public object OperationString { get; private set; }
    public string ElapsedTime => Sw.GetTime();
    protected Stopwatch Sw { get; }

    public ProgressState(IProgress<string> progress, StorageOperation operation)
        : this(progress, operation, null, null)
    {
    }

    public ProgressState(IProgress<string> progress, StorageOperation operation, string name, double? totalSize)
    {
        OperationString = operation.GetEnumValueDescription();
        _progress = progress;
        Sw = new();
        Reset(name, totalSize, null);
    }

    public static string GetProgress(double prg) =>
        FileLengthConverter.ConvertFileLengthToString(prg, LengthType.Bytes, LengthType.AutoDetect, 2);

    public void Reset() => Reset(null, null, null);

    public void Reset(string name, double? totalSize, StorageOperation? operation)
    {
        if (Sw.IsRunning)
            Sw.Stop();

        LoggedPercentage = 0;
        LoggedProgress = 0;

        if (totalSize.HasValue)
            TotalSize = totalSize.Value;

        if (name is not null)
            Name = name;

        if (operation.HasValue)
            OperationString = operation.Value.GetEnumValueDescription();
    }

    public void LogProgress(TransferStatus progress)
    {
        if (!Sw.IsRunning
            && progress.BytesTransferred == 0)
            Sw.Restart();

        if (progress.BytesTransferred > 0
            && LoggedProgress != progress.BytesTransferred)
        {
            var ms = Sw.ElapsedMilliseconds;
            var prg = Convert.ToDouble(progress.BytesTransferred);

            if (prg == TotalSize)
                Sw.Stop();

            var percentage = Math.Floor(prg / TotalSize * 100);

            if (LoggedPercentage + 1 <= percentage)
            {
                LoggedPercentage = percentage;
                LoggedProgress = progress.BytesTransferred;

                _progress.Report(
                    $"{OperationString}:\t{percentage}%\t{GetProgress(prg)}\tof {Name}\tETC:\t{GetRemainingTime(ms)}");
            }
        }
    }

    public void Restart() => Sw.Restart();

    public void Stop() => Sw.Stop();

    private string GetRemainingTime(double ms)
    {
        var etr = (TotalSize - LoggedProgress) / LoggedProgress * ms;

        return TimeSpan.FromMilliseconds(etr).GetTime();
    }
}