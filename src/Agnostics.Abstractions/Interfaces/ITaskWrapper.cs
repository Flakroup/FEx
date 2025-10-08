using FEx.Agnostics.Abstractions.Flow;
using System.Threading.Tasks;

namespace FEx.Agnostics.Abstractions.Interfaces;

public interface ITaskWrapper : ITaskWrapperBase<Task, Result<ExceptionError>>
{
}

public interface ITaskWrapper<T> : ITaskWrapperBase<Task<T>, Result<T, ExceptionError>>
{
}