using System;
using System.IO;
using FEx.McpServer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;

var repoPath = Environment.GetEnvironmentVariable("FEX_REPO_PATH")
    ?? Directory.GetCurrentDirectory();

var apiSurfacePath = Path.Combine(repoPath, ".api-surface", "FEx");

if (!Directory.Exists(apiSurfacePath))
{
    Console.Error.WriteLine($"API surface not found: {apiSurfacePath}");
    Console.Error.WriteLine("Set FEX_REPO_PATH environment variable to the FEx repository root.");
    return;
}

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton(new ApiSurfaceConfig(apiSurfacePath, repoPath));
builder.Services.AddSingleton<TomlApiSurfaceReader>();
builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
