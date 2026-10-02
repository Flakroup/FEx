using Shouldly;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using Xunit;

namespace FEx.WPFx.Tests;

public class FileSystemIconsProviderTests
{
    private const uint GdiObjects = 0;

    [DllImport("user32.dll")]
    private static extern uint GetGuiResources(IntPtr hProcess, uint uiFlags);

    private static uint GdiObjectCount()
    {
        using var process = Process.GetCurrentProcess();

        return GetGuiResources(process.Handle, GdiObjects);
    }

    [Fact]
    public void CreateFrozenBitmapSource_ReleasesGdiHandles() =>
        StaTestRunner.Run(() =>
        {
            using var bitmap = new Bitmap(16, 16);
            FileSystemIconsProvider.CreateFrozenBitmapSource(bitmap); // warm up lazy WPF/GDI state

            var before = GdiObjectCount();

            for (var i = 0; i < 100; i++)
            {
                var source = FileSystemIconsProvider.CreateFrozenBitmapSource(bitmap);
                source.IsFrozen.ShouldBeTrue();
            }

            GdiObjectCount().ShouldBeLessThan(before + 10);
        });
}
