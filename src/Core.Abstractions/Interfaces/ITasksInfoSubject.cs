using FEx.Agnostics.Abstractions.Interfaces;
using System;

namespace FEx.Core.Abstractions.Interfaces;

public interface ITasksInfoSubject
{
    void AddTask(ITaskWrapperBase value);
    void AddTask(Guid taskId);
    void RemoveTask(ITaskWrapperBase value);
    void RemoveTask(Guid taskId);
}