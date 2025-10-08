using FEx.DependencyInjection;
using FEx.DependencyInjection.Abstractions;

namespace FEx.Sample.WebAPI;

/// <summary>
/// Sample ASP.NET Core Web API demonstrating Multi-DI pattern.
/// Shows how to integrate FEx with both StrongInject and Microsoft DI.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        // Step 1: Initialize StrongInject (FEx foundation)
        using AppContainer? container = FExServiceProvider.Initialize<AppContainer, FExStrongInjectServiceProvider>();

        // Step 2: Initialize Microsoft DI (integrates FEx modules into ASP.NET)
        await FExServiceProvider.InitializeAsync<FExMicrosoftDIServiceProvider>();

        Console.WriteLine("Multi-DI initialized: StrongInject + Microsoft DI");

        // Step 3: Build ASP.NET Core application
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // Add ASP.NET specific services
        builder.Services.AddOpenApi();
        builder.Services.AddControllers();

        WebApplication app = builder.Build();

        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
            app.MapOpenApi();

        app.UseHttpsRedirection();
        app.MapControllers();

        // Sample endpoint demonstrating Multi-DI
        app.MapGet("/health",
                () =>
                {
                    Console.WriteLine("Health check endpoint called");

                    return Results.Ok(new
                    {
                        Status = "Healthy",
                        Framework = "FEx Multi-DI",
                        DI = "StrongInject + Microsoft DI"
                    });
                })
            .WithName("HealthCheck");

        app.MapGet("/weatherforecast",
                () =>
                {
                    string[] summaries = new[]
                    {
                        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering",
                        "Scorching"
                    };

                    WeatherForecast[] forecast = Enumerable.Range(1, 5)
                        .Select(index => new WeatherForecast(DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                            Random.Shared.Next(-20, 55),
                            summaries[Random.Shared.Next(summaries.Length)]))
                        .ToArray();

                    Console.WriteLine($"Generated {forecast.Length} weather forecasts");

                    return forecast;
                })
            .WithName("GetWeatherForecast");

        await app.RunAsync();
    }
}

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}