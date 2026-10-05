using FEx.Common.Abstractions.Interfaces;
using FEx.MVVM.Rx.Utilities;
using Shouldly;
using Xunit;

namespace FEx.MVVM.Tests;

public class StatusServiceTests
{
    // The WPF bootstrapper wraps its startup steps in Log(...) before any hub is registered.
    [Fact]
    public void Log_without_a_hub_returns_a_no_op_scope()
    {
        IStatusService service = new StatusService();

        var scope = Should.NotThrow(() => service.Log("Checking duplicated instances"));

        Should.NotThrow(scope.Dispose);
    }

    [Fact]
    public void Log_uses_the_main_hub_once_one_is_registered()
    {
        var service = new StatusService();
        var hub = service.GetOrAdd(null, null, null, null, true);

        using (service.Log("Starting"))
            hub.GetStatuses().ShouldContain("Starting");

        hub.GetStatuses().ShouldNotContain("Starting");
    }
}
