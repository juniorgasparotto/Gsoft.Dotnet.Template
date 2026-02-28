namespace SqliteBrowser.UI;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SqliteBrowser.UI.Services;
using Shared.Infra.Module.Base;

/// <summary>
/// Módulo que adiciona o SQLite Browser UI a uma aplicação (Blazor + serviços + HttpClient).
/// Não faz MapBlazorHub nem rotas – o host deve configurar isso.
/// </summary>
public sealed class SqliteBrowserUIModule : IWebModule
{
    public void AddConfigurations(WebModuleConfigurationOptions configurationOptions) { }

    public void ConfigureHost(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<SqliteBrowserStateService>();

        var apiBaseUrl = builder.Configuration["SqliteBrowser:ApiBaseUrl"]?.TrimEnd('/');
        var httpBuilder = builder.Services.AddHttpClient<SqliteBrowserApiService>(static (sp, client) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var baseUrl = config["SqliteBrowser:ApiBaseUrl"]?.TrimEnd('/');
            if (!string.IsNullOrEmpty(baseUrl))
                client.BaseAddress = new Uri(baseUrl);
            else
                client.BaseAddress = new("https+http://SqliteBrowserApi");
        });
        if (string.IsNullOrEmpty(apiBaseUrl))
            httpBuilder.AddServiceDiscovery();
    }

    public void Configure(WebApplication app) { }
}
