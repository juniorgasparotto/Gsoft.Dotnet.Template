//using Microsoft.AspNetCore.Builder;
//using Microsoft.Extensions.Configuration;
//// using Microsoft.EntityFrameworkCore;

///// <summary>
///// Módulo para Entity Framework Core.
///// 
///// REQUER um dos seguintes:
///// - SQL Server: dotnet add package Microsoft.EntityFrameworkCore.SqlServer
///// - PostgreSQL: dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
///// - MySQL: dotnet add package Pomelo.EntityFrameworkCore.MySql
///// - SQLite: dotnet add package Microsoft.EntityFrameworkCore.Sqlite
///// 
///// Configure no appsettings.json:
///// {
/////   "ConnectionStrings": {
/////     "DefaultConnection": "Server=...;Database=...;..."
/////   }
///// }
///// </summary>
//public class DatabaseModule
//{
//    public int Order => 5;

//    public void ConfigureServices(WebApplicationBuilder builder, IConfiguration configuration)
//    {
//        // Descomente o provider desejado:
        
//        // SQL Server
//        // builder.Services.AddDbContext<AppDbContext>(options =>
//        //     options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
        
//        // PostgreSQL
//        // builder.Services.AddDbContext<AppDbContext>(options =>
//        //     options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        
//        // MySQL
//        // builder.Services.AddDbContext<AppDbContext>(options =>
//        //     options.UseMySql(
//        //         configuration.GetConnectionString("DefaultConnection"),
//        //         ServerVersion.AutoDetect(configuration.GetConnectionString("DefaultConnection"))));
        
//        // SQLite
//        // builder.Services.AddDbContext<AppDbContext>(options =>
//        //     options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));
//    }
//}
