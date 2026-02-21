namespace Shared.UI.Api.Base.Modules;

using Microsoft.AspNetCore.Builder;
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
/// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/using-openapi-documents?view=aspnetcore-10.0
/// </summary>
[ModuleAttribute(Title = "Swagger Module", Description = "Configures Swagger UI for interactive API documentation. Provides web interface to explore and test API endpoints using OpenAPI specification.")]
public class SwaggerModule(IOptions<OpenApiOptions> opt) : IWebModule
{
    public void AddConfigurations(WebModuleConfigurationOptions configurationOptions)
    {

    }

    public void ConfigureHost(WebApplicationBuilder builder)
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            if (opt.Value != null)
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = opt.Value.Title,
                    Version = "v1",
                    Description = opt.Value.Description
                });
            }
        });
    }

    public void Configure(WebApplication app)
    {
        // Habilita Swagger apenas em ambiente de desenvolvimento
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger(options =>
            {
                options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
            });

            app.UseSwaggerUI(options =>
            {
                if (opt.Value != null)
                {
                    options.DocumentTitle = opt.Value.Title;
                }
            });
        }
    }
}