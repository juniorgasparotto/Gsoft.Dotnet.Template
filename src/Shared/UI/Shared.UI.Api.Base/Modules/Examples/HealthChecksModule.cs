//using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.Diagnostics.HealthChecks;
//using Microsoft.Extensions.Diagnostics.HealthChecks;
//using Microsoft.Extensions.DependencyInjection;

///// <summary>
///// Módulo para Health Checks.
///// Endpoints: /health, /health/ready, /health/live
///// </summary>
//public class HealthChecksModule
//{
//    public int Order => 20;

//    public void ConfigureServices(WebApplicationBuilder builder)
//    {
//        builder.Services.AddHealthChecks()
//            .AddCheck("self", () => HealthCheckResult.Healthy());
//    }

//    public void Configure(WebApplication app)
//    {
//        app.MapHealthChecks("/health");
//        app.MapHealthChecks("/health/ready", new HealthCheckOptions
//        {
//            Predicate = check => check.Tags.Contains("ready")
//        });
//        app.MapHealthChecks("/health/live", new HealthCheckOptions
//        {
//            Predicate = _ => false
//        });
//    }
//}
