namespace Admin;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SqliteBrowser.UI;

/// <summary>
/// Módulo Admin: Blazor + rotas. SQLite Browser vem da DLL SqliteBrowser.UI; Hangfire do WorkerDashboard.
/// Admin gerencia toda a aplicação: banco de dados + dashboard do worker.
/// </summary>
public sealed class AdminModule : Shared.Infra.Module.Base.IWebModule
{
    public void AddConfigurations(Shared.Infra.Module.Base.WebModuleConfigurationOptions configurationOptions) { }

    public void ConfigureHost(WebApplicationBuilder builder)
    {
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();
    }

    public void Configure(WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            app.UseExceptionHandler("/Error");

        app.UseStaticFiles();
        app.UseRouting();
        app.MapRazorPages();
        app.MapBlazorHub();
        app.MapFallbackToPage("/_Host");
    }
}
