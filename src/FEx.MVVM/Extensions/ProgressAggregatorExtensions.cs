using FEx.MVVM.Abstractions.Interfaces;
using FEx.MVVM.Utilities;
using System.Collections.Generic;
using System.Net;

namespace FEx.MVVM.Extensions;

public static class ProgressAggregatorExtensions
{
    public static IList<string> ListenerPropertyNames { get; } =
    [
        nameof(IProgressStatus.Value),
        nameof(IProgressStatus.Maximum),
        nameof(IProgressStatus.IsIndeterminate),
        nameof(IProgressStatus.Percentage),
        nameof(IProgressStatus.PrecisePercentage),
        nameof(IProgressStatus.Info),
        nameof(IProgressStatus.Unit),
        nameof(IProgressStatus.Mode),
        nameof(IProgressStatus.IsBusy),
        nameof(IProgressStatus.IsInfoVisible),
        nameof(IProgressStatus.StatusInfo),
        nameof(IProgressStatus.CurrItemInfo),
        nameof(IProgressStatus.ThreadsInfo),
        nameof(IProgressStatus.State)
    ];

    /// <summary>
    ///     Sets the state of the current download.
    /// </summary>
    /// <param name="viewModel">The view model.</param>
    /// <param name="e">The <see cref="DownloadProgressChangedEventArgs" /> instance containing the event data.</param>
    public static void SetCurrentDownloadState(this IProgressAggregator viewModel, DownloadProgressChangedEventArgs e) => viewModel.SetCurrentDownloadState(e.BytesReceived, e.TotalBytesToReceive, e.UserState);

    // ReSharper disable UnusedParameter.Global
    public static void SetCurrentDownloadState(this IProgressAggregator viewModel,
                                               double? bytesReceived,
                                               double? totalBytesToReceive = null,
                                               object userState = null)
        // ReSharper restore UnusedParameter.Global
    {
        viewModel.SetIsFileOperation(true);
        viewModel.PrgSet(bytesReceived, totalBytesToReceive);
    }
}