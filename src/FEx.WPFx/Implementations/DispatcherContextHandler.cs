using FEx.MVVM.Abstractions;
using System;
using System.Windows.Threading;

namespace FEx.WPFx.Implementations;

public class DispatcherContextHandler : UIContextHandler
{
    private readonly Func<DispatcherObject> _getDispatcherObject;

    public DispatcherContextHandler(Func<DispatcherObject> getDispatcherObject)
    {
        _getDispatcherObject = getDispatcherObject;
    }

    protected override object GetDispatcherObject() => _getDispatcherObject();
}