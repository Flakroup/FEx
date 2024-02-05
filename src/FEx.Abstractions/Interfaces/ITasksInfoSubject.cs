namespace FEx.Abstractions.Interfaces;

public interface ITasksInfoSubject
{
    void AddTask(ITaskWrapper value);
    void RemoveTask(ITaskWrapper value);
}