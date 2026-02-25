//using Microsoft.AspNetCore.Builder;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;

///// <summary>
///// Módulo para configuração de caching (memória, distribuído e response cache).
///// </summary>
//public class CachingModule
//{
//    public int Order => 15;

//    public void ConfigureServices(WebApplicationBuilder builder, IConfiguration configuration)
//    {
//        // Cache em memória
//        builder.Services.AddMemoryCache();
        
//        // Cache de respostas HTTP
//        builder.Services.AddResponseCaching();
        
//        // Cache distribuído Redis (descomente se necessário)
//        // Requer: dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis
//        // builder.Services.AddStackExchangeRedisCache(options =>
//        // {
//        //     options.Configuration = configuration.GetConnectionString("Redis");
//        //     options.InstanceName = "MyApp_";
//        // });
//    }

//    public void Configure(WebApplication app)
//    {
//        app.UseResponseCaching();
//    }
//}
