using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace FEx.DI.Abstractions.Interfaces;

public interface IFExMicrosoftDIServiceProvider : IFExServiceProvider, IAsyncDisposable
{
    Task ConfigureServiceProviderAsync(Func<IServiceCollection, IServiceCollection> configuration = null,
                                       IServiceCollection services = null);
}