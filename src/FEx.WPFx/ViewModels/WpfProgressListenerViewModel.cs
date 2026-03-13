using FEx.Core.Abstractions.Interfaces;
using FEx.Legacy.Mvvm.ViewModels;
using FEx.WPFx.Abstractions.Interfaces;
using FEx.WPFx.Models;
using FEx.WPFx.Services;
using System.ComponentModel;
using System.Windows.Controls;

namespace FEx.WPFx.ViewModels;

public class WpfProgressListenerViewModel : ProgressListenerViewModel<WpfProgressStatusContainer>,
    IWpfProgressListenerViewModel<WpfProgressStatusContainer>
{
    private string _viewName;

    private IViewDesign _design;

    public ContentControl View { get; set; }

    public string ViewName
    {
        get => _viewName;
        set => SetProperty(ref _viewName, value);
    }

    public IViewDesign Design
    {
        get => _design;
        protected set => SetProperty(ref _design, value);
    }

    public WpfProgressListenerViewModel(bool useMainProgressContainer = false,
                                        params IAsyncInitializable[] dependencies)
        : base(useMainProgressContainer, dependencies)
    {
        Design = new ViewDesign();
    }

    protected override bool GetIsInDesignMode() => DesignerProperties.GetIsInDesignMode(new());
}
