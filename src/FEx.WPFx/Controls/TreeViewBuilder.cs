using FEx.Abstractions.Interfaces;
using FEx.MVVM.Abstractions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace FEx.WPFx.Controls;

public class TreeViewBuilder : TreeViewBuilderBase<TreeViewItem>
{
    public TreeViewBuilder(IFExDispatcher dispatcher, FileSystemIconsProvider fileSystemIconsProvider)
        : base(fileSystemIconsProvider, dispatcher)
    {
    }

    /// <summary>
    ///     Gets the TreeView item.
    /// </summary>
    /// <param name="nodeStub">The node stub.</param>
    /// <returns>
    ///     TreeViewItem
    /// </returns>
    public override async Task<TreeViewItem> GetTreeViewItemAsync(FExTreeViewNode nodeStub)
    {
        BitmapSource img = await GetBitmapSourceAsync(nodeStub);

        return await _dispatcher.InvokeOnMainThreadAsync(() =>
        {
            var item= new TreeViewItem
            {
                Name = nodeStub.NodeName,
                Header = nodeStub.NodeHeader,
                IsExpanded = nodeStub.IsExpanded
            };

            //if(img is not null)
            //    item.=img;

            //res.SetBinding(Control.ForegroundProperty, new Binding(nameof(Foreground)) { Source = this });
            return item;
        });
    }
}