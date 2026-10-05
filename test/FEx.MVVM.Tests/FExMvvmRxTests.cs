using FEx.Common.Abstractions.Interfaces;
using FEx.MVVM.Rx;
using FEx.MVVM.Rx.Utilities;
using Shouldly;
using Xunit;

namespace FEx.MVVM.Tests;

public class FExMvvmRxTests
{
    [Fact]
    public void Initialize_sets_the_status_service_and_is_idempotent()
    {
        IStatusService service = new StatusService();
        var module = new FExMvvmRx(service);

        module.Initialize();
        module.Initialize();

        module.IsInitialized.ShouldBeTrue();
        FExMvvmRx.StatusService.ShouldBeSameAs(service);
    }
}
