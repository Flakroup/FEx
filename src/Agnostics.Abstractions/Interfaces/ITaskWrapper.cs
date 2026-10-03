using FEx.Agnostics.Abstractions.Flow;
using System.Threading.Tasks;

namespace FEx.Agnostics.Abstractions.Interfaces;

/// <summary>Task wrapper for a task without a return value.</summary>
public interface ITaskWrapper : ITaskWrapperBase<Task, Result<ExceptionError>>
{
}

/// <summary>Task wrapper for a task that returns a value</summary>
/// <typeparam name="T">The task result type.</typeparam>
public interface ITaskWrapper<T> : ITaskWrapperBase<Task<T>, Result<T, ExceptionError>>
{
}