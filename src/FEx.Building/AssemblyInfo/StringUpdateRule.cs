namespace FEx.Building.AssemblyInfo;

public class StringUpdateRule
{
    private readonly string _rule;

    public StringUpdateRule(string rule)
    {
        _rule = rule;
    }

    public string Update(string? version = null) => _rule;
}
