using Shouldly;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace FEx.MSBuildx.Tests;

public sealed class MSProjectItemTests : IDisposable
{
    private readonly MSBuildTestWorkspace _workspace = new();

    public void Dispose() => _workspace.Dispose();

    [Fact]
    public void Item_MirrorsTheEvaluatedProjectItem()
    {
        var props = MSBuildTestWorkspace.Props(extra: "<Folder>assets</Folder>");
        var project = _workspace.Load(_workspace.WriteProject("Demo.csproj",
            props,
            """<None Include="$(Folder)_logo.png" Link="logo.png" Pack="true" />"""));

        var item = project.IncludedFiles.ShouldHaveSingleItem();

        item.ItemType.ShouldBe("None");
        item.EvaluatedInclude.ShouldBe("assets_logo.png");
        item.UnevaluatedInclude.ShouldBe("$(Folder)_logo.png");
        item.DirectMetadataCount.ShouldBe(2);
        item.ProjectItem.EvaluatedInclude.ShouldBe("assets_logo.png");
        item.FullPath.ShouldBe(Path.Combine(project.ProjectDir, "assets_logo.png"));
        item.Extension.ShouldBe(".png");
    }

    [Theory]
    [InlineData("None", ProjectItemType.None)]
    [InlineData("EmbeddedResource", ProjectItemType.EmbeddedResource)]
    public void ProjectItemType_IsMappedFromTheItemTypeName(string itemType, ProjectItemType expected)
    {
        var project = _workspace.Load(_workspace.WriteProject("Demo.csproj", items: $"""<{itemType} Include="a.txt" />"""));

        project.IncludedFiles.ShouldHaveSingleItem().ProjectItemType.ShouldBe(expected);
    }

    [Fact]
    public void ProjectItemType_IsNull_ForAnItemTypeWithoutAMapping()
    {
        var project = _workspace.Load(_workspace.WriteProject("Demo.csproj", items: """<Compile Include="a.cs" />"""));

        project.IncludedFiles.ShouldHaveSingleItem().ProjectItemType.ShouldBeNull();
    }

    [Fact]
    public void ProjectItemTypes_IsKeyedByTheItemTypeName()
    {
        MSProjectItem.ProjectItemTypes.ForwardIndex.Keys.OrderBy(x => x).ShouldBe(["EmbeddedResource", "None"]);
        MSProjectItem.ProjectItemTypes.ForwardIndex["None"].ShouldBe(ProjectItemType.None);
    }

    [Fact]
    public void AcceptedItemsNames_ListsTheItemTypesTheModuleTracks() =>
        MSProject.AcceptedItemsNames.OrderBy(x => x).ShouldBe(["EmbeddedResource", "None"]);

    [Fact]
    public void IgnoredItemsNames_CoversTheReferenceItemTypes()
    {
        MSProject.PackageRefStr.ShouldBe("PackageReference");
        MSProject.IgnoredItemsNames.ShouldContain("Reference");
        MSProject.IgnoredItemsNames.ShouldContain("PackageReference");
        MSProject.IgnoredItemsNames.ShouldNotContain("Compile");
    }
}
