using FEx.WPFx.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FEx.WPFx.Controls;

public class FExShellWindow<TViewModel> : FExWindow<TViewModel> where TViewModel : WpfProgressListenerViewModel
{
    public FExShellWindow()
    {
        if (!IsInDesignMode)
        {
            CommandBindings.Add(new(SystemCommands.CloseWindowCommand, OnCloseWindow));

            CommandBindings.Add(new(SystemCommands.MaximizeWindowCommand, OnMaximizeWindow, OnCanResizeWindow));

            CommandBindings.Add(new(SystemCommands.MinimizeWindowCommand, OnMinimizeWindow, OnCanMinimizeWindow));

            CommandBindings.Add(new(SystemCommands.RestoreWindowCommand, OnRestoreWindow, OnCanResizeWindow));

            CommandBindings.Add(new(SystemCommands.ShowSystemMenuCommand, OnSystemMenu, OnCanSystemMenu));
        }
    }

    private static void OnCanSystemMenu(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = true;

    private void OnCanResizeWindow(object sender, CanExecuteRoutedEventArgs e) =>
        e.CanExecute = ResizeMode is ResizeMode.CanResize or ResizeMode.CanResizeWithGrip;

    private void OnCanMinimizeWindow(object sender, CanExecuteRoutedEventArgs e) =>
        e.CanExecute = ResizeMode != ResizeMode.NoResize;

    private void OnCloseWindow(object target, ExecutedRoutedEventArgs e) => SystemCommands.CloseWindow(this);

    private void OnMaximizeWindow(object target, ExecutedRoutedEventArgs e) => SystemCommands.MaximizeWindow(this);

    private void OnMinimizeWindow(object target, ExecutedRoutedEventArgs e) => SystemCommands.MinimizeWindow(this);

    private void OnRestoreWindow(object target, ExecutedRoutedEventArgs e) => SystemCommands.RestoreWindow(this);

    private void OnSystemMenu(object target, ExecutedRoutedEventArgs e)
    {
        var borderHeight = 0;
        var borderWidth = 0;

        if (Template.FindName("WindowsBorder", this) is Border border)
        {
            borderHeight += (int)border.Margin.Top;
            borderWidth += (int)border.Margin.Left;
        }

        if (Template.FindName("InnerBorder", this) is Border inner)
        {
            borderHeight += (int)inner.BorderThickness.Top;
            borderWidth += (int)inner.BorderThickness.Left;
        }

        var titleHeight = 0;

        if (Template.FindName("LayoutRoot", this) is Grid { RowDefinitions.Count: > 0 } borderGrid)
            titleHeight = (int)borderGrid.RowDefinitions[0].Height.Value;

        var currentPosition = new Point(Left + borderWidth, Top + borderHeight + titleHeight);
        SystemCommands.ShowSystemMenu(this, currentPosition);
    }
}