//using Microsoft.AspNetCore.Builder;
//using Microsoft.Extensions.Hosting;

///// <summary>
///// Módulo para tratamento de exceções.
///// Dev: página detalhada de erros
///// Prod: handler customizado + HSTS
///// </summary>
//public class ExceptionHandlingModule
//{
//    public int Order => 1;

//    public void Configure(WebApplication app)
//    {
//        if (app.Environment.IsDevelopment())
//        {
//            app.UseDeveloperExceptionPage();
//        }
//        else
//        {
//            app.UseExceptionHandler("/error");
//            app.UseHsts();
//        }
//    }
//}
