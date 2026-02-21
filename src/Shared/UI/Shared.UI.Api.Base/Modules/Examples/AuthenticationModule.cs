//using Microsoft.AspNetCore.Builder;
//using Microsoft.Extensions.Configuration;
//// using Microsoft.AspNetCore.Authentication.JwtBearer;
//// using Microsoft.IdentityModel.Tokens;
//// using System.Text;

///// <summary>
///// Módulo de autenticação JWT.
///// 
///// REQUER: dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
///// 
///// Configure no appsettings.json:
///// {
/////   "Jwt": {
/////     "Key": "sua-chave-secreta",
/////     "Issuer": "seu-emissor",
/////     "Audience": "sua-audiencia"
/////   }
///// }
///// </summary>
//public class AuthenticationModule
//{
//    public int Order => 10;

//    public void ConfigureServices(WebApplicationBuilder builder, IConfiguration configuration)
//    {
//        // Descomente após instalar o pacote Microsoft.AspNetCore.Authentication.JwtBearer
        
//        // builder.Services.AddAuthentication(options =>
//        // {
//        //     options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//        //     options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//        // })
//        // .AddJwtBearer(options =>
//        // {
//        //     options.TokenValidationParameters = new TokenValidationParameters
//        //     {
//        //         ValidateIssuer = true,
//        //         ValidateAudience = true,
//        //         ValidateLifetime = true,
//        //         ValidateIssuerSigningKey = true,
//        //         ValidIssuer = configuration["Jwt:Issuer"],
//        //         ValidAudience = configuration["Jwt:Audience"],
//        //         IssuerSigningKey = new SymmetricSecurityKey(
//        //             Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!))
//        //     };
//        // });
//        //
//        // builder.Services.AddAuthorization(options =>
//        // {
//        //     options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
//        // });
//    }

//    public void Configure(WebApplication app)
//    {
//        // app.UseAuthentication();
//        // app.UseAuthorization();
//    }
//}
