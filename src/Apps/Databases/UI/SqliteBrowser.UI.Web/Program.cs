using Microsoft.Extensions.ServiceDiscovery;
using SqliteBrowser.UI.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<SqliteBrowserStateService>();

// Com Aspire: usa service discovery (https+http://SqliteBrowserApi)
// Sem Aspire: usa SqliteBrowser:ApiBaseUrl do appsettings (ex: http://localhost:5200)
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

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Error");

app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
