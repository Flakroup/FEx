using DynamicData;
using DynamicData.Binding;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace FEx.Rx.Tests;

public class DTests
{
    private readonly ITestOutputHelper _output;

    public DTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task D_TestAsync()
    {
        string[] props = [nameof(Tester.Value), nameof(Tester.TestIt)];
        var cache = new SourceCache<Tester, string>(x => x.Key);

        IObservable<IChangeSet<Tester, string>> s = cache.Connect()
            .AutoRefreshOnObservable(x => x.WhenAnyPropertyChanged(props));

        s = AddMappedProps(s, props);

        using IDisposable m = s.Subscribe(x =>
        {
            Change<Tester, string> f = x.First();
            _output.WriteLine($"change:{f.Reason}\t{f.Key}");
        });

        var testerA = new Tester
        {
            Key = Guid.NewGuid().ToString(),
            TestIt = new NestedTester()
        };

        cache.AddOrUpdate(testerA);
        await Task.Delay(1000);
        testerA.Value = true;
        await Task.Delay(1000);
        testerA.TestIt.Value = true;
        await Task.Delay(1000);
        cache.Edit(x => x.Remove(testerA.Key));
        testerA.Value = false;
    }

    /*
        private IObservable<IChangeSet<TValue, TKey>> ApplyObservable<TKey, TValue, TObject>(
            IObservable<IChangeSet<TValue, TKey>> observable,
            Delegate delegateFunc) where TObject : INotifyPropertyChanged
        {
            var c = (Func<TValue, TObject>)delegateFunc;
            observable = observable.AutoRefreshOnObservable(x => c(x).WhenAnyPropertyChanged());
            return observable;
        }
    */

    [SuppressMessage("ReSharper", "UnusedVariable")]
    private IObservable<IChangeSet<Tester, string>> AddMappedProps(IObservable<IChangeSet<Tester, string>> observable,
                                                                   string[] props)
    {
        foreach (PropertyInfo p in typeof(Tester).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .Where(x => props.Contains(x.Name)))
        {
            if (typeof(INotifyPropertyChanged).IsAssignableFrom(p.PropertyType))
            {
                ParameterExpression parameter = Expression.Parameter(typeof(Tester), "i");
                MemberExpression property = Expression.Property(parameter, p.Name);
                Type delegateType = typeof(Func<,>).MakeGenericType(typeof(Tester), p.PropertyType);

                LambdaExpression yourExpression = Expression.Lambda(delegateType, property, parameter);
                //Func<Tester, NestedTester> c = GetFunc(yourExpression.Compile());
                //observable = ApplyObservable(observable, c);
            }
        }

        return observable;
    }
}

public class Tester : AbstractNotifyPropertyChanged
{
    private bool _value;
    public string Key { get; set; }

    public bool Value
    {
        get => _value;
        set => SetAndRaise(ref _value, value);
    }

    public NestedTester TestIt { get; set; }
}

public class NestedTester : AbstractNotifyPropertyChanged
{
    private bool _value;

    public bool Value
    {
        get => _value;
        set => SetAndRaise(ref _value, value);
    }
}