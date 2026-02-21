using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Shared.UI.Api.Base.Controllers.Diagnostics;

[ApiController]
[Route("version")]
public class VersionController : ControllerBase
{
    private readonly ILogger<VersionController> _logger;

    public VersionController(ILogger<VersionController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Endpoint para obter a versão da aplicação
    /// GET /version
    /// </summary>
    [HttpGet]
    public IActionResult GetVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "Unknown";
        
        var fileVersion = assembly
            .GetCustomAttribute<AssemblyFileVersionAttribute>()
            ?.Version ?? "Unknown";
        
        var assemblyVersion = assembly.GetName().Version?.ToString() ?? "Unknown";

        _logger.LogInformation("Version endpoint accessed - Version: {Version}", informationalVersion);

        return Ok(new
        {
            Version = informationalVersion,
            FileVersion = fileVersion,
            AssemblyVersion = assemblyVersion
        });
    }
}
