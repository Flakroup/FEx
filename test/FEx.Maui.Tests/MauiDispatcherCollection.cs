namespace FEx.Maui.Tests;

/// <summary>The MAUI dispatcher provider is process-global state, so the tests that replace it run serially.</summary>
internal static class MauiDispatcherCollection
{
    public const string Name = "MAUI dispatcher provider";
}
