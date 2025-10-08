using System.Collections.Generic;

namespace FEx.Core.Abstractions.Interfaces;

public interface IResxManager
{
    void UpdateResourceFile(List<KeyValuePair<string, string>> data, string path);
    string[] Resgen(string resx, string resxDesigner, string fileName, string projectNamespace);
    Dictionary<string, string> GetResourcesEntries(string path);
}