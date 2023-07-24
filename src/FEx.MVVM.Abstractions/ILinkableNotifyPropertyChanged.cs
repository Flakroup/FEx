using System;

namespace FEx.MVVM.Abstractions;

public interface ILinkableNotifyPropertyChanged : IFExNotifyPropertyChanged
{
    void AddLink(ILink link);
    void Unlink(ILink link, bool resetProperty = false);
    void Unlink(Guid linkId, string propertyName, Type propertyType, bool resetProperty = false);
}