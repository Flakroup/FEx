using System;

namespace FEx.Agnostics.Abstractions.Utilities.OS;

internal class OSVersion : IEquatable<OSVersion>
{
    private int Major { get; }
    private int? Minor { get; }
    private int? ProductType { get; }

    public OSVersion(int majorVersion, int minorVersion, int productType)
    {
        Major = majorVersion;
        Minor = minorVersion;
        ProductType = productType;
    }

    public bool Equals(OSVersion other)
    {
        if (other is null)
            return false;

        return ReferenceEquals(this, other)
               || Major == other.Major && Minor == other.Minor && ProductType == other.ProductType;
    }

    public static bool operator ==(OSVersion left, OSVersion right) => Equals(left, right);

    public static bool operator !=(OSVersion left, OSVersion right) => !Equals(left, right);

    public override string ToString() =>
        Major switch
        {
            3 => "Windows NT 3.51",
            4 => ProductType switch
            {
                1 => "Windows NT 4.0",
                3 => "Windows NT 4.0 Server",
                _ => "unknown"
            },
            5 => Minor switch
            {
                0 => "Windows 2000",
                1 => "Windows XP",
                2 => "Windows Server 2003",
                _ => "unknown"
            },
            6 => Minor switch
            {
                0 => ProductType switch
                {
                    1 => "Windows Vista",
                    3 => "Windows Server 2008",
                    _ => "unknown"
                },
                1 => ProductType switch
                {
                    1 => "Windows 7",
                    3 => "Windows Server 2008 R2",
                    _ => "unknown"
                },
                2 => ProductType switch
                {
                    1 => "Windows 8",
                    3 => "Windows Server 2012",
                    _ => "unknown"
                },
                3 => ProductType switch
                {
                    1 => "Windows 8.1",
                    3 => "Windows Server 2012 R2",
                    _ => "unknown"
                },
                _ => "unknown"
            },
            10 => Minor switch
            {
                0 => ProductType switch
                {
                    1 => "Windows 10",
                    3 => "Windows Server 2016",
                    _ => "unknown"
                },
                _ => "unknown"
            },
            _ => "unknown"
        };

    public override bool Equals(object obj)
    {
        if (obj is null)
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        return obj.GetType() == GetType() && Equals((OSVersion)obj);
    }

    public override int GetHashCode()
#if NETSTANDARD
    {
        unchecked
        {
            var hashCode = Major;
            hashCode = hashCode * 397 ^ Minor.GetHashCode();
            hashCode = hashCode * 397 ^ ProductType.GetHashCode();

            return hashCode;
        }
    }
#else
        =>
            HashCode.Combine(Major, Minor, ProductType);
#endif
}