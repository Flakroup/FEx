using System.Windows.Controls;

namespace FEx.WPFx.Extensions;

public static class TreeViewItemExtensions
{
    public static string FullPath(this HeaderedItemsControl sender)
    {
        string res = null;

        sender.InvokeOnDispatcherContext(() =>
        {
            res = sender.Header.ToString();
            var curr = sender.Parent as TreeViewItem;

            while (curr is not null)
            {
                res = curr.Header + "\\" + res;
                curr = curr.Parent as TreeViewItem;
            }
        });

        return res;
    }
}