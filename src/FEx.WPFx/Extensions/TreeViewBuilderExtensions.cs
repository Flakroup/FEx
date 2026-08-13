using FEx.MVVM.Abstractions;
using FEx.WPFx.Abstractions.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace FEx.WPFx.Extensions;

public static class TreeViewBuilderExtensions
{
    public static void AddChildNode(this ITreeViewBuilder builder, string rootNodeName, List<string> nodePath) =>
        builder.AddChildNode(rootNodeName, nodePath, null, true, null, true, false);

    public static void AddChildNode(this ITreeViewBuilder builder,
                                    string rootNodeName,
                                    List<string> nodePath,
                                    string name) =>
        builder.AddChildNode(rootNodeName, nodePath, name, true, null, true, false);

    public static void AddChildNode(this ITreeViewBuilder builder,
                                    string rootNodeName,
                                    List<string> nodePath,
                                    string name,
                                    bool unique) =>
        builder.AddChildNode(rootNodeName, nodePath, name, unique, null, true, false);

    public static void AddChildNode(this ITreeViewBuilder builder,
                                    string rootNodeName,
                                    List<string> nodePath,
                                    string name,
                                    bool unique,
                                    string iconPath) =>
        builder.AddChildNode(rootNodeName, nodePath, name, unique, iconPath, true, false);

    public static void AddChildNode(this ITreeViewBuilder builder,
                                    string rootNodeName,
                                    List<string> nodePath,
                                    string name,
                                    bool unique,
                                    string iconPath,
                                    bool isIconAttachedToFile) =>
        builder.AddChildNode(rootNodeName, nodePath, name, unique, iconPath, isIconAttachedToFile, false);

    public static void AddChildNode(this ITreeViewBuilder builder, string rootNodeName, string nodePath) =>
        builder.AddChildNode(rootNodeName, nodePath, null, '\\', true, null, false);

    public static void AddChildNode(this ITreeViewBuilder builder, string rootNodeName, string nodePath, string name) =>
        builder.AddChildNode(rootNodeName, nodePath, name, '\\', true, null, false);

    public static void AddChildNode(this ITreeViewBuilder builder,
                                    string rootNodeName,
                                    string nodePath,
                                    string name,
                                    char pathSeparator) =>
        builder.AddChildNode(rootNodeName, nodePath, name, pathSeparator, true, null, false);

    public static void AddChildNode(this ITreeViewBuilder builder,
                                    string rootNodeName,
                                    string nodePath,
                                    string name,
                                    char pathSeparator,
                                    bool unique) =>
        builder.AddChildNode(rootNodeName, nodePath, name, pathSeparator, unique, null, false);

    public static void AddChildNode(this ITreeViewBuilder builder,
                                    string rootNodeName,
                                    string nodePath,
                                    string name,
                                    char pathSeparator,
                                    bool unique,
                                    string iconPath) =>
        builder.AddChildNode(rootNodeName, nodePath, name, pathSeparator, unique, iconPath, false);

    public static void AddChildNodes(this ITreeViewBuilder builder,
                                     string rootNodeName,
                                     IEnumerable<FExTreeViewNode> childNodes) =>
        builder.AddChildNodes(rootNodeName, childNodes, true);

    public static Task GrowTreeAsync<TItem>(this ITreeViewBuilder<TItem> builder,
                                            ItemsControl tree,
                                            IReadOnlyList<TItem> curr) where TItem : HeaderedItemsControl, new() =>
        builder.GrowTreeAsync(tree, curr, 0);

    public static Task GrowTreeAsync<TItem>(this ITreeViewBuilder<TItem> builder,
                                            ItemsControl tree,
                                            TItem newNode,
                                            int[] location) where TItem : HeaderedItemsControl, new() =>
        builder.GrowTreeAsync(tree, newNode, location, 0);

    public static Task GrowTreeAsync<TItem>(this ITreeViewBuilder<TItem> builder, TItem tree, FExTreeViewNode nodeStub)
        where TItem : HeaderedItemsControl, new() =>
        builder.GrowTreeAsync(tree, nodeStub, 0, false);

    public static Task GrowTreeAsync<TItem>(this ITreeViewBuilder<TItem> builder,
                                            TItem tree,
                                            FExTreeViewNode nodeStub,
                                            int locationIndex) where TItem : HeaderedItemsControl, new() =>
        builder.GrowTreeAsync(tree, nodeStub, locationIndex, false);

    public static Task GrowTreeAsync<TItem>(this ITreeViewBuilder<TItem> builder,
                                            TItem tree,
                                            FExTreeViewNode nodeStub,
                                            IList<string> headers) where TItem : HeaderedItemsControl, new() =>
        builder.GrowTreeAsync(tree, nodeStub, headers, 0, false);

    public static Task GrowTreeAsync<TItem>(this ITreeViewBuilder<TItem> builder,
                                            TItem tree,
                                            FExTreeViewNode nodeStub,
                                            IList<string> headers,
                                            int locationIndex) where TItem : HeaderedItemsControl, new() =>
        builder.GrowTreeAsync(tree, nodeStub, headers, locationIndex, false);

    public static Task<TItem> GetTreeNodeAsync<TItem>(this ITreeViewBuilder<TItem> builder, string rootNodeName)
        where TItem : HeaderedItemsControl, new() =>
        builder.GetTreeNodeAsync(rootNodeName, false);
}