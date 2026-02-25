namespace Shared.UI.Api.Base.Modules;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infra.Module.Base;
using Shared.Infra.Module.Base.Attributes;

/// <summary>
/// Módulo para configuração de CORS (Cross-Origin Resource Sharing).
/// </summary>
[ModuleAttribute(Title = "CORS Module", Description = "Configures CORS (Cross-Origin Resource Sharing) to allow requests from different origins. By default, configures permissive policy for development.")]
public class CorsModule : IWebModule
{
    public void AddConfigurations(WebModuleConfigurationOptions configurationOptions)
    {
    }

    public void ConfigureHost(WebApplicationBuilder builder)
    {
        // Configuração padrão de CORS (permissivo para desenvolvimento)
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });
    }

    public void Configure(WebApplication app)
    {
        // Aplica o middleware de CORS
        app.UseCors();
    }
}
