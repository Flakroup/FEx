using FEx.Agnostics.Collections.Concurrent;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FEx.MVVM.Abstractions;

public class FExTreeViewNode : IEquatable<FExTreeViewNode>
{
    public static string[] ForbiddenItemNameChars { get; } =
    [
        "~", "`", "!", "@", "#", "$", "%", "^", "&", "*", "(", ")", "-", "+", "=", "[", "{", "]", "}", "\\", "|",
        "'", "\"", ";", ":", "/", "?", ".", ">", ",", "<", "©", " ", "—"
    ];

    public static string DefaultDirectoryPathForIcon { get; } = Path.GetTempPath();

    public bool IsIconAttachedToFile { get; }
    public List<string> NodePath { get; }
    public string NodeName { get; }
    public string NodeHeader { get; }
    public ConcurrentList<FExTreeViewNode> ChildNodes { get; }
    public string? IconPath { get; }
    public bool IsExpanded { get; }

    public FExTreeViewNode(string path)
        : this(path, null, '\\', null, true, false)
    {
    }

    public FExTreeViewNode(string path, string? name)
        : this(path, name, '\\', null, true, false)
    {
    }

    public FExTreeViewNode(string path, string? name, char pathSeparator)
        : this(path, name, pathSeparator, null, true, false)
    {
    }

    public FExTreeViewNode(string path,
                           string? name,
                           char pathSeparator,
                           string? iconPath,
                           bool isIconAttachedToFile,
                           bool isExpanded)
        : this(GetNodePath(path, pathSeparator), name, iconPath, isIconAttachedToFile, isExpanded)
    {
    }

    public FExTreeViewNode(List<string> path)
        : this(path, null, null, true, false)
    {
    }

    public FExTreeViewNode(List<string> path, string? name)
        : this(path, name, null, true, false)
    {
    }

    public FExTreeViewNode(List<string> path, string? name, string? iconPath)
        : this(path, name, iconPath, true, false)
    {
    }

    public FExTreeViewNode(List<string> path, string? name, string? iconPath, bool isIconAttachedToFile, bool isExpanded)
    {
        ChildNodes = [];
        NodeHeader = path.Last();
        //path.RemoveAt(path.Count - 1);
        NodePath = path;
        name ??= NodeHeader;

        NodeName = FixTreeViewItemName(name);
        IconPath = iconPath;
        IsIconAttachedToFile = isIconAttachedToFile;
        IsExpanded = isExpanded;
    }

    public static List<string> GetNodePath(string nodePath) => GetNodePath(nodePath, '\\');

    public static List<string> GetNodePath(string nodePath, char pathSeparator) =>
        pathSeparator != '\0'
            ? [.. nodePath.Split(pathSeparator)]
            :
            [
                nodePath
            ];

    public static string FixTreeViewItemName(string name) => FixTreeViewItemName(name, "_");

    public static string FixTreeViewItemName(string name, string replacement)
    {
        name = ForbiddenItemNameChars.Aggregate(name, (current, ch) => current.Replace(ch, replacement));

        return $"_{name}";
    }

    public string? GetIconCacheKey()
    {
        if (IconPath is null)
            return null;

        return IsIconAttachedToFile
            ? Path.GetExtension(IconPath)
            : IconPath;
    }

    public void AddChildNode(string nodePath) => AddChildNode(nodePath, null, '\\', true, null, false);

    public void AddChildNode(string nodePath,
                             string? name,
                             char pathSeparator,
                             bool unique,
                             string? iconPath,
                             bool isExpanded) =>
        AddChildNode(GetNodePath(nodePath, pathSeparator), name, unique, iconPath, true, isExpanded);

    public void AddChildNode(List<string> nodePath) => AddChildNode(nodePath, null, true, null, true, false);

    public void AddChildNode(List<string> nodePath,
                             string? name,
                             bool unique,
                             string? iconPath,
                             bool isIconAttachedToFile,
                             bool isExpanded)
    {
        var node = new FExTreeViewNode(nodePath, name, iconPath, isIconAttachedToFile, isExpanded);

        if (unique) //todo detect uniquity before object creation
            ChildNodes.AddUnique(node);
        else
            ChildNodes.Add(node);
    }

    public void AddChildNodes(IEnumerable<FExTreeViewNode> nodes) => AddChildNodes(nodes, true);

    public void AddChildNodes(IEnumerable<FExTreeViewNode> nodes, bool unique)
    {
        if (unique)
            ChildNodes.AddUniqueRange(nodes);
        else
            ChildNodes.AddRange(nodes);
    }

    #region IEquatable
    public bool Equals(FExTreeViewNode? other)
    {
        if (other is null)
            return false;

        return ReferenceEquals(this, other) || NodePath.SequenceEqual(other.NodePath);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        return obj.GetType() == GetType() && Equals((FExTreeViewNode)obj);
    }

    public override int GetHashCode()
    {
        if (NodePath is null)
            return 0;

#if NETSTANDARD
        unchecked
        {
            var hash = 17;

            foreach (var segment in NodePath)
                hash = hash * 31 + (segment?.GetHashCode() ?? 0);

            return hash;
        }
#else
        var hash = new HashCode();

        foreach (var segment in NodePath)
            hash.Add(segment);

        return hash.ToHashCode();
#endif
    }
    #endregion
}