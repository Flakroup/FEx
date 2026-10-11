using FEx.Agnostics.Abstractions.Interfaces;
using FEx.Agnostics.Abstractions.Logging;
using FEx.Json.Extensions;
using Newtonsoft.Json;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FEx.Json.Tests;

/// <summary>
/// FEx.SecureStorage reads decrypted secrets through <c>FromJson</c>, and Newtonsoft quotes the offending value
/// in its exception message, so a failed deserialization must log neither the value nor the input (#295).
/// </summary>
[Collection(StaticStateCollection.Name)]
public sealed class FromJsonLoggingTests : IDisposable
{
    private const string LeakMarker = "4711-secret";
    private const string LeakMarkerJson = $"{{\"Pin\":\"{LeakMarker}\"}}";

    private readonly IFExLogger _previousLogger = FExStaticLogger.Instance;
    private readonly IFExLogger _logger = Substitute.For<IFExLogger>();

    public FromJsonLoggingTests()
    {
        FExStaticLogger.Configure(() => _logger);
    }

    public void Dispose() => FExStaticLogger.Configure(() => _previousLogger);

    [Fact]
    public void FromJson_ValueOfTheWrongType_LogsTypePathAndExceptionTypeButNotTheSecret()
    {
        Should.Throw<JsonReaderException>(() => LeakMarkerJson.FromJson<Dictionary<string, int>>());

        AssertNoLoggedSecret();
        LoggedText().ShouldContain(typeof(Dictionary<string, int>).FullName!);
        LoggedText().ShouldContain("'Pin'");
        LoggedText().ShouldContain(nameof(JsonReaderException));
    }

    [Fact]
    public void FromJson_ValueThatIsNotAnObject_LogsTheSerializationFailureWithoutTheSecret()
    {
        Should.Throw<JsonSerializationException>(() => $"\"{LeakMarker}\"".FromJson<Dictionary<string, int>>());

        AssertNoLoggedSecret();
        LoggedText().ShouldContain(typeof(Dictionary<string, int>).FullName!);
        LoggedText().ShouldContain(nameof(JsonSerializationException));
    }

    [Fact]
    public void FromJson_ConverterQuotingTheValue_LogsNeitherTheMessageNorTheInput()
    {
        var settings = new JsonSerializerSettings { Converters = [new QuotingConverter()] };

        Should.Throw<InvalidOperationException>(() => LeakMarkerJson.FromJson<Dictionary<string, int>>(settings));

        AssertNoLoggedSecret();
        LoggedText().ShouldContain(typeof(Dictionary<string, int>).FullName!);
        LoggedText().ShouldContain(nameof(InvalidOperationException));
    }

    // Every argument of every call the logger received: messages, scope and label values, and the logged
    // exception rendered the way a sink renders it (ToString includes the message and inner exceptions).
    private IEnumerable<string> LoggedArguments() =>
        _logger.ReceivedCalls()
               .SelectMany(call => call.GetArguments())
               .Select(argument => argument?.ToString() ?? string.Empty);

    private string LoggedText() => string.Join(Environment.NewLine, LoggedArguments());

    private void AssertNoLoggedSecret()
    {
        _logger.ReceivedCalls().ShouldNotBeEmpty();
        LoggedArguments().ShouldAllBe(text => !text.Contains(LeakMarker));
    }

    private sealed class QuotingConverter : JsonConverter<int>
    {
        public override int ReadJson(JsonReader reader,
                                     Type objectType,
                                     int existingValue,
                                     bool hasExistingValue,
                                     JsonSerializer serializer) =>
            throw new InvalidOperationException($"Cannot read '{reader.Value}'.");

        public override void WriteJson(JsonWriter writer, int value, JsonSerializer serializer) =>
            throw new NotSupportedException();
    }
}
