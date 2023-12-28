using FEx.Abstractions;
using FEx.Extensions;

namespace FEx.Fundamentals;

public class FExFundamentalsModuleInitializer : InitializeModule
{
    private readonly Foundation _foundation;

    public FExFundamentalsModuleInitializer(Foundation foundation)
    {
        _foundation = foundation;
    }

    protected override void OnInitialize()
    {
        _foundation.Guard();
    }
}