using FEx.MVVM.BaseObjects;
using FEx.MVVM.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace FEx.MVVM.Tests;

public class LinkedPropertiesTests
{
    private readonly ITestOutputHelper _output;

    public LinkedPropertiesTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void LinkMemberPropertyTest()
    {
        const string aName = "Zbyszko";
        const string bName = "Monia";
        const int aAge = 16;
        const int bAge = 10;
        var p = new SingleParent(true)
        {
            Child = new Child
            {
                Info =
                {
                    Name = aName,
                    Age = aAge
                }
            }
        };
        Child c = p.Child;
        Assert.Equal(aName, p.ChildName);
        Assert.Equal(aAge, p.ChildAge);

        p.Child = null;
        Assert.Equal(default, p.ChildName);
        Assert.Equal(default, p.ChildAge);

        p.Child = c;
        Assert.Equal(aName, p.ChildName);
        Assert.Equal(aAge, p.ChildAge);

        p.Child.Info.Name = bName;
        p.Child.Info.Age = bAge;
        Assert.Equal(bName, p.ChildName);
        Assert.Equal(bAge, p.ChildAge);
    }
}

public class Child : LinkableNotifyPropertyChanged
{
    private ChildInfo _info;

    public ChildInfo Info
    {
        get => _info;
        set => SetProperty(ref _info, value);
    }

    public Child()
    {
        Info = new ChildInfo();
    }
}

public class ChildInfo : LinkableNotifyPropertyChanged
{
    private string _name;
    private int _age;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public int Age
    {
        get => _age;
        set => SetProperty(ref _age, value);
    }
}

public class SingleParent : LinkableNotifyPropertyChanged
{
    public static void Link(SingleParent p)
    {
        p.Link(x => x.Child, (l, c) =>
        {
            l.RelinkChildren(c, () =>
            {
                c.LinkChild(x => x.Info, (cl, i) =>
                {
                    cl.RelinkChildren(i, () =>
                    {
                        i.LinkChild(x => x.Age, a => p.ChildAge = a, cl);
                        i.LinkChild(x => x.Name, n => p.ChildName = n, cl);
                    });
                }, l);
            });
        });
    }

    private Child _child;

    public Child Child
    {
        get => _child;
        set => SetProperty(ref _child, value);
    }

    public int ChildAge { get; private set; }
    public string ChildName { get; private set; }

    public SingleParent(bool link)
    {
        if (link)
            Link(this);
    }
}