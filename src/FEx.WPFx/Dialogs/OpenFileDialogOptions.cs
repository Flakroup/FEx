using FEx.Common.Extensions;
using FEx.MVVM.Abstractions.Dialogs;
using FEx.MVVM.Abstractions.Interfaces;
using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace FEx.WPFx.Dialogs;

public class OpenFileDialogOptions : OpenFileDialogOptionsBase<OpenFileDialog>
{
    /// <summary>
    /// Shows the dialog.
    /// </summary>
    /// <param name="owner">
    /// Any object that implements <see cref="T:System.Windows.Window" /> that represents the
    /// top-level window that will own the modal dialog box.
    /// </param>
    /// <param name="viewModel">The view model.</param>
    public bool ShowDialog(Window owner = null, IProgressAggregator viewModel = null) =>
        ShowDialog(openFileDialog => owner is not null
                ? openFileDialog.ShowDialog(owner)
                : openFileDialog.ShowDialog(),
            viewModel)
        == true;

    protected override OpenFileDialog MapToDialog() =>
        new()
        {
            Title = Title,
            Filter = Filter,
            FileName = FileName,
            RestoreDirectory = RestoreDirectory,
            InitialDirectory = InitialDirectory,
            FilterIndex = FilterIndex,
            DefaultExt = FileName.IsNotNullOrEmptyString()
                ? Path.GetExtension(FileName)
                : DefaultExt,
            Multiselect = Multiselect
        };

    /// <inheritdoc />
    protected override void MapFromDialog(OpenFileDialog dialog)
    {
        FileName = dialog.FileName;
        FileNames = dialog.FileNames;
    }
}