namespace FEx.Abstractions.Interfaces;

public interface ITasksInfoSubject
{
    void AddTask(ITaskWrapperBase value);
    void RemoveTask(ITaskWrapperBase value);
}