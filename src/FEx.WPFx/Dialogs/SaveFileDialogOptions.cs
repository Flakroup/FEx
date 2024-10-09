using FEx.Common.Extensions;
using FEx.MVVM.Abstractions.Dialogs;
using FEx.MVVM.Abstractions.Interfaces;
using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace FEx.WPFx.Dialogs;

public class SaveFileDialogOptions : SaveFileDialogOptionsBase<SaveFileDialog>
{
    /// <summary>
    ///     Shows the dialog.
    /// </summary>
    /// <param name="owner">
    ///     Any object that implements <see cref="T:System.Windows.Window" /> that represents the
    ///     top-level window that will own the modal dialog box.
    /// </param>
    /// <param name="viewModel">The view model.</param>
    public bool ShowDialog(Window owner = null, IProgressAggregator viewModel = null) =>
        ShowDialog(saveFileDialog => owner is not null
                ? saveFileDialog.ShowDialog(owner)
                : saveFileDialog.ShowDialog(),
            viewModel)
        == true;

    protected override SaveFileDialog MapToDialog() =>
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
                : DefaultExt
        };

    /// <inheritdoc />
    protected override void MapFromDialog(SaveFileDialog dialog)
    {
        FileName = dialog.FileName;
        FileNames = dialog.FileNames;
    }
}