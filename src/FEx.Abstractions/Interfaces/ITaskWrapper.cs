using FEx.Abstractions.Flow;
using FEx.Abstractions.Flow.Errors;
using System.Threading.Tasks;

namespace FEx.Abstractions.Interfaces;

public interface ITaskWrapper : ITaskWrapperBase<Task, Result<ExceptionError>>
{
}

public interface ITaskWrapper<T> : ITaskWrapperBase<Task<T>, Result<T, ExceptionError>>
{
}