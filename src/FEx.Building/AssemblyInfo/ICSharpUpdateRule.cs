namespace FEx.Building.AssemblyInfo;

public interface ICSharpUpdateRule
{
    string AttributeName { get; }

    string Update(string? v);
}