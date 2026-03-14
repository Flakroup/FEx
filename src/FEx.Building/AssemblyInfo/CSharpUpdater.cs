using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using static Serilog.Log;

namespace FEx.Building.AssemblyInfo;

public class CSharpUpdater
{
    private readonly List<ICSharpUpdateRule> _updateRules;

    public CSharpUpdater(string newAssemblyVersion,
                         string? newAssemblyFileVersion = null,
                         string? copyright = null,
                         string? company = null)
    {
        _updateRules = [];

        if (!string.IsNullOrEmpty(newAssemblyVersion))
            _updateRules.Add(new CSharpVersionUpdateRule("AssemblyVersion", newAssemblyVersion));

        if (!string.IsNullOrEmpty(newAssemblyFileVersion))
            _updateRules.Add(new CSharpVersionUpdateRule("AssemblyFileVersion", newAssemblyFileVersion));

        if (!string.IsNullOrEmpty(copyright))
            _updateRules.Add(new CSharpStringUpdateRule("AssemblyCopyright", copyright));

        if (!string.IsNullOrEmpty(company))
            _updateRules.Add(new CSharpStringUpdateRule("AssemblyCompany", company));
    }

    public static bool Execute(string fileName,
                               string assemblyVersion,
                               string assemblyFileVersion,
                               string copyright,
                               string company)
    {
        try
        {
            if (fileName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                var updater = new CSharpUpdater(assemblyVersion, assemblyFileVersion, copyright, company);
                updater.UpdateFile(fileName);
            }
        }
        catch (Exception e)
        {
            Error(e.Message);

            return false;
        }

        return true;
    }

    public static string? GetAssemblyVersion(string fileName) => GetAssemblyProperty(fileName, "AssemblyVersion", true);

    public static string? GetAssemblyProperty(string fileName, string propertyName, bool isVersionString = false)
    {
        try
        {
            if (fileName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                foreach (string line in File.ReadAllLines(fileName))
                {
                    Group? g = isVersionString
                        ? GetVersionString(line, propertyName)
                        : GetValueString(line, propertyName);

                    if (g is not null)
                        return g.Value;
                }
        }
        catch (Exception e)
        {
            Error(e.Message);
        }

        return null;
    }

    public static bool UpdateLineWithRule(ref string line, ICSharpUpdateRule updateRule)
    {
        var updated = false;

        switch (updateRule)
        {
            case CSharpVersionUpdateRule rule:
            {
                VersionString? v = null;
                Group? g = GetVersionString(line, rule.AttributeName);

                if (g is not null)
                    VersionString.TryParse(g.Value, out v);

                if (v is not null)
                {
                    string newVersion = rule.Update(v);
                    line = line[..g!.Index] + newVersion + line[(g.Index + g.Length)..];
                    updated = true;
                }

                break;
            }
            case CSharpStringUpdateRule stringRule:
            {
                if (line.Contains(stringRule.AttributeName))
                {
                    Group? g = GetValueString(line, stringRule.AttributeName);
                    string newVersion = stringRule.Update(null);
                    line = line[..g!.Index] + newVersion + line[(g.Index + g.Length)..];
                    updated = true;
                }

                break;
            }
        }

        return updated;
    }

    public static Group? GetVersionString(string input, string attributeName)
    {
        int commentIndex = input.IndexOf("//", StringComparison.Ordinal);

        if (commentIndex != -1)
            input = input[..commentIndex];

        var attributeMatch = $"(?:(?:{attributeName})|(?:{attributeName}Attribute))";

        var regex = new Regex($@"^\s*\[assembly: {attributeMatch}\(""(?<Version>[0-9\.\*]+)""\)\]");
        Match m = regex.Match(input);

        return m.Success
            ? m.Groups["Version"]
            : null;
    }

    public static Group? GetValueString(string input, string attributeName)
    {
        int commentIndex = input.IndexOf("//", StringComparison.Ordinal);

        if (commentIndex != -1)
            input = input[..commentIndex];

        var attributeMatch = $"(?:(?:{attributeName})|(?:{attributeName}Attribute))";

        var regex = new Regex($@"^\s*\[assembly: {attributeMatch}\(""(?<Value>[^\""]*)""\)\]");
        Match m = regex.Match(input);

        return m.Success
            ? m.Groups["Value"]
            : null;
    }

    public void UpdateFile(string fileName)
    {
        string[] lines = File.ReadAllLines(fileName);

        File.WriteAllLines(fileName, lines.Select(UpdateLine).ToArray());
    }

    private string UpdateLine(string line)
    {
        foreach (ICSharpUpdateRule rule in _updateRules)
        {
            if (UpdateLineWithRule(ref line, rule))
                break;
        }

        return line;
    }
}
