using FEx.Basics.Collections.Concurrent;
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
    public string IconPath { get; }
    public bool IsExpanded { get; }

    public FExTreeViewNode(string path,
                           string name = null,
                           char pathSeparator = '\\',
                           string iconPath = null,
                           bool isIconAttachedToFile = true,
                           bool isExpanded = false)
        : this(GetNodePath(path, pathSeparator), name, iconPath, isIconAttachedToFile, isExpanded)
    {
    }

    public FExTreeViewNode(List<string> path,
                           string name = null,
                           string iconPath = null,
                           bool isIconAttachedToFile = true,
                           bool isExpanded = false)
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

    public static List<string> GetNodePath(string nodePath, char pathSeparator = '\\') =>
        pathSeparator != default(char)
            ? [.. nodePath.Split(pathSeparator)]
            :
            [
                nodePath
            ];

    public static string FixTreeViewItemName(string name, string replacement = "_")
    {
        name = ForbiddenItemNameChars.Aggregate(name, (current, ch) => current.Replace(ch, replacement));

        return $"_{name}";
    }

    public string GetIconCacheKey()
    {
        if (IconPath is null)
            return null;

        return IsIconAttachedToFile
            ? Path.GetExtension(IconPath)
            : IconPath;
    }

    public void AddChildNode(string nodePath,
                             string name = null,
                             char pathSeparator = '\\',
                             bool unique = true,
                             string iconPath = null,
                             bool isExpanded = false) =>
        AddChildNode(GetNodePath(nodePath, pathSeparator), name, unique, iconPath, isExpanded);

    public void AddChildNode(List<string> nodePath,
                             string name = null,
                             bool unique = true,
                             string iconPath = null,
                             bool isIconAttachedToFile = true,
                             bool isExpanded = false)
    {
        var node = new FExTreeViewNode(nodePath, name, iconPath, isIconAttachedToFile, isExpanded);

        if (unique) //todo detect uniquity before object creation
            ChildNodes.AddUnique(node);
        else
            ChildNodes.Add(node);
    }

    public void AddChildNodes(IEnumerable<FExTreeViewNode> nodes, bool unique = true)
    {
        if (unique)
            ChildNodes.AddUniqueRange(nodes);
        else
            ChildNodes.AddRange(nodes);
    }

    #region IEquatable
    public bool Equals(FExTreeViewNode other)
    {
        if (other is null)
            return false;

        return ReferenceEquals(this, other) || NodePath.SequenceEqual(other.NodePath);
    }

    public override bool Equals(object obj)
    {
        if (obj is null)
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        return obj.GetType() == GetType() && Equals((FExTreeViewNode)obj);
    }

    public override int GetHashCode() => NodePath?.GetHashCode() ?? 0;
    #endregion
}