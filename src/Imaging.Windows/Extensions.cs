using FEx.Imaging.Windows.Model;

namespace FEx.Imaging.Windows;

public static class Extensions
{
    public static bool DoesCacheExists(this IndexEntry entry)
    {
        entry?.Cache?.Refresh();

        return entry?.Cache?.Exists ?? false;
    }
}