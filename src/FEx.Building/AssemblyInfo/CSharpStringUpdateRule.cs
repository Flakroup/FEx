namespace FEx.Building.AssemblyInfo;

public class CSharpStringUpdateRule : ICSharpUpdateRule
{
    private readonly StringUpdateRule _updateRule;

    public string AttributeName { get; }

    public CSharpStringUpdateRule(string attributeName, string updateRule)
    {
        AttributeName = attributeName;
        _updateRule = new(updateRule);
    }

    public string Update(string v) => _updateRule.Update(v);
}
