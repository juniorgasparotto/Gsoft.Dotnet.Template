//using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.ResponseCompression;

///// <summary>
///// Módulo para compressão de respostas HTTP (Brotli e Gzip).
///// </summary>
//public class CompressionModule
//{
//    public int Order => 35;

//    public void ConfigureServices(WebApplicationBuilder builder)
//    {
//        builder.Services.AddResponseCompression(options =>
//        {
//            options.EnableForHttps = true;
//            options.Providers.Add<BrotliCompressionProvider>();
//            options.Providers.Add<GzipCompressionProvider>();
//        });
//    }

//    public void Configure(WebApplication app)
//    {
//        app.UseResponseCompression();
//    }
//}
