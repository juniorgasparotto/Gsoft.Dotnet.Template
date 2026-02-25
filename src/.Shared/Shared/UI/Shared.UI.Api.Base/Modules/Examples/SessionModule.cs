//using Microsoft.AspNetCore.Builder;
//using Microsoft.Extensions.DependencyInjection;

///// <summary>
///// Módulo para gerenciamento de sessão.
///// </summary>
//public class SessionModule
//{
//    public int Order => 70;

//    public void ConfigureServices(WebApplicationBuilder builder)
//    {
//        builder.Services.AddDistributedMemoryCache();
//        builder.Services.AddSession(options =>
//        {
//            options.IdleTimeout = TimeSpan.FromMinutes(30);
//            options.Cookie.HttpOnly = true;
//            options.Cookie.IsEssential = true;
//        });
//    }

//    public void Configure(WebApplication app)
//    {
//        app.UseSession();
//    }
//}
