using Microsoft.AspNetCore.Mvc;

namespace FEx.Sample.WebAPI.Controllers;

/// <summary>
/// Sample controller demonstrating FEx service usage in ASP.NET Core
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SampleController : ControllerBase
{
    [HttpGet("di-info")]
    public IActionResult GetDIInfo()
    {
        Console.WriteLine("DI Info endpoint called");

        return Ok(new
        {
            Framework = "FEx",
            Pattern = "Multi-DI (StrongInject + Microsoft DI)",
            StrongInject = "Base container - always on",
            MicrosoftDI = "Optional - integrated via IInitializeModule<IServiceCollection>",
            EngineAgnostic = "Ready for any DI engine (Autofac, Unity, etc.)",
            Modules = new[] { "FExDependencyInjectionModule", "SampleApiModule (custom app module)" }
        });
    }

    [HttpGet("test-framework")]
    public IActionResult TestFramework()
    {
        Console.WriteLine("Test framework endpoint - demonstrating Multi-DI integration");

        return Ok(new
        {
            Message = "FEx Multi-DI pattern working successfully!",
            Timestamp = DateTime.UtcNow,
            InitializedWith = "StrongInject + Microsoft DI"
        });
    }
}