using System;
using System.IO;
using System.Security;

namespace FEx.FileSystem;

/// <summary>Provides file and directory enumeration options.</summary>
public class FExEnumerationOptions
{
    private int _maxRecursionDepth;

    internal const int DefaultMaxRecursionDepth = int.MaxValue;

    /// <summary>
    /// For internal use. These are the options we want to use if calling the existing Directory/File APIs where you don't
    /// explicitly specify EnumerationOptions.
    /// </summary>
    internal static FExEnumerationOptions Compatible { get; } = new()
    {
        UseSimpleMatching = false,
        AttributesToSkip = 0,
        IgnoreInaccessible = false
    };

    internal static FExEnumerationOptions CompatibleRecursive { get; } = new()
    {
        RecurseSubdirectories = true,
        UseSimpleMatching = false,
        AttributesToSkip = 0,
        IgnoreInaccessible = false
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="EnumerationOptions" /> class with the recommended default
    /// options.
    /// </summary>
    public FExEnumerationOptions()
    {
        IgnoreInaccessible = true;
        AttributesToSkip = FileAttributes.Hidden | FileAttributes.System;
        MaxRecursionDepth = DefaultMaxRecursionDepth;
        UseSimpleMatching = true;
    }

    /// <summary>
    /// Gets or sets a value that indicates whether to recurse into subdirectories while enumerating. The default is
    /// <see langword="false" />.
    /// </summary>
    /// <value><see langword="true" /> to recurse into subdirectories; otherwise, <see langword="false" />.</value>
    public bool RecurseSubdirectories { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates whether to skip files or directories when access is denied (for example,
    /// <see cref="UnauthorizedAccessException" /> or <see cref="SecurityException" />). The default is
    /// <see langword="true" />.
    /// </summary>
    /// <value><see langword="true" /> to skip inaccessible files or directories; otherwise, <see langword="false" />.</value>
    public bool IgnoreInaccessible { get; set; }

    /// <summary>Gets or sets the suggested buffer size, in bytes. The default is 0 (no suggestion).</summary>
    /// <value>The buffer size.</value>
    /// <remarks>
    /// Not all platforms use user allocated buffers, and some require either fixed buffers or a buffer that has enough
    /// space to return a full result.
    /// One scenario where this option is useful is with remote share enumeration on Windows. Having a large buffer may
    /// result in better performance as more results can be batched over the wire (for example, over a network share).
    /// A "large" buffer, for example, would be 16K. Typical is 4K.
    /// The suggested buffer size will not be used if it has no meaning for the native APIs on the current platform or if
    /// it would be too small for getting at least a single result.
    /// </remarks>
    public int BufferSize { get; set; }

    /// <summary>Gets or sets the attributes to skip. The default is <c>FileAttributes.Hidden | FileAttributes.System</c>.</summary>
    /// <value>The attributes to skip.</value>
    public FileAttributes AttributesToSkip { get; set; }

    /// <summary>Gets or sets whether to use case-sensitive matching.</summary>
    /// <value>True for case-sensitive matching, false otherwise.</value>
    /// <remarks>
    /// For APIs that allow specifying a match expression, this property allows you to specify the case matching behavior.
    /// The default is to match platform defaults.
    /// </remarks>
    public bool CaseSensitive { get; set; }

    /// <summary>Gets or sets whether to use simple wildcard matching.</summary>
    /// <value>True for simple matching where '*' is 0+ chars and '?' is 1 char, false for regex.</value>
    /// <remarks>
    /// For APIs that allow specifying a match expression, this property allows you to specify how to interpret the match
    /// expression. The default is simple matching.
    /// </remarks>
    public bool UseSimpleMatching { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates the maximum directory depth to recurse while enumerating, when
    /// <see cref="RecurseSubdirectories" /> is set to <see langword="true" />.
    /// </summary>
    /// <value>
    /// A number that represents the maximum directory depth to recurse while enumerating. The default value is
    /// <see cref="int.MaxValue" />.
    /// </value>
    /// <remarks>If <see cref="MaxRecursionDepth" /> is set to zero, enumeration returns the contents of the initial directory.</remarks>
    public int MaxRecursionDepth
    {
        get => _maxRecursionDepth;
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            _maxRecursionDepth = value;
        }
    }

    /// <summary>Gets or sets a value that indicates whether to return the special directory entries "." and "..".</summary>
    /// <value>
    /// <see langword="true" /> to return the special directory entries "." and ".."; otherwise,
    /// <see langword="false" />.
    /// </value>
    public bool ReturnSpecialDirectories { get; set; }

#if !NETSTANDARD2_0
    /// <summary>
    /// Converts current <see cref="FExEnumerationOptions" /> instance to the runtime <see cref="EnumerationOptions" />
    /// which is supported on <c>net6.0+</c> and therefore on <c>net9.0</c> that this solution targets.
    /// </summary>
    /// <returns>Fully populated <see cref="EnumerationOptions" />.</returns>
    public EnumerationOptions ToEnumerationOptions()
    {
        var eo = new EnumerationOptions
        {
            RecurseSubdirectories = RecurseSubdirectories,
            IgnoreInaccessible = IgnoreInaccessible,
            BufferSize = BufferSize,
            AttributesToSkip = AttributesToSkip
        };

#if NETCOREAPP
        eo.ReturnSpecialDirectories = ReturnSpecialDirectories;
        eo.MaxRecursionDepth = MaxRecursionDepth;

        eo.MatchCasing = CaseSensitive
            ? MatchCasing.CaseSensitive
            : MatchCasing.PlatformDefault;

        eo.MatchType = UseSimpleMatching
            ? MatchType.Simple
            : MatchType.Win32;
#endif

        return eo;
    }

    /// <summary>
    /// Implicit conversion operator so the wrapper can be passed directly where <see cref="EnumerationOptions" /> is expected.
    /// </summary>
    public static implicit operator EnumerationOptions(FExEnumerationOptions options) => options.ToEnumerationOptions();
#else
    // .NET Standard 2.0 / 2.1 do not expose EnumerationOptions. We still provide the method
    // so that multi-targeted source can compile, but the implementation merely returns <c>null</c>.
    // Callers must branch on target framework when using the result.
    public object ToEnumerationOptions() => null;
#endif
}