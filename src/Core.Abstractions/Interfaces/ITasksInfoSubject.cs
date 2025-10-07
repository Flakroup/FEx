using System;

namespace FEx.Abstractions.Interfaces;

public interface ITasksInfoSubject
{
    void AddTask(ITaskWrapperBase value);
    void AddTask(Guid taskId);
    void RemoveTask(ITaskWrapperBase value);
    void RemoveTask(Guid taskId);
}