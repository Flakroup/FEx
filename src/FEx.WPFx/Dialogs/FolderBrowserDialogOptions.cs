using FEx.MVVM.Abstractions.Dialogs;
using FEx.MVVM.Abstractions.Interfaces;
using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using IWin32Window = System.Windows.Forms.IWin32Window;

namespace FEx.WPFx.Dialogs;

public sealed class FolderBrowserDialogOptions : FolderBrowserDialogOptionsBase<FolderBrowserDialog>, IDisposable
{
    private bool _isDisposed;

    /// <summary>
    ///     Shows the dialog.
    /// </summary>
    /// <param name="owner">
    ///     Any object that implements <see cref="T:System.Windows.Forms.IWin32Window" /> that represents the
    ///     top-level window that will own the modal dialog box.
    /// </param>
    /// <param name="viewModel">The view model.</param>
    public bool ShowDialogOk(Window owner = null, IProgressAggregator viewModel = null) =>
        ShowDialog(owner, viewModel) == DialogResult.OK;

    /// <summary>
    ///     Shows the dialog.
    /// </summary>
    /// <param name="owner">
    ///     Any object that implements <see cref="T:System.Windows.Forms.IWin32Window" /> that represents the
    ///     top-level window that will own the modal dialog box.
    /// </param>
    /// <param name="viewModel">The view model.</param>
    public DialogResult ShowDialog(Window owner = null, IProgressAggregator viewModel = null) =>
        ShowDialog(folderBrowserDialog =>
            {
                IWin32Window win32Window = new NativeWindow();
                ((NativeWindow)win32Window).AssignHandle(new WindowInteropHelper(owner!).Handle);

                return folderBrowserDialog.ShowDialog(win32Window);
            },
            viewModel);

    protected override FolderBrowserDialog MapToDialog() =>
        new()
        {
            Description = Description,
            SelectedPath = SelectedPath,
            ShowNewFolderButton = ShowNewFolderButton,
            RootFolder = RootFolder
        };

    /// <inheritdoc />
    protected override void MapFromDialog(FolderBrowserDialog dialog)
    {
        SelectedPath = dialog.SelectedPath;
    }

    #region IDisposable
    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        Dialog?.Dispose();
    }
    #endregion
}