using FEx.MVVM.Abstractions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace FEx.WPFx.Abstractions.Interfaces;

public interface ITreeViewBuilder
{
    void CreateNewNode(FExTreeViewNode node);
    void CreateNewNode(string nodePath);

    void AddChildNode(string rootNodeName,
                      List<string> nodePath,
                      string name,
                      bool unique,
                      string iconPath,
                      bool isIconAttachedToFile,
                      bool isExpanded);

    void AddChildNode(string rootNodeName,
                      string nodePath,
                      string name,
                      char pathSeparator,
                      bool unique,
                      string iconPath,
                      bool isExpanded);

    void AddChildNodes(string rootNodeName, IEnumerable<FExTreeViewNode> childNodes, bool unique);
}

public interface ITreeViewBuilder<TItem> : ITreeViewBuilder where TItem : HeaderedItemsControl, new()
{
    Task<TItem> GetTreeViewItemAsync(FExTreeViewNode nodeStub);
    Task GrowTreeAsync(ItemsControl tree, IReadOnlyList<TItem> curr, int i);

    /// <summary>
    /// Grows the tree.
    /// </summary>
    /// <param name="tree">The tree.</param>
    /// <param name="newNode">The new node.</param>
    /// <param name="location">The location.</param>
    /// <param name="i">The i.</param>
    Task GrowTreeAsync(ItemsControl tree, TItem newNode, int[] location, int i);

    /// <summary>
    /// Grows the tree.
    /// </summary>
    /// <param name="tree">The tree.</param>
    /// <param name="nodeStub">The node stub.</param>
    /// <param name="locationIndex">Index of the location.</param>
    /// <param name="setDirectoriesIcons">if set to <c>true</c> [set directories icons].</param>
    Task GrowTreeAsync(TItem tree, FExTreeViewNode nodeStub, int locationIndex, bool setDirectoriesIcons);

    Task GrowTreeAsync(TItem tree,
                       FExTreeViewNode nodeStub,
                       IList<string> headers,
                       int locationIndex,
                       bool setDirectoriesIcons);

    Task<TItem> GetTreeNodeAsync(string rootNodeName, bool setDirectoriesIcons);
}