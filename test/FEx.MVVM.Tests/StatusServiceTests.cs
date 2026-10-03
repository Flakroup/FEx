using FEx.Common.Abstractions.Interfaces;
using Shouldly;
using Xunit;
using LegacyStatusService = FEx.MVVM.Rx.Legacy.Utilities.StatusService;
using RxStatusService = FEx.MVVM.Rx.Utilities.StatusService;

namespace FEx.MVVM.Tests;

public class StatusServiceTests
{
    // The WPF bootstrapper wraps its startup steps in Log(...) before any hub is registered.
    [Fact]
    public void Legacy_log_without_a_hub_returns_a_no_op_scope() => LogWithoutHub(new LegacyStatusService());

    [Fact]
    public void Rx_log_without_a_hub_returns_a_no_op_scope() => LogWithoutHub(new RxStatusService());

    [Fact]
    public void Legacy_log_uses_the_main_hub_once_one_is_registered()
    {
        var service = new LegacyStatusService();
        var hub = service.GetOrAdd(null, null, null, null, true);

        using (service.Log("Starting"))
            hub.GetStatuses().ShouldContain("Starting");

        hub.GetStatuses().ShouldNotContain("Starting");
    }

    private static void LogWithoutHub(IStatusService service)
    {
        var scope = Should.NotThrow(() => service.Log("Checking duplicated instances"));

        Should.NotThrow(scope.Dispose);
    }
}
