using FEx.Agnostics.Abstractions.Interfaces;
using System;

namespace FEx.MVVM.Abstractions.Interfaces;

public interface ILinkableNotifyPropertyChanged : IFExNotifyPropertyChanged
{
    void AddLink(ILink link);
    void Unlink(ILink link, bool resetProperty);
    void Unlink(Guid linkId, string propertyName, Type propertyType, bool resetProperty);
}