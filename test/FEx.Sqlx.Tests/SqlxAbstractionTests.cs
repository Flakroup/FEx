using FEx.Sqlx.Abstractions;
using Microsoft.SqlServer.Management.Smo;
using NSubstitute;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace FEx.Sqlx.Tests;

/// <summary>Consumers read instance data and enumerate instances through Sqlx.Abstractions only.</summary>
public sealed class SqlxAbstractionTests
{
    [Fact]
    public void SqlDbHelper_SatisfiesTheAbstraction()
    {
        typeof(ISqlDbHelper).IsAssignableFrom(typeof(SqlDbHelper)).ShouldBeTrue();
        typeof(ISqlInstanceInfo).IsAssignableFrom(typeof(SQLInstanceInfo)).ShouldBeTrue();
        typeof(ISqlInstanceProvider).IsAssignableFrom(typeof(SqlInstanceProvider)).ShouldBeTrue();
    }

    [Fact]
    public void DbHelper_ExposesInstanceInfoThroughTheInterface()
    {
        var helper = Substitute.For<ISqlDbHelper>();
        helper.SQLInstanceInfo.Returns(new TestInstanceInfo("host"));

        helper.SQLInstanceInfo!.LoginMode.ShouldBe(SqlLoginMode.Mixed);
    }

    [Fact]
    public void InstanceInfo_CarriesValuesThroughTheInterface()
    {
        ISqlInstanceInfo info = new TestInstanceInfo("host\\SQL");

        info.SQLInstance.ShouldBe("host\\SQL");
        info.ProductVersion.ShouldBe(new Version(16, 0, 1000));
        info.IsLocalDB.ShouldBeTrue();
        info.LoginMode.ShouldBe(SqlLoginMode.Mixed);
    }

    [Fact]
    public void InstanceInfo_WithoutServer_ReportsUnknownLoginMode() =>
        new SQLInstanceInfo("host").LoginMode.ShouldBe(SqlLoginMode.Unknown);

    [Fact]
    public async Task Provider_ReturnsOnlyAbstractionTypes()
    {
        ISqlInstanceProvider provider = new SqlInstanceProvider();

        var instances = await provider.GetSqlInstancesAsync();

        instances.ShouldNotBeNull();
    }

    [Fact]
    public void AbstractionEnums_MirrorEverySmoMember()
    {
        foreach (var value in Enum.GetValues<ServerLoginMode>().Where(x => x != ServerLoginMode.Unknown))
            ((int)value).ShouldBe((int)Enum.Parse<SqlLoginMode>(value.ToString()));

        foreach (var value in Enum.GetValues<FileStreamEffectiveLevel>())
            ((int)value).ShouldBe((int)Enum.Parse<SqlFileStreamLevel>(value.ToString().Replace("TSql", "Sql")));

        foreach (var value in Enum.GetValues<HadrManagerStatus>())
            ((int)value).ShouldBe((int)Enum.Parse<SqlHadrManagerStatus>(value.ToString()));
    }

    private sealed class TestInstanceInfo : SQLInstanceInfo
    {
        public TestInstanceInfo(string name) : base(name)
        {
            ProductVersion = new(16, 0, 1000);
            IsLocalDB = true;
        }

        public override SqlLoginMode LoginMode => SqlLoginMode.Mixed;
    }
}
