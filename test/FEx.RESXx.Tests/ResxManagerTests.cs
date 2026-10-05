using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Resources;
using Xunit;

namespace FEx.RESXx.Tests;

public sealed class ResxManagerTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "FExResxTests", Guid.NewGuid().ToString("N"));
    private readonly ResxManager _sut = new();

    public ResxManagerTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try
        {
            Directory.Delete(_dir, true);
        }
        catch (IOException)
        {
            // best effort: the folder lives under the temp path
        }
        catch (UnauthorizedAccessException)
        {
            // same as above
        }
    }

    private string PathOf(string name) => Path.Combine(_dir, name);

    private string WriteResx(string name, params (string Key, string Value)[] entries)
    {
        var path = PathOf(name);

        using (var writer = new ResXResourceWriter(path))
        {
            foreach (var (key, value) in entries)
                writer.AddResource(key, value);

            writer.Generate();
        }

        return path;
    }

    private static List<KeyValuePair<string, string>> Data(params (string Key, string Value)[] entries)
    {
        var data = new List<KeyValuePair<string, string>>();

        foreach (var (key, value) in entries)
            data.Add(new(key, value));

        return data;
    }

    [Fact]
    public void GetResourcesEntries_MissingFile_IsEmpty() =>
        _sut.GetResourcesEntries(PathOf("missing.resx")).ShouldBeEmpty();

    [Fact]
    public void GetResourcesEntries_ExistingFile_ReturnsItsStrings()
    {
        var path = WriteResx("a.resx", ("Hello", "Hi there"), ("Bye", "See you"));

        var entries = _sut.GetResourcesEntries(path);

        entries.Count.ShouldBe(2);
        entries["Hello"].ShouldBe("Hi there");
        entries["Bye"].ShouldBe("See you");
    }

    [Fact]
    public void GetResourcesEntries_RepeatedValue_KeepsOnlyTheFirstKey()
    {
        var path = WriteResx("dups.resx", ("First", "same"), ("Second", "same"), ("Third", "other"));

        var entries = _sut.GetResourcesEntries(path);

        entries.Count.ShouldBe(2);
        entries.Values.ShouldContain("same");
        entries.Values.ShouldContain("other");
        entries.Values.ShouldBe(["same", "other"], true);
    }

    [Fact]
    public void GetResourcesEntries_EmptyValue_IsReadAsAnEmptyString()
    {
        var path = WriteResx("empty.resx", ("Blank", string.Empty));

        _sut.GetResourcesEntries(path)["Blank"].ShouldBeEmpty();
    }

    [Fact]
    public void UpdateResourceFile_EmptyData_DoesNotCreateTheFile()
    {
        var path = PathOf("none.resx");

        _sut.UpdateResourceFile([], path);

        File.Exists(path).ShouldBeFalse();
    }

    [Fact]
    public void UpdateResourceFile_NewFileInANewFolder_IsCreatedWithTheEntries()
    {
        var path = Path.Combine(_dir, "nested", "deeper", "new.resx");

        _sut.UpdateResourceFile(Data(("Title", "Welcome")), path);

        File.Exists(path).ShouldBeTrue();
        _sut.GetResourcesEntries(path)["Title"].ShouldBe("Welcome");
    }

    [Fact]
    public void UpdateResourceFile_ExistingFile_KeepsOldEntriesAndAddsNewOnes()
    {
        var path = WriteResx("merge.resx", ("Old", "old value"));

        _sut.UpdateResourceFile(Data(("New", "new value")), path);

        var entries = _sut.GetResourcesEntries(path);
        entries["Old"].ShouldBe("old value");
        entries["New"].ShouldBe("new value");
    }

    [Fact]
    public void UpdateResourceFile_ValueAlreadyPresent_IsNotAddedUnderANewKey()
    {
        var path = WriteResx("known.resx", ("Existing", "shared text"));

        _sut.UpdateResourceFile(Data(("Another", "shared text")), path);

        var entries = _sut.GetResourcesEntries(path);
        entries.Count.ShouldBe(1);
        entries.ContainsKey("Another").ShouldBeFalse();
    }

    [Fact]
    public void UpdateResourceFile_KeyTaken_GetsANumericSuffix()
    {
        var path = WriteResx("suffix.resx", ("Key", "v0"));

        _sut.UpdateResourceFile(Data(("Key", "v1"), ("Key", "v2")), path);

        var entries = _sut.GetResourcesEntries(path);
        entries["Key"].ShouldBe("v0");
        entries["Key1"].ShouldBe("v1");
        entries["Key2"].ShouldBe("v2");
    }

    [Fact]
    public void UpdateResourceFile_ReadOnlyFile_IsOverwritten()
    {
        var path = WriteResx("readonly.resx", ("Old", "old value"));
        File.SetAttributes(path, FileAttributes.ReadOnly);

        _sut.UpdateResourceFile(Data(("New", "new value")), path);

        (File.GetAttributes(path) & FileAttributes.ReadOnly).ShouldBe((FileAttributes)0);
        _sut.GetResourcesEntries(path)["New"].ShouldBe("new value");
    }

    [Fact]
    public void Resgen_GeneratesAStronglyTypedDesignerFile()
    {
        var resx = WriteResx("Strings.resx", ("Greeting", "Hello"), ("Farewell", "Goodbye"));
        var designer = PathOf("Strings.Designer.cs");

        var unmatched = _sut.Resgen(resx, designer, "Strings", "My.App");

        unmatched.ShouldBeEmpty();
        var code = File.ReadAllText(designer);
        code.ShouldContain("namespace My.App.Properties");
        code.ShouldContain("class Strings");
        code.ShouldContain("Greeting");
        code.ShouldContain("Farewell");
    }

    [Fact]
    public void Resgen_OverwritesAnExistingDesignerFile()
    {
        var resx = WriteResx("Texts.resx", ("Only", "one"));
        var designer = PathOf("Texts.Designer.cs");
        File.WriteAllText(designer, "stale content that is much longer than nothing");

        _sut.Resgen(resx, designer, "Texts", "My.App");

        File.ReadAllText(designer).ShouldNotContain("stale content");
    }
}
