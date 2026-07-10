using FEx.Core.Abstractions.Interfaces;
using Microsoft.CSharp;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Resources;
using System.Resources.Tools;
using System.Text;

namespace FEx.RESXx;

public class ResxManager : IResxManager
{
    public void UpdateResourceFile(List<KeyValuePair<string, string>> data, string path)
    {
        if (data.Count > 0)
        {
            var resourceEntries = GetResourcesEntries(path);

            //Modify resources here...
            foreach (var entry in data)
            {
                if (!resourceEntries.ContainsValue(entry.Value))
                {
                    var temp = entry.Key;
                    var apx = 0;

                    while (resourceEntries.ContainsKey(temp))
                    {
                        apx++;
                        temp = entry.Key + apx;
                    }

                    resourceEntries.Add(temp, entry.Value);
                }
            }

            var directoryPath = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directoryPath))
                Directory.CreateDirectory(directoryPath);

            if (File.Exists(path))
                File.SetAttributes(path, FileAttributes.Normal);

            //Write the combined resource file
            using var resourceWriter = new ResXResourceWriter(path);

            foreach (var entry in resourceEntries)
                resourceWriter.AddResource(entry.Key, resourceEntries[entry.Key]);

            resourceWriter.Generate();
        }
    }

    public Dictionary<string, string> GetResourcesEntries(string path)
    {
        var resourceEntries = new Dictionary<string, string>();

        if (File.Exists(path))
        {
            //Get existing resources
            using var reader = new ResXResourceReader(path);
            resourceEntries = new();

            foreach (DictionaryEntry entry in reader)
            {
                var key = entry.Key.ToString();
                var value = entry.Value?.ToString() ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(key)
                    && !resourceEntries.ContainsValue(value))
                    resourceEntries.Add(key, value);
            }
        }

        return resourceEntries;
    }

    /// <summary>
    /// Resgens the specified  file.
    /// </summary>
    /// <returns>System.String[].</returns>
    public string[] Resgen(string resx, string resxDesigner, string fileName, string projectNamespace)
    {
        var generatedCodeNamespace = $"{projectNamespace}.Properties";
        using var codeProvider = new CSharpCodeProvider();

        var code = StronglyTypedResourceBuilder.Create(resx,
            fileName,
            generatedCodeNamespace,
            codeProvider,
            false,
            out var unmatchedElements);

        using var writer = new StreamWriter(resxDesigner, false, Encoding.UTF8);
        codeProvider.GenerateCodeFromCompileUnit(code, writer, new());

        // StronglyTypedResourceBuilder.Create may set unmatchedElements to null; interface contract is non-null string[], so coalesce to empty.
        return unmatchedElements ?? [];
    }
}