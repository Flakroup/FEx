using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace FEx.AppSettings.Abstractions.Interfaces;

public interface IConfigurationService
{
    Dictionary<string, string> AppSettings { get; }
    IConfigurationRoot Configuration { get; }

    void Build(IEnumerable<IConfigurationSource> sources);
    bool? GetBoolSetting(string key, bool? defaultValue);
    T GetSetting<T>(string key, Func<string, T> func);
}