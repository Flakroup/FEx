using FEx.Agnostics.Abstractions.Extensions;
using ReactiveUI;
using System.Runtime.CompilerServices;

namespace FEx.MVVM.Rx.Extensions;

public static class ReactiveObjectExtensions
{
    public static bool SetProperty<TObj, TRet>(this TObj sender,
                                               ref TRet backingField,
                                               TRet newValue,
                                               [CallerMemberName] string? propertyName = null)
        where TObj : IReactiveObject
    {
        if (propertyName is not null
            && ObjectExtensions.IsNotEqual(ref backingField, newValue))
        {
            sender.RaiseAndSetIfChanged(ref backingField, newValue, propertyName);

            return true;
        }

        return false;
    }
}