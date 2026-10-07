using FEx.DependencyInjection.Abstractions;
using FEx.Json.Abstractions;
using FEx.Json.Abstractions.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Json.Tests.Contract;

/// <summary>
/// The <see cref="IFExJsonSerializer" /> contract. Every implementation runs the same suite, which is what makes them
/// interchangeable.
/// </summary>
public abstract class JsonSerializerContractTests
{
    private const string Malformed = "{\"Name\":";

    private static readonly Person Sample = new()
    {
        Name = "Ada",
        Age = 36,
        Tags = ["math", "engines"],
        Address = new() { City = "London" }
    };

    protected abstract IFExJsonSerializer CreateSerializer(DIMeta diMeta);

    private IFExJsonSerializer CreateSerializer() => CreateSerializer(new());

    private async Task<IFExJsonSerializer> CreateSerializerWithRegistrationsAsync(Action<IServiceCollection> register)
    {
        var services = new ServiceCollection();
        register(services);
        var meta = new DIMeta();
        await meta.OnCompleteInitializationAsync(services);

        return CreateSerializer(meta);
    }

    private static void ShouldMatchSample(Person? person)
    {
        person.ShouldNotBeNull();
        person.Name.ShouldBe(Sample.Name);
        person.Age.ShouldBe(Sample.Age);
        person.Tags.ShouldBe(Sample.Tags);
        person.Address.ShouldNotBeNull().City.ShouldBe(Sample.Address!.City);
    }

    #region Round trip
    [Fact]
    public void String_Generic_RoundTrips()
    {
        var serializer = CreateSerializer();

        ShouldMatchSample(serializer.Deserialize<Person>(serializer.Serialize(Sample)));
    }

    [Fact]
    public void String_TypeBased_RoundTrips()
    {
        var serializer = CreateSerializer();

        var json = serializer.Serialize(Sample, typeof(Person));

        ShouldMatchSample(serializer.Deserialize(json, typeof(Person)).ShouldBeOfType<Person>());
    }

    [Fact]
    public async Task Stream_Generic_RoundTripsAsUtf8WithoutBomAndLeavesStreamOpen()
    {
        var serializer = CreateSerializer();
        using var stream = new MemoryStream();

        await serializer.SerializeAsync(stream, Sample, TestContext.Current.CancellationToken);

        stream.CanRead.ShouldBeTrue();
        var bytes = stream.ToArray();
        bytes[0].ShouldBe((byte)'{');
        Encoding.UTF8.GetString(bytes).ShouldBe(serializer.Serialize(Sample));
        stream.Position = 0;
        ShouldMatchSample(await serializer.DeserializeAsync<Person>(stream, TestContext.Current.CancellationToken));
        stream.CanRead.ShouldBeTrue();
    }

    [Fact]
    public async Task Stream_TypeBased_RoundTrips()
    {
        var serializer = CreateSerializer();
        using var stream = new MemoryStream();

        await serializer.SerializeAsync(stream, Sample, typeof(Person), TestContext.Current.CancellationToken);
        stream.Position = 0;

        ShouldMatchSample((await serializer.DeserializeAsync(stream, typeof(Person),
            TestContext.Current.CancellationToken)).ShouldBeOfType<Person>());
    }

    [Fact]
    public async Task Stream_WithUtf8ByteOrderMark_IsRead()
    {
        var serializer = CreateSerializer();
        using var stream = new MemoryStream([0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes(serializer.Serialize(Sample))]);

        ShouldMatchSample(await serializer.DeserializeAsync<Person>(stream, TestContext.Current.CancellationToken));
    }
    #endregion

    #region Null and empty input
    [Fact]
    public void NullValue_SerializesToNullLiteral_AndReadsBackAsNull()
    {
        var serializer = CreateSerializer();

        serializer.Serialize<Person?>(null).ShouldBe("null");
        serializer.Serialize(null, typeof(Person)).ShouldBe("null");
        serializer.Deserialize<Person>("null").ShouldBeNull();
        serializer.Deserialize("null", typeof(Person)).ShouldBeNull();
    }

    [Fact]
    public void NullLiteral_ForNonNullableValueType_ThrowsFExJsonException()
    {
        var serializer = CreateSerializer();

        Should.Throw<FExJsonException>(() => serializer.Deserialize<int>("null")).InnerException.ShouldNotBeNull();
    }

    [Fact]
    public async Task NullArguments_ThrowArgumentNullException()
    {
        var serializer = CreateSerializer();
        using var stream = new MemoryStream();

        Should.Throw<ArgumentNullException>(() => serializer.Deserialize<Person>(null!));
        Should.Throw<ArgumentNullException>(() => serializer.Deserialize(null!, typeof(Person)));
        Should.Throw<ArgumentNullException>(() => serializer.Deserialize("{}", null!));
        Should.Throw<ArgumentNullException>(() => serializer.Serialize(Sample, null!));
        await Should.ThrowAsync<ArgumentNullException>(() => serializer.SerializeAsync(null!, Sample));
        await Should.ThrowAsync<ArgumentNullException>(() => serializer.SerializeAsync(stream, Sample, null!));
        await Should.ThrowAsync<ArgumentNullException>(() => serializer.DeserializeAsync<Person>(null!));
        await Should.ThrowAsync<ArgumentNullException>(() => serializer.DeserializeAsync(stream, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyPayload_ThrowsFExJsonException(string json)
    {
        var serializer = CreateSerializer();

        Should.Throw<FExJsonException>(() => serializer.Deserialize<Person>(json));
        Should.Throw<FExJsonException>(() => serializer.Deserialize(json, typeof(Person)));
    }

    [Fact]
    public async Task EmptyStream_ThrowsFExJsonException()
    {
        var serializer = CreateSerializer();
        using var stream = new MemoryStream();

        await Should.ThrowAsync<FExJsonException>(() => serializer.DeserializeAsync<Person>(stream));
    }

    [Fact]
    public void ValueOfAnotherType_ThrowsArgumentException()
    {
        var serializer = CreateSerializer();

        Should.Throw<ArgumentException>(() => serializer.Serialize("text", typeof(Person)));
    }

    [Fact]
    public async Task ValueOfAnotherType_ThrowsArgumentException_FromTheStreamOverload()
    {
        var serializer = CreateSerializer();
        using var stream = new MemoryStream();

        await Should.ThrowAsync<ArgumentException>(() => serializer.SerializeAsync(stream, "text", typeof(Person)));
    }

    [Fact]
    public async Task NullValueForNonNullableValueType_ThrowsArgumentException()
    {
        var serializer = CreateSerializer();
        using var stream = new MemoryStream();

        Should.Throw<ArgumentException>(() => serializer.Serialize(null, typeof(int)));
        await Should.ThrowAsync<ArgumentException>(() => serializer.SerializeAsync(stream, null, typeof(int)));
        serializer.Serialize<int?>(null).ShouldBe("null");
    }

    [Fact]
    public async Task StreamThatCannotBeReadOrWritten_ThrowsArgumentException()
    {
        var serializer = CreateSerializer();
        using var readOnly = new MemoryStream(new byte[16], false);
        using var unreadable = new WriteOnlyStream();

        await Should.ThrowAsync<ArgumentException>(() => serializer.SerializeAsync(readOnly, Sample));
        await Should.ThrowAsync<ArgumentException>(() => serializer.SerializeAsync(readOnly, Sample, typeof(Person)));
        await Should.ThrowAsync<ArgumentException>(() => serializer.DeserializeAsync<Person>(unreadable));
        await Should.ThrowAsync<ArgumentException>(() => serializer.DeserializeAsync(unreadable, typeof(Person)));
    }

    [Fact]
    public async Task Cancellation_ThrowsOperationCanceledException_Unwrapped()
    {
        var serializer = CreateSerializer();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        using var target = new MemoryStream();
        using var source = new MemoryStream(Encoding.UTF8.GetBytes(serializer.Serialize(Sample)));

        await Should.ThrowAsync<OperationCanceledException>(() =>
            serializer.SerializeAsync(target, Sample, cancellation.Token));
        await Should.ThrowAsync<OperationCanceledException>(() =>
            serializer.SerializeAsync(target, Sample, typeof(Person), cancellation.Token));
        await Should.ThrowAsync<OperationCanceledException>(() =>
            serializer.DeserializeAsync<Person>(source, cancellation.Token));
        await Should.ThrowAsync<OperationCanceledException>(() =>
            serializer.DeserializeAsync(source, typeof(Person), cancellation.Token));
    }
    #endregion

    #region Malformed payload
    [Theory]
    [InlineData(Malformed)]
    [InlineData("{\"Name\":\"a\"} {\"Name\":\"b\"}")]
    [InlineData("{\"Age\":\"not a number\"}")]
    public void MalformedOrUnbindablePayload_ThrowsFExJsonExceptionWrappingTheLibraryException(string json)
    {
        var serializer = CreateSerializer();

        Should.Throw<FExJsonException>(() => serializer.Deserialize<Person>(json)).InnerException.ShouldNotBeNull();
        Should.Throw<FExJsonException>(() => serializer.Deserialize(json, typeof(Person))).InnerException
            .ShouldNotBeNull();
    }

    [Fact]
    public async Task MalformedStream_ThrowsFExJsonException()
    {
        var serializer = CreateSerializer();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(Malformed));

        (await Should.ThrowAsync<FExJsonException>(() => serializer.DeserializeAsync(stream, typeof(Person))))
            .InnerException.ShouldNotBeNull();
    }

    [Fact]
    public void UnregisteredInterface_ThrowsFExJsonException()
    {
        var serializer = CreateSerializer();

        Should.Throw<FExJsonException>(() => serializer.Deserialize<IDIModel>("{\"Name\":\"x\"}"));
    }

    [Fact]
    public async Task StreamThatIsNotUtf8_ThrowsFExJsonException()
    {
        var serializer = CreateSerializer();
        // "zażółć" encoded in Windows-1250: not valid UTF-8.
        using var stream = new MemoryStream([
            .. Encoding.ASCII.GetBytes("{\"Name\":\"za"), 0xBF, 0xF3, 0xB3, 0xE6, .. Encoding.ASCII.GetBytes("\"}")
        ]);

        await Should.ThrowAsync<FExJsonException>(() => serializer.DeserializeAsync<Person>(stream));
    }

    [Fact]
    public void InvalidTypeContract_ThrowsFExJsonException()
    {
        var serializer = CreateSerializer();

        Should.Throw<FExJsonException>(() => serializer.Serialize(new CollidingModel()));
        Should.Throw<FExJsonException>(() => serializer.Deserialize<CollidingModel>("{\"x\":1}"));
    }
    #endregion

    #region DI-aware construction
    [Fact]
    public async Task InterfaceRegisteredWithImplementation_IsCreatedAsTheImplementation()
    {
        var serializer = await CreateSerializerWithRegistrationsAsync(static services => services.AddSingleton<IDIModel, DIModel>());

        var model = serializer.Deserialize<IDIModel>("{\"Name\":\"x\"}").ShouldBeOfType<DIModel>();

        model.Name.ShouldBe("x");
        model.Origin.ShouldBe("constructor"); // a singleton is never handed out for deserialization
    }

    [Theory]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    public async Task SharedRegistration_IsNeverHandedOut(ServiceLifetime lifetime)
    {
        var serializer = await CreateSerializerWithRegistrationsAsync(services =>
            services.Add(new(typeof(IDIModel), typeof(DIModel), lifetime)));
        FExServiceProvider.Release();

        try
        {
            // Even with a provider that would hand out a container instance, a shared registration is built anew.
            await FExServiceProvider.InitializeAsync<ServiceProviderTestContainer>();

            serializer.Deserialize<IDIModel>("{\"Name\":\"x\"}").ShouldBeOfType<DIModel>().Origin
                .ShouldBe("constructor");
        }
        finally
        {
            FExServiceProvider.Release();
        }
    }

    [Fact]
    public async Task ConcreteRegistration_IsDeserializedAsItself()
    {
        var serializer = await CreateSerializerWithRegistrationsAsync(static services => services.AddSingleton<DIModel>());

        serializer.Deserialize<DIModel>("{\"Name\":\"x\"}").ShouldNotBeNull().Name.ShouldBe("x");
    }

    [Fact]
    public async Task TransientRegistration_IsResolvedFromTheServiceProvider()
    {
        var serializer = await CreateSerializerWithRegistrationsAsync(static services => services.AddTransient<IDIModel, DIModel>());
        FExServiceProvider.Release();

        try
        {
            await FExServiceProvider.InitializeAsync<ServiceProviderTestContainer>();

            var model = serializer.Deserialize<IDIModel>("{\"Name\":\"x\"}").ShouldBeOfType<DIModel>();

            model.Name.ShouldBe("x");
            model.Origin.ShouldBe(ServiceProviderTestContainer.Origin);
        }
        finally
        {
            FExServiceProvider.Release();
        }
    }
    #endregion

    #region Converters
    [Fact]
    public void ParseStringToLong_ReadsStringAndNumber_AndWritesString()
    {
        var serializer = CreateSerializer();

        serializer.Deserialize<LongHolder>("{\"Amount\":\"42\"}").ShouldNotBeNull().Amount.ShouldBe(42);
        serializer.Deserialize<LongHolder>("{\"Amount\":43}").ShouldNotBeNull().Amount.ShouldBe(43);
        serializer.Serialize(new LongHolder { Amount = 44 }).ShouldBe("{\"Amount\":\"44\"}");
    }

    [Fact]
    public void ParseStringToLong_UnparsableString_ThrowsFExJsonException()
    {
        var serializer = CreateSerializer();

        Should.Throw<FExJsonException>(() => serializer.Deserialize<LongHolder>("{\"Amount\":\"x\"}"));
    }

    [Fact]
    public void ParseStringToDouble_ReadsStringAndNumber_AndWritesString()
    {
        var serializer = CreateSerializer();
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        try
        {
            serializer.Deserialize<DoubleHolder>("{\"Value\":\"1.5\"}").ShouldNotBeNull().Value.ShouldBe(1.5);
            serializer.Deserialize<DoubleHolder>("{\"Value\":2.5}").ShouldNotBeNull().Value.ShouldBe(2.5);
            serializer.Serialize(new DoubleHolder { Value = 3.5 }).ShouldBe("{\"Value\":\"3.5\"}");
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public void ParseStringToDouble_UnparsableString_ThrowsFExJsonException()
    {
        var serializer = CreateSerializer();

        Should.Throw<FExJsonException>(() => serializer.Deserialize<DoubleHolder>("{\"Value\":\"abc\"}"));
    }

    [Fact]
    public void ParseStringToDouble_UnparsableString_FailsWithFormatException()
    {
        var serializer = CreateSerializer();

        var exception = Should.Throw<FExJsonException>(() => serializer.Deserialize<DoubleHolder>("{\"Value\":\"abc\"}"));

        exception.GetBaseException().ShouldBeOfType<FormatException>();
    }

    [Fact]
    public void ParseStringToDouble_UnderCommaCulture_WritesWithInvariantCulture()
    {
        var serializer = CreateSerializer();
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new("pl-PL");

        try
        {
            CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator.ShouldBe(",");
            serializer.Serialize(new DoubleHolder { Value = 1.5, Optional = 2.25 }).ShouldBe("{\"Value\":\"1.5\",\"Optional\":\"2.25\"}");
            serializer.Deserialize<DoubleHolder>("{\"Value\":\"1.5\"}").ShouldNotBeNull().Value.ShouldBe(1.5);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public void NullPayloadForNonNullableDouble_ThrowsFExJsonException()
    {
        var serializer = CreateSerializer();

        // A null member is not covered: the FEx Newtonsoft defaults (NullValueHandling.Ignore) skip it on read.
        Should.Throw<FExJsonException>(() => serializer.Deserialize<double>("null"));
        Should.Throw<FExJsonException>(() => serializer.Deserialize("null", typeof(double)));
    }

    [Fact]
    public void NullForNullableDouble_ReadsAsNull()
    {
        var serializer = CreateSerializer();
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        try
        {
            serializer.Deserialize<DoubleHolder>("{\"Optional\":null}").ShouldNotBeNull().Optional.ShouldBeNull();
            serializer.Deserialize<DoubleHolder>("{\"Optional\":\"1.5\"}").ShouldNotBeNull().Optional.ShouldBe(1.5);
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public void JsonPath_ReadsEachPropertyFromItsPath()
    {
        var serializer = CreateSerializer();
        const string json = "{\"data\":{\"items\":[{\"name\":\"a\"},{\"name\":\"b\"}],\"count\":3},\"Flat\":\"f\"}";

        var model = serializer.Deserialize<PathModel>(json).ShouldNotBeNull();

        model.Name.ShouldBe("b");
        model.Count.ShouldBe(3);
        model.Flat.ShouldBe("f");
    }

    [Fact]
    public void JsonPath_DollarPrefixedName_IsAMemberNotTheRoot()
    {
        var serializer = CreateSerializer();

        var model = serializer.Deserialize<PathModel>("{\"$schema\":\"dollar\",\"schema\":\"plain\"}")
            .ShouldNotBeNull();

        model.Schema.ShouldBe("dollar");
    }

    [Fact]
    public void JsonPath_NonObjectPayload_ThrowsFExJsonException()
    {
        var serializer = CreateSerializer();

        Should.Throw<FExJsonException>(() => serializer.Deserialize<PathModel>("[1,2]"));
    }

    [Fact]
    public void JsonPath_MissingPath_LeavesTheDefault()
    {
        var serializer = CreateSerializer();

        var model = serializer.Deserialize<PathModel>("{\"data\":{\"items\":[]}}").ShouldNotBeNull();

        model.Name.ShouldBeNull();
        model.Count.ShouldBe(0);
    }

    [Fact]
    public void JsonPath_WritesAFlatObjectKeyedByThePaths()
    {
        var serializer = CreateSerializer();

        var json = serializer.Serialize(new PathModel { Name = "n", Count = 2 });

        json.ShouldBe("{\"data.items[1].name\":\"n\",\"data.count\":2}");
    }
    #endregion

    private sealed class WriteOnlyStream : MemoryStream
    {
        public override bool CanRead => false;
    }
}
