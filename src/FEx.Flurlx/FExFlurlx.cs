using FEx.Agnostics.Abstractions;
using FEx.Flurlx.Abstractions.Interfaces;

namespace FEx.Flurlx;

public class FExFlurlx : FExInitialize
{
    public FExFlurlx(IFlurlConfigurator configurator)
        : base(configurator)
    {
        // Initialize with configurator dependency
    }

    protected override void OnInitialize()
    {
        // Business logic initialization if needed
    }
}