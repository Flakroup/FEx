using FEx.WPFx.Implementations;
using Shouldly;
using System.Linq;
using System.Reflection;
using Xunit;

namespace FEx.WPFx.Tests;

public class DispatcherContextExecutorTests
{
    /// <summary>
    /// The base <c>FExDispatcher</c> declares every <c>sender</c> as optional, and a default is not inherited by an override, so
    /// a call through <see cref="DispatcherContextExecutor"/> without a sender compiles only while each override repeats <c>= null</c>.
    /// </summary>
    [Fact]
    public void Every_override_with_a_sender_defaults_it_to_null()
    {
        var senders = typeof(DispatcherContextExecutor)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(method => (method, parameter: method.GetParameters().SingleOrDefault(parameter => parameter.Name == "sender")))
            .Where(pair => pair.parameter is not null)
            .ToList();

        senders.ShouldNotBeEmpty();
        foreach (var (method, parameter) in senders)
        {
            parameter!.HasDefaultValue.ShouldBeTrue(method.ToString());
            parameter.DefaultValue.ShouldBeNull(method.ToString());
        }
    }
}
