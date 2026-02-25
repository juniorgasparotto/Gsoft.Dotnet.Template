using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Shared.UI.Api.Base.Controllers.Diagnostics;

[ApiController]
[Route("diagnostics/configuration")]
public class ConfigurationController : ControllerBase
{
    private readonly ILogger<ConfigurationController> _logger;
    private readonly IConfiguration _configuration;

    public ConfigurationController(ILogger<ConfigurationController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Lista todas as configurações disponíveis na aplicação
    /// GET /diagnostics/configuration
    /// </summary>
    [HttpGet]
    public IActionResult GetAllConfigurations()
    {
        var configurations = new Dictionary<string, object?>();
        
        // Percorrer todas as configurações
        foreach (var item in _configuration.AsEnumerable())
            configurations[item.Key] = MaskSensitiveValue(item.Key, item.Value);

        return Ok(new
        {
            TotalKeys = configurations.Count,
            Configurations = configurations
        });
    }

    private static void BuildHierarchicalStructure(Dictionary<string, object?> root, string key, object? value)
    {
        var parts = key.Split(':');
        var current = root;

        for (int i = 0; i < parts.Length - 1; i++)
        {
            var part = parts[i];
            if (!current.ContainsKey(part))
            {
                current[part] = new Dictionary<string, object?>();
            }

            if (current[part] is Dictionary<string, object?> dict)
            {
                current = dict;
            }
            else
            {
                // Conflito: já existe um valor não-dicionário nesta chave
                return;
            }
        }

        current[parts[^1]] = value;
    }

    private static string? MaskSensitiveValue(string key, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        // Lista de palavras-chave que indicam valores sensíveis
        var sensitiveKeywords = Array.Empty<string>();

        var keyLower = key.ToLowerInvariant();
        var isSensitive = sensitiveKeywords.Any(keyword => keyLower.Contains(keyword));

        if (isSensitive && !string.IsNullOrEmpty(value))
        {
            // Mascarar valores sensíveis (mostrar apenas primeiros e últimos caracteres)
            if (value.Length <= 8)
            {
                return "***";
            }

            var visibleLength = Math.Min(4, value.Length / 4);
            return $"{value.Substring(0, visibleLength)}...{value.Substring(value.Length - visibleLength)}";
        }

        return value;
    }
}
