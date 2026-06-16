using System;
using System.Collections.Generic;

namespace FEx.Building.AssemblyInfo;

public class VersionUpdateRule
{
    private readonly string[] _partRules;

    public VersionUpdateRule(string rule)
    {
        _partRules = rule.Split('.');

        if (_partRules.Length is < 2 or > 4)
            throw new ArgumentException("Expecting 2-4 version parts");

        foreach (var partRule in _partRules)
        {
            if (partRule is "+" or "=")
            {
            }
            else
            {
                var _ = int.Parse(partRule);
            }
        }
    }

    public string Update(string? version) => Update(new VersionString(version ?? "0.0.0"));

    public string Update(VersionString version)
    {
        var inParts = new List<string>
        {
            version.Major,
            version.Minor,
            version.Build,
            version.Revision
        };

        var outParts = new List<string>();

        for (var index = 0; index < _partRules.Length; index++)
        {
            var rule = _partRules[index];
            var inPart = inParts[index];

            switch (rule)
            {
                case "=":
                {
                    if (inPart.Length > 0)
                        outParts.Add(inParts[index]);

                    break;
                }
                case "+" when inPart.Length == 0:
                    throw new ArgumentException("Can't increment missing value");
                case "+":
                    int.TryParse(inPart, out var inNumber);
                    inNumber++;
                    outParts.Add(inNumber.ToString());

                    break;
                default:
                    outParts.Add(_partRules[index]);

                    break;
            }
        }

        return string.Join(".", [.. outParts]);
    }
}