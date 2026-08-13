using FEx.MVVM.Abstractions.Interfaces;
using System.Windows;
using System.Windows.Shell;

namespace FEx.WPFx.ViewModels;

public interface IWpfProgressInfo : IProgressAggregator
{
    TaskbarItemProgressState ProgressState { get; set; }
    Visibility PrgInfoVisibility { get; set; }
}

public interface IWpfProgressInfoGet : IProgressAggregator
{
    TaskbarItemProgressState TaskbarProgressState { get; }
    Visibility PrgInfoVisibility { get; }
}