using System.Collections.Generic;

namespace FEx.Platforms.Windows.Models;

public class ACL
{
    public string Path { get; }
    public bool HasEveryoneAccess { get; }
    public bool HasEveryoneOwner { get; }

    public ACL(IReadOnlyList<string> splitted)
        : this(splitted[0], bool.Parse(splitted[1]), bool.Parse(splitted[2]))
    {
    }

    public ACL(string v1, bool v2, bool v3)
    {
        Path = v1;
        HasEveryoneAccess = v2;
        HasEveryoneOwner = v3;
    }

    public override string ToString() => $"{Path}|{HasEveryoneAccess}|{HasEveryoneOwner}";
}