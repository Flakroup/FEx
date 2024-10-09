using FEx.Abstractions.Interfaces;
using FEx.Fundamentals.Utilities;
using StrongInject;

namespace FEx.Fundamentals;

[Register(typeof(ExceptionHandler), Scope.SingleInstance, typeof(IExceptionHandler))]
public class FExDefaultFundamentalsModule : FExFundamentalsModule
{
}