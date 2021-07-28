using FEx.Abstractions;
using FEx.Utilities;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace FEx.DependencyInjection
{
    public class FExIoCProvider : IFExIoCProvider
    {
        /// <summary>
        /// Returns a singleton instance of the default IoC Provider. If possible use dependency injection instead.
        /// </summary>
        public static IFExIoCProvider IoCProvider => FExSingleton<FExIoCProvider>.Instance;

        private IServiceProvider Provider { get; set; }
        private IServiceCollection Services { get; set; }

        public IServiceProvider ConfigureServiceProvider(
            Func<IServiceCollection, IServiceCollection> configuration = null,
            IServiceCollection services = null,
            bool buildProvider = true)
        {
            if (Provider != null)
            {
                throw new InvalidOperationException("Provider is already configured");
            }

            if (services == null)
            {
                services = new ServiceCollection();
            }

            if (configuration != null)
            {
                services = configuration(services);
            }

            if (buildProvider)
            {
                BuildServiceProvider();
            }

            return Provider;

        }

        public void BuildServiceProvider()
        {
            Provider = Services.BuildServiceProvider();
        }

        /// <summary>
        /// Get service of type <typeparamref name="T"/> from the <see cref="IServiceProvider"/>.
        /// </summary>
        /// <typeparam name="T">The type of service object to get.</typeparam>
        /// <returns>A service object of type <typeparamref name="T"/>.</returns>
        /// <exception cref="System.InvalidOperationException">There is no service of type <typeparamref name="T"/>.</exception>
        public T GetRequiredService<T>()
        {
            return Provider.GetRequiredService<T>();
        }

        /// <summary>
        /// Get service of type <paramref name="serviceType" /> from the <see cref="IServiceProvider" />.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serviceType">An object that specifies the type of service object to get.</param>
        /// <returns>
        /// A service object of type <paramref name="serviceType" />.
        /// </returns>
        /// <exception cref="System.InvalidOperationException">There is no service of type <paramref name="serviceType" />.</exception>
        public T GetRequiredService<T>(Type serviceType)
        {
            return (T)Provider.GetRequiredService(serviceType);
        }

        public object GetRequiredService(Type serviceType)
        {
            return Provider.GetRequiredService(serviceType);
        }

        public T GetService<T>() => Provider.GetService<T>();
    }
}
