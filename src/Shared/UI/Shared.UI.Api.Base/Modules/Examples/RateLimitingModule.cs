//using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.RateLimiting;
//using System.Threading.RateLimiting;

///// <summary>
///// Módulo para Rate Limiting (100 requisições por minuto).
///// Use: [EnableRateLimiting("fixed")] em controllers/endpoints
///// </summary>
//public class RateLimitingModule
//{
//    public int Order => 30;

//    public void ConfigureServices(WebApplicationBuilder builder)
//    {
//        builder.Services.AddRateLimiter(options =>
//        {
//            options.AddFixedWindowLimiter("fixed", opt =>
//            {
//                opt.PermitLimit = 100;
//                opt.Window = TimeSpan.FromMinutes(1);
//                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
//                opt.QueueLimit = 5;
//            });
//        });
//    }

//    public void Configure(WebApplication app)
//    {
//        app.UseRateLimiter();
//    }
//}
