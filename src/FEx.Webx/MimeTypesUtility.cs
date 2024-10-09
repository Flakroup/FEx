using FEx.Common.Extensions;
using FEx.Extensions;
using FEx.Extensions.Base.Enums;
using FEx.Extensions.Collections;
using FEx.Extensions.Collections.Dictionaries;
using FEx.Platforms;
using FEx.Platforms.Abstractions.Interfaces;
using MimeMapping;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;

namespace FEx.Webx;

public static class MimeTypesUtility
{
    public static IRegistryService RegistryService => FExPlatforms.RegistryService;
    public static ConcurrentDictionary<string, string> TypeMap { get; }
    public static IReadOnlyDictionary<string, string> Mappings => TypeMap;

    static MimeTypesUtility()
    {
        try
        {
            var typeMap =
                new ConcurrentDictionary<string, string>(MimeUtility.TypeMap.ToDictionary(x => x.Key, x => x.Value));

            typeMap.TryAdd(".*", "application/octet-stream");

            TypeMap = typeMap;
        }
        catch
        {
            //ignored
        }
    }

    public static IReadOnlyCollection<string> GetDefaultExtensions(this MediaTypeHeaderValue contentType) =>
        GetDefaultExtensions(contentType?.MediaType);

    public static IReadOnlyCollection<string> GetDefaultExtensions(MediaTypes mediaType) =>
        GetDefaultExtensions(mediaType.GetEnumValueDescription());

    public static IReadOnlyCollection<string> GetDefaultExtensions(string mimeType)
    {
        ReadOnlyCollection<string> extensions = Mappings.IsNotNullOrEmptyReadOnlyCollection()
            ? Mappings.Where(x => x.Value == mimeType).Select(x => x.Key).ToList().AsReadOnly()
            : null;

        return extensions.IsNotNullOrEmptyReadOnlyCollection() ? extensions :
            RegistryService is not null ? RegistryService.GetDefaultExtension(mimeType).Yield().ToList().AsReadOnly() :
            Enumerable.Empty<string>().ToList().AsReadOnly();
    }

    public static string GetDefaultMimeType(string extension) =>
        Mappings.IsNotNullOrEmptyReadOnlyCollection()
            ? Mappings.TryGetReadOnlyKeyValue(extension)
            : RegistryService?.GetDefaultMimeType(extension);

    public static string GetDefaultExtension(this MediaTypeHeaderValue contentType) =>
        contentType.GetDefaultExtensions().FirstOrDefault();

    public static string GetDefaultExtension(string contentType, string fileName)
    {
        string urlExtension = Path.GetExtension(fileName);
        IReadOnlyCollection<string> webExtensions = GetDefaultExtensions(contentType);

        string extension = webExtensions.Contains(".*") && urlExtension.IsNotNullOrEmptyString()
            ? urlExtension
            : webExtensions.FindInEnumerable(x => x.IsEqual(urlExtension)) ?? webExtensions.FirstOrDefault();

        if (extension.IsNullOrEmptyString())
            extension = urlExtension;

        return extension.StartsWith(".")
            ? extension
            : "." + extension;
    }
}