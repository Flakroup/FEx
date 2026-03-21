using FEx.MVVM.Abstractions.Interfaces;
using System;

namespace FEx.MVVM.Abstractions.Extensions;

public static class LinkableNotifyPropertyChangedExtensions
{
    public static void Unlink(this ILinkableNotifyPropertyChanged source, ILink link) =>
        source.Unlink(link, false);

    public static void Unlink(this ILinkableNotifyPropertyChanged source, Guid linkId, string propertyName, Type propertyType) =>
        source.Unlink(linkId, propertyName, propertyType, false);
}
