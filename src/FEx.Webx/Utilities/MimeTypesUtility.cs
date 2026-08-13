using FEx.Agnostics.Abstractions.Enums;
using FEx.Agnostics.Abstractions.Extensions;
using FEx.Agnostics.Abstractions.Extensions.Collections.Dictionaries;
using FEx.Platforms;
using FEx.Platforms.Abstractions.Interfaces;
using MimeMapping;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;

namespace FEx.Webx.Utilities;

public static class MimeTypesUtility
{
    public static IRegistryService RegistryService => FExPlatforms.RegistryService;

    // Nullable: the static constructor leaves TypeMap unset if building the map throws (catch swallows it).
    public static ConcurrentDictionary<string, string?>? TypeMap { get; }
    public static IReadOnlyDictionary<string, string?>? Mappings => TypeMap;

    static MimeTypesUtility()
    {
        try
        {
            var typeMap =
                new ConcurrentDictionary<string, string?>(MimeUtility.TypeMap.ToDictionary(x => x.Key, x => x.Value));

            typeMap.TryAdd(".*", "application/octet-stream");

            TypeMap = typeMap;
        }
        catch
        {
            //ignored
        }
    }

    public static IReadOnlyCollection<string> GetDefaultExtensions(this MediaTypeHeaderValue? contentType) =>
        GetDefaultExtensions(contentType?.MediaType);

    public static IReadOnlyCollection<string> GetDefaultExtensions(MediaTypes mediaType) =>
        GetDefaultExtensions(mediaType.GetEnumValueDescription());

    public static IReadOnlyCollection<string> GetDefaultExtensions(string? mimeType)
    {
        var extensions = Mappings is { Count: > 0 }
            ? Mappings.Where(x => x.Value == mimeType).Select(x => x.Key).ToList().AsReadOnly()
            : null;

        return extensions is { Count: > 0 }
            ? extensions
            : RegistryService is not null && RegistryService.GetDefaultExtension(mimeType) is { } registryExtension
                ? registryExtension.Yield().ToList().AsReadOnly()
                : Enumerable.Empty<string>().ToList().AsReadOnly();
    }

    public static string? GetDefaultMimeType(string extension) =>
        Mappings is { Count: > 0 }
            ? Mappings.TryGetReadOnlyKeyValue(extension)
            : RegistryService?.GetDefaultMimeType(extension);

    public static string? GetDefaultExtension(this MediaTypeHeaderValue? contentType) =>
        contentType.GetDefaultExtensions().FirstOrDefault();

    public static string GetDefaultExtension(string? contentType, string fileName)
    {
        var urlExtension = Path.GetExtension(fileName);
        var webExtensions = GetDefaultExtensions(contentType);

        var extension = webExtensions.Contains(".*") && urlExtension.IsNotNullOrEmptyString()
            ? urlExtension
            : webExtensions.FindInEnumerable(x => x.IsEqual(urlExtension)) ?? webExtensions.FirstOrDefault();

        if (extension.IsNullOrEmptyString())
            extension = urlExtension;

        return extension.StartsWith(".")
            ? extension
            : "." + extension;
    }
}