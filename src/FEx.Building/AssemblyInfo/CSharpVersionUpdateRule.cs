namespace FEx.Building.AssemblyInfo;

public class CSharpVersionUpdateRule : ICSharpUpdateRule
{
    private readonly VersionUpdateRule _updateRule;

    public string AttributeName { get; }

    public CSharpVersionUpdateRule(string attributeName, string updateRule)
    {
        AttributeName = attributeName;
        _updateRule = new(updateRule);
    }

    public string Update(string? v) => _updateRule.Update(v);

    public string Update(VersionString v) => _updateRule.Update(v);
}