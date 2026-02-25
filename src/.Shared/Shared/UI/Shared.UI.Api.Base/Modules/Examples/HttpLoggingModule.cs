//using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.HttpLogging;
//using Microsoft.Extensions.DependencyInjection;

///// <summary>
///// Módulo para logging de requisições HTTP.
///// </summary>
//public class HttpLoggingModule
//{
//    public int Order => 80;

//    public void ConfigureServices(WebApplicationBuilder builder)
//    {
//        builder.Services.AddHttpLogging(options =>
//        {
//            options.LoggingFields = HttpLoggingFields.RequestPropertiesAndHeaders |
//                                   HttpLoggingFields.ResponsePropertiesAndHeaders;
//        });
//    }

//    public void Configure(WebApplication app)
//    {
//        app.UseHttpLogging();
//    }
//}
