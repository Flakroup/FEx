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
    private string? _viewName;

    // Assigned in the constructor via the Design setter (SetProperty ref-assign the compiler cannot track).
    private IViewDesign _design = null!;

    /// <summary>
    /// The view
    /// </summary>
    public ContentControl? View { get; set; }

    /// <summary>
    /// Gets or sets the name of the view.
    /// </summary>
    /// <value>
    /// The name of the view.
    /// </value>
    public string? ViewName
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