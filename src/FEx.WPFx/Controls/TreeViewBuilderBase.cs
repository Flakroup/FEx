using FEx.Agnostics.Abstractions.Extensions;
using FEx.Core.Abstractions.Interfaces;
using FEx.MVVM.Abstractions;
using FEx.WPFx.Abstractions.Interfaces;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace FEx.WPFx.Controls;

public abstract class TreeViewBuilderBase<TItem> : ITreeViewBuilder<TItem> where TItem : HeaderedItemsControl, new()
{
    protected readonly IFExDispatcher _dispatcher;
    protected readonly FileSystemIconsProvider _fileSystemIconsProvider;

    protected ConcurrentDictionary<string, FExTreeViewNode> TreeNodes { get; }

    protected TreeViewBuilderBase(FileSystemIconsProvider fileSystemIconsProvider, IFExDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        _fileSystemIconsProvider = fileSystemIconsProvider;
        TreeNodes = new();
    }

    public void CreateNewNode(FExTreeViewNode node) => TreeNodes.TryAdd(node.NodeHeader, node);

    public void AddChildNode(string rootNodeName,
                             List<string> nodePath,
                             string name = null,
                             bool unique = true,
                             string iconPath = null,
                             bool isIconAttachedToFile = true,
                             bool isExpanded = false)
    {
        FExTreeViewNode rootNodeStub = GetRootNodeStub(rootNodeName);
        rootNodeStub.AddChildNode(nodePath, name, unique, iconPath, isIconAttachedToFile, isExpanded);
    }

    public void AddChildNodes(string rootNodeName, IEnumerable<FExTreeViewNode> childNodes, bool unique = true)
    {
        FExTreeViewNode rootNodeStub = GetRootNodeStub(rootNodeName);
        rootNodeStub.AddChildNodes(childNodes, unique);
    }

    public void CreateNewNode(string nodePath) => CreateNewNode(new FExTreeViewNode(nodePath));

    public void AddChildNode(string rootNodeName,
                             string nodePath,
                             string name = null,
                             char pathSeparator = '\\',
                             bool unique = true,
                             string iconPath = null,
                             bool isExpanded = false) =>
        AddChildNode(rootNodeName,
            FExTreeViewNode.GetNodePath(nodePath, pathSeparator),
            name,
            unique,
            iconPath,
            isExpanded);

    public abstract Task<TItem> GetTreeViewItemAsync(FExTreeViewNode nodeStub);

    public async Task GrowTreeAsync(ItemsControl tree, IReadOnlyList<TItem> curr, int i = 0)
    {
        TItem[] items = tree.Items.OfType<TItem>().ToArray();

        if (items.None(x => x.Header == curr[i].Header))
            tree.Items.Add(curr[i]);

        if (i < curr.Count - 1)
        {
            int j = items.IndexWhere(x => x.Header == curr[i].Header);
            await GrowTreeAsync((TItem)tree.Items[j], curr, i + 1);
        }
    }

    /// <summary>
    /// Grows the tree.
    /// </summary>
    /// <param name="tree">The tree.</param>
    /// <param name="newNode">The new node.</param>
    /// <param name="location">The location.</param>
    /// <param name="i">The i.</param>
    public async Task GrowTreeAsync(ItemsControl tree, TItem newNode, int[] location, int i = 0)
    {
        while (location[i] > tree.Items.Count) //todo is it necessary
            tree.Items.Add(new TItem());

        if (i == location.Length - 1)
            tree.Items.Insert(location[i], newNode);
        else
            await GrowTreeAsync((TItem)tree.Items[location[i]], newNode, location, i + 1);
    }

    /// <summary>
    /// Grows the tree.
    /// </summary>
    /// <param name="tree">The tree.</param>
    /// <param name="nodeStub">The node stub.</param>
    /// <param name="locationIndex">Index of the location.</param>
    /// <param name="setDirectoriesIcons">if set to <c>true</c> [set directories icons].</param>
    public async Task GrowTreeAsync(TItem tree,
                                    FExTreeViewNode nodeStub,
                                    int locationIndex = 0,
                                    bool setDirectoriesIcons = false)
    {
        // //string header = PostInContext(() => tree.Header.ToString());
        //
        // TItem node = await _dispatcher.InvokeOnMainThreadAsync(() =>
        // {
        //     return tree.Items.Cast<TItem>()
        //         .FirstOrDefault(x => x.Header.ToString() == nodeStub.NodePath[locationIndex]);
        // });
        //
        // if (node is null)
        // {
        //     node = await GetTreeViewItemAsync(nodeStub);
        //     await _dispatcher.InvokeOnMainThreadAsync(() => tree.Items.Add(node));
        // }
        //
        // //await LockService.Instance.WaitAsync(header);
        //
        // if (locationIndex != nodeStub.NodePath.Count - 1)
        //     await GrowTreeAsync(node, nodeStub, locationIndex + 1);
        //
        // //LockService.Instance.Release(header);

        List<string> headers = await _dispatcher.InvokeOnMainThreadAsync(() => tree.Items.OfType<TItem>()
            .Select(x => x.Header.ToString())
            .ToList());

        await GrowTreeAsync(tree, nodeStub, headers, locationIndex, setDirectoriesIcons);
    }

    public async Task GrowTreeAsync(TItem tree,
                                    FExTreeViewNode nodeStub,
                                    IList<string> headers,
                                    int locationIndex = 0,
                                    bool setDirectoriesIcons = false)
    {
        if (tree != null)
        {
            string header = nodeStub.NodePath[locationIndex];
            TItem node = null;
            int idx = headers.IndexOf(header);

            if (idx > -1)
                node = await _dispatcher.InvokeOnMainThreadAsync(() => tree.Items[idx] as TItem);

            if (node == null)
            {
                if (locationIndex == nodeStub.NodePath.Count - 1)
                    node = await GetTreeViewItemAsync(nodeStub);
                else
                    node = await GetTreeViewItemAsync(new(nodeStub.NodePath.Take(locationIndex + 1).ToList(),
                        null,
                        setDirectoriesIcons
                            ? FExTreeViewNode.DefaultDirectoryPathForIcon
                            : null));

                headers.Add(header);

                await _dispatcher.InvokeOnMainThreadAsync(() =>
                {
                    if (headers.Count - 1 < tree.Items.Count)
                        tree.Items.Insert(headers.Count - 1, node);
                });
            }

            //await LockSrv.WaitAsync(header);

            if (locationIndex != nodeStub.NodePath.Count - 1)
                await GrowTreeAsync(node, nodeStub, headers, locationIndex + 1, setDirectoriesIcons);

            //LockSrv.Release(header);
        }
    }

    public async Task<TItem> GetTreeNodeAsync(string rootNodeName, bool setDirectoriesIcons = false)
    {
        FExTreeViewNode rootNode = GetRootNodeStub(rootNodeName);
        TItem res = await GetTreeViewItemAsync(rootNode);

        var leafsDictionary = new ConcurrentDictionary<FExTreeViewNode, TItem>();
        var parentsDictionary = new ConcurrentDictionary<FExTreeViewNode, TItem>();

        if (rootNode.ChildNodes.Any())
        {
            await Task.WhenAll(rootNode.ChildNodes.Select(x => PutNewNodeAsync(x, leafsDictionary)).ToArray());
            rootNode.ChildNodes.Clear();
        }

        while (leafsDictionary.Count > 0)
        {
            parentsDictionary.Clear();

            foreach (KeyValuePair<FExTreeViewNode, TItem> node in leafsDictionary)
            {
                if (node.Key.NodePath.Count > 1)
                {
                    var stub = new FExTreeViewNode(node.Key.NodePath.GetRange(0, node.Key.NodePath.Count - 1),
                        null,
                        setDirectoriesIcons
                            ? FExTreeViewNode.DefaultDirectoryPathForIcon
                            : null);

                    if (!parentsDictionary.ContainsKey(stub))
                        await PutNewNodeAsync(stub, parentsDictionary);

                    await _dispatcher.InvokeOnMainThreadAsync(() => parentsDictionary[stub].Items.Add(node.Value));
                }
                else
                {
                    await _dispatcher.InvokeOnMainThreadAsync(() => res.Items.Add(node.Value));
                }
            }

            leafsDictionary.Clear();

            foreach (KeyValuePair<FExTreeViewNode, TItem> parent in parentsDictionary)
                leafsDictionary.AddOrUpdateValue(parent.Key, parent.Value);
        }

        return res;
    }

    protected virtual async Task<BitmapSource> GetBitmapSourceAsync(FExTreeViewNode nodeStub) =>
        nodeStub.IconPath is not null
            ? await _fileSystemIconsProvider.GetFileIconAsync(nodeStub.IconPath, nodeStub.IsIconAttachedToFile)
            : null;

    protected FExTreeViewNode GetRootNodeStub(string rootNodeName) =>
        TreeNodes.GetOrAdd(rootNodeName, new FExTreeViewNode(rootNodeName));

    private async Task PutNewNodeAsync(FExTreeViewNode treeNodeStub,
                                       ConcurrentDictionary<FExTreeViewNode, TItem> nodesDictionary)
    {
        TItem item = await GetTreeViewItemAsync(treeNodeStub);
        nodesDictionary.AddOrUpdateValue(treeNodeStub, item);
    }
}