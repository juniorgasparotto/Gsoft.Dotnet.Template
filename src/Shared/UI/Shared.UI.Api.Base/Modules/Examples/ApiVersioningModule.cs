//using Microsoft.AspNetCore.Builder;
//// using Asp.Versioning;

///// <summary>
///// Módulo para versionamento de API.
///// 
///// REQUER: dotnet add package Asp.Versioning.Mvc
///// 
///// Suporta: URL (/api/v1/users), Header (X-Api-Version), Query String (?api-version=1.0)
///// </summary>
//public class ApiVersioningModule
//{
//    public int Order => 25;

//    public void ConfigureServices(WebApplicationBuilder builder)
//    {
//        // Descomente após instalar o pacote Asp.Versioning.Mvc
        
//        // builder.Services.AddApiVersioning(options =>
//        // {
//        //     options.DefaultApiVersion = new ApiVersion(1, 0);
//        //     options.AssumeDefaultVersionWhenUnspecified = true;
//        //     options.ReportApiVersions = true;
//        //     options.ApiVersionReader = ApiVersionReader.Combine(
//        //         new UrlSegmentApiVersionReader(),
//        //         new HeaderApiVersionReader("X-Api-Version"),
//        //         new QueryStringApiVersionReader("api-version"));
//        // });
//    }
//}
