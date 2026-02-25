namespace Shared.UI.Api.Base.Modules;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Shared.Infra.Module.Base;
using Shared.Infra.Module.Base.Attributes;
using Shared.UI.Api.Base.Modules.Options;

/// <summary>
/// Módulo para configuração do Swagger (documentação de API).
/// </summary>
[ModuleAttribute(Title = "Scalar UI Module", Description = "Configures Scalar interface for API documentation. Provides a modern and interactive UI to explore and test endpoints using OpenAPI specification.")]
public class ScalarUIModule(IOptionsMonitor<OpenApiOptions> opt) : IWebModule
{
    public void AddConfigurations(WebModuleConfigurationOptions configurationOptions)
    {

    }

    public void ConfigureHost(WebApplicationBuilder builder)
    {
        builder.Services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, ct) =>
            {
                if (opt.CurrentValue != null)
                {
                    document.Info = new OpenApiInfo
                    {
                        Title = opt.CurrentValue.Title,
                        Description = opt.CurrentValue.Description
                    };
                }

                return Task.CompletedTask;
            });
        });
    }

    public void Configure(WebApplication app)
    {
        // Habilita Swagger apenas em ambiente de desenvolvimento
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
            app.MapGet("/", () => Results.Redirect("/scalar"));
        }
    }
}
