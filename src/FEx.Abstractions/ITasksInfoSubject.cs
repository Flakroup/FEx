namespace FEx.Abstractions;

public interface ITasksInfoSubject
{
    void AddTask(ITaskWrapper value);
    void RemoveTask(ITaskWrapper value);
}