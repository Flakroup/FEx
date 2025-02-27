using FEx.Abstractions.Interfaces;
using FEx.Fundamentals.Helpers;
using FEx.Fundamentals.Utilities;
using StrongInject;

namespace FEx.Fundamentals;

[Register(typeof(DefaultDispatcher), Scope.SingleInstance, typeof(IFExDispatcher))]
[Register(typeof(ExceptionHandler), Scope.SingleInstance, typeof(IExceptionHandler))]
public class FExDefaultFundamentalsModule : FExFundamentalsModule
{
}