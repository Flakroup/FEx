using FEx.Abstractions;
using FEx.Utilities.Subjects;
using StrongInject;

namespace FEx.Utilities;

[Register(typeof(TasksInfoSubject), Scope.SingleInstance, typeof(ITasksInfoSubject))]
public class FExUtilitiesModule
{
}