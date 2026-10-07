using FEx.Sqlx.Abstractions;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Sqlx.Tests;

/// <summary>Consumers read instance data and enumerate instances through Sqlx.Abstractions only.</summary>
public sealed class SqlxAbstractionTests
{
    [Fact]
    public void ConcreteTypes_SatisfyTheAbstractions()
    {
        typeof(ISqlDbHelper).IsAssignableFrom(typeof(SqlDbHelper)).ShouldBeTrue();
        typeof(ISqlInstanceInfo).IsAssignableFrom(typeof(SQLInstanceInfo)).ShouldBeTrue();
        typeof(ISqlInstanceProvider).IsAssignableFrom(typeof(SqlInstanceProvider)).ShouldBeTrue();
    }

    [Fact]
    public async Task SqlDbHelper_ExposesTheChosenInstanceThroughTheInterface()
    {
        TestInstanceInfo old = new("host\\OLD", SqlLoginMode.Mixed, new(15, 0));
        TestInstanceInfo latest = new("host\\NEW", SqlLoginMode.Integrated, new(16, 0));
        TestInstanceInfo unusable = new("host\\DOWN", SqlLoginMode.Unknown, new(17, 0));
        using SqlDbHelper helper = new(() => Task.FromResult<IList<SQLInstanceInfo>>([old, unusable, latest]));
        await helper.InitializeAsync();

        ISqlDbHelper abstraction = helper;

        abstraction.SQLInstanceInfo.ShouldBeSameAs(latest);
        abstraction.SQLInstance.ShouldBe("host\\NEW");
        abstraction.SQLInstanceInfo!.ProductVersion.ShouldBe(new Version(16, 0));
        abstraction.SQLInstanceInfo.LoginMode.ShouldBe(SqlLoginMode.Integrated);
    }

    [Fact]
    public async Task SqlDbHelper_WithOnlyUnusableInstances_ExposesNone()
    {
        using SqlDbHelper helper = new(() => Task.FromResult<IList<SQLInstanceInfo>>([new("host\\DOWN", SqlLoginMode.Unknown)]));
        await helper.InitializeAsync();

        ((ISqlDbHelper)helper).SQLInstanceInfo.ShouldBeNull();
    }

    [Fact]
    public void Provider_DefaultConstructor_IsReadyWithoutTouchingTheMachine() =>
        new SqlInstanceProvider().ShouldBeAssignableTo<ISqlInstanceProvider>();

    [Fact]
    public async Task Provider_MapsDiscoveredInstancesToTheAbstraction()
    {
        TestInstanceInfo first = new("host\\A", SqlLoginMode.Mixed, new(16, 0));
        TestInstanceInfo second = new("host\\B", SqlLoginMode.Normal, new(14, 0));
        ISqlInstanceProvider provider = new SqlInstanceProvider(() => Task.FromResult<IList<SQLInstanceInfo>>([first, second]));

        var instances = await provider.GetSqlInstancesAsync();

        instances.Select(x => x.SQLInstance).ShouldBe(["host\\A", "host\\B"]);
        instances[0].ShouldBeSameAs(first);
        instances[1].LoginMode.ShouldBe(SqlLoginMode.Normal);
        instances[1].ProductVersion.ShouldBe(new Version(14, 0));
    }

    [Fact]
    public async Task Provider_WithNoInstances_ReturnsEmpty()
    {
        ISqlInstanceProvider provider = new SqlInstanceProvider(() => Task.FromResult<IList<SQLInstanceInfo>>([]));

        (await provider.GetSqlInstancesAsync()).ShouldBeEmpty();
    }

    [Fact]
    public void InstanceInfo_WithoutServer_ReportsUnknownLoginMode() =>
        new SQLInstanceInfo("host").LoginMode.ShouldBe(SqlLoginMode.Unknown);

    [Fact]
    public void ReadLoginMode_MapsDefinedSmoValues()
    {
        SQLInstanceInfo.ReadLoginMode(() => ServerLoginMode.Integrated).ShouldBe(SqlLoginMode.Integrated);
        SQLInstanceInfo.ReadLoginMode(() => ServerLoginMode.Mixed).ShouldBe(SqlLoginMode.Mixed);
        SQLInstanceInfo.ReadLoginMode(() => ServerLoginMode.Normal).ShouldBe(SqlLoginMode.Normal);
    }

    [Fact]
    public void ReadLoginMode_OnUnreachableInstance_ReturnsUnknownInsteadOfThrowing() =>
        SQLInstanceInfo.ReadLoginMode(() => throw new ConnectionFailureException("unreachable")).ShouldBe(SqlLoginMode.Unknown);

    [Fact]
    public void ReadLoginMode_DoesNotSwallowOtherFailures() =>
        Should.Throw<InvalidOperationException>(() => SQLInstanceInfo.ReadLoginMode(() => throw new InvalidOperationException()));

    [Fact]
    public void MapByValue_DefinedValue_MapsToTheSameMember()
    {
        2.MapByValue(SqlFileStreamLevel.Unknown).ShouldBe(SqlFileStreamLevel.SqlLocalFileSystemAccess);
        1.MapByValue(SqlHadrManagerStatus.Unknown).ShouldBe(SqlHadrManagerStatus.Running);
        FileStreamEffectiveLevel.TSqlAccess.MapByValue(SqlFileStreamLevel.Unknown).ShouldBe(SqlFileStreamLevel.SqlAccess);
    }

    [Fact]
    public void MapByValue_UndefinedValue_ReturnsTheFallback()
    {
        4.MapByValue(SqlFileStreamLevel.Unknown).ShouldBe(SqlFileStreamLevel.Unknown);
        3.MapByValue(SqlHadrManagerStatus.Unknown).ShouldBe(SqlHadrManagerStatus.Unknown);
        ServerLoginMode.Unknown.MapByValue(SqlLoginMode.Normal).ShouldBe(SqlLoginMode.Unknown);
        7.MapByValue(SqlLoginMode.Unknown).ShouldBe(SqlLoginMode.Unknown);
    }

    [Fact]
    public void AbstractionEnums_MirrorEverySmoMember()
    {
        foreach (var value in Enum.GetValues<ServerLoginMode>())
            ((int)value).ShouldBe((int)Enum.Parse<SqlLoginMode>(value.ToString()));

        foreach (var value in Enum.GetValues<FileStreamEffectiveLevel>())
            ((int)value).ShouldBe((int)Enum.Parse<SqlFileStreamLevel>(value.ToString().Replace("TSql", "Sql")));

        foreach (var value in Enum.GetValues<HadrManagerStatus>())
            ((int)value).ShouldBe((int)Enum.Parse<SqlHadrManagerStatus>(value.ToString()));
    }

    private sealed class TestInstanceInfo : SQLInstanceInfo
    {
        public TestInstanceInfo(string name, SqlLoginMode loginMode, Version productVersion)
            : base(name, loginMode)
        {
            ProductVersion = productVersion;
            IsLocalDB = true;
        }
    }
}
