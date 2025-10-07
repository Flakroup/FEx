using FEx.Abstractions.Interfaces;
using FEx.Extensions;
using Microsoft.CSharp;
using System.CodeDom;
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
            Dictionary<string, string> resourceEntries = GetResourcesEntries(path);

            //Modify resources here...
            foreach (KeyValuePair<string, string> entry in data)
            {
                if (!resourceEntries.ContainsValue(entry.Value))
                {
                    string temp = entry.Key;
                    var apx = 0;

                    while (resourceEntries.ContainsKey(temp))
                    {
                        apx++;
                        temp = entry.Key + apx;
                    }

                    resourceEntries.Add(temp, entry.Value);
                }
            }

            string directoryPath = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directoryPath))
                Directory.CreateDirectory(directoryPath);

            if (File.Exists(path))
                File.SetAttributes(path, FileAttributes.Normal);

            //Write the combined resource file
            var resourceWriter = new ResXResourceWriter(path);

            foreach (KeyValuePair<string, string> entry in resourceEntries)
                resourceWriter.AddResource(entry.Key, resourceEntries[entry.Key]);

            resourceWriter.Generate();
            resourceWriter.Close();
        }
    }

    public Dictionary<string, string> GetResourcesEntries(string path)
    {
        var resourceEntries = new Dictionary<string, string>();

        if (File.Exists(path))
        {
            //Get existing resources
            var reader = new ResXResourceReader(path);
            resourceEntries = new();

            foreach (DictionaryEntry entry in reader)
            {
                var key = entry.Key.ToString();
                string value = entry.Value?.ToString() ?? string.Empty;

                if (key.IsNotNullOrWhiteSpace()
                    && !resourceEntries.ContainsValue(value))
                    resourceEntries.Add(key, value);
            }

            reader.Close();
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
        var codeProvider = new CSharpCodeProvider();

        CodeCompileUnit code = StronglyTypedResourceBuilder.Create(resx,
            fileName,
            generatedCodeNamespace,
            codeProvider,
            false,
            out string[] unmatchedElements);

        using var writer = new StreamWriter(resxDesigner, false, Encoding.UTF8);
        codeProvider.GenerateCodeFromCompileUnit(code, writer, new());

        return unmatchedElements;
    }
}