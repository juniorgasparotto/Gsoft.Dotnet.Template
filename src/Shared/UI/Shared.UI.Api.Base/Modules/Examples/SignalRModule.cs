//using Microsoft.AspNetCore.Builder;
//using Microsoft.Extensions.DependencyInjection;

///// <summary>
///// Módulo para SignalR (WebSockets e real-time).
///// 
///// Exemplo:
///// 1. Crie um Hub: public class ChatHub : Hub { ... }
///// 2. Mapeie: app.MapHub<ChatHub>("/chatHub");
///// </summary>
//public class SignalRModule
//{
//    public int Order => 45;

//    public void ConfigureServices(WebApplicationBuilder builder)
//    {
//        builder.Services.AddSignalR();
//    }

//    public void Configure(WebApplication app)
//    {
//        // Mapeie seus hubs aqui
//        // app.MapHub<ChatHub>("/chatHub");
//    }
//}
