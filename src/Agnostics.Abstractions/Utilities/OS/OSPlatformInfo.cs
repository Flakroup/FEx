namespace FEx.Agnostics.Abstractions.Utilities.OS;

/// <summary>Operating system families that can be detected at run time.</summary>
public enum OSPlatformInfo
{
    /// <summary>The platform could not be determined.</summary>
    Unknown,
    /// <summary>Microsoft Windows.</summary>
    Windows,
    /// <summary>Linux.</summary>
    Linux,
    /// <summary>Apple macOS.</summary>
    OSX,
    /// <summary>Google Android.</summary>
    Android,
    /// <summary>Apple iOS.</summary>
    IOS,
    /// <summary>A browser (WebAssembly) host.</summary>
    Browser
}