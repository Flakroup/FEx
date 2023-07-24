using FEx.Basics.Interfaces;
using FEx.Fundamentals.StackTraces;
using StrongInject;

namespace FEx.Fundamentals;

[Register(typeof(Foundation), Scope.SingleInstance)]
[Register(typeof(StackTraceGenerator), Scope.SingleInstance, typeof(IStackTraceProvider))]
public class FExFoundationModule
{
}