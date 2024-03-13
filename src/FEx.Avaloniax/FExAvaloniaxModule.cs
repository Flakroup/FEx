using FEx.Abstractions.Interfaces;
using StrongInject;

namespace FEx.Avaloniax;

[Register(typeof(AvaloniaDispatcher), typeof(IFExDispatcher))]
public class FExAvaloniaxModule
{
}