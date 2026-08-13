using Microsoft.AspNetCore.Builder;
using Serilog;

namespace FEx.Logging.Web;

public static class FExSerilogWebExtensions
{
    public static WebApplicationBuilder AddFExSerilog(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext();
        });
        return builder;
    }
}
