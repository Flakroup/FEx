using FEx.MVVM.Abstractions.Interfaces;

namespace FEx.Legacy.Mvvm.Abstractions.Extensions;

public static class ViewModelBaseExtensions
{
    public static string GetListenerKey<TRec>(this IViewModelBase sender, TRec receiver, string propertyName)
        where TRec : IViewModelBase =>
        $"{sender.Id}=>{receiver.Id}_{propertyName}";
}