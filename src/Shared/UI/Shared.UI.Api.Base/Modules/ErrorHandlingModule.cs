namespace Shared.UI.Api.Base.Modules;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Infra.Module.Base;
using Shared.Infra.Module.Base.Attributes;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

/// <summary>
/// Opções de configuração para o ErrorHandlingModule.
/// </summary>
[ModuleOptionAttribute("ErrorHandlingModule")]
public class ErrorHandlingModuleOptions
{
    public GlobalExceptionHandlerOptions? GlobalExceptionHandler { get; set; }
    public ProblemDetailsOptions? ProblemDetails { get; set; }
    public ModelStateValidationOptions? ModelStateValidation { get; set; }
}

public class GlobalExceptionHandlerOptions
{
    public bool? Enabled { get; set; }
    public bool? IncludeExceptionDetails { get; set; }
    public bool? IncludeStackTrace { get; set; }
    public string? CustomErrorPath { get; set; }
}

public class ProblemDetailsOptions
{
    public bool? Enabled { get; set; }
    public bool? IncludeTraceId { get; set; }
    public Dictionary<int, string>? CustomErrorTitles { get; set; }
    public Dictionary<int, string>? CustomErrorLinks { get; set; }
}

public class ModelStateValidationOptions
{
    public bool? Enabled { get; set; }
    public bool? SuppressModelStateInvalidFilter { get; set; }
    public bool? SuppressMapClientErrors { get; set; }
}

/// <summary>
/// Módulo que implementa tratamento de erros com Problem Details (RFC 7807).
/// </summary>
[ModuleAttribute(Title = "Error Handling Module", Description = "Implements error handling with Problem Details (RFC 7807), including global exception handler, ModelState validation and standardized error responses.")]
public class ErrorHandlingModule : IWebModule
{
    private readonly ErrorHandlingModuleOptions _options;

    public ErrorHandlingModule(IOptions<ErrorHandlingModuleOptions> options)
    {
        _options = options.Value;
    }

    public void AddConfigurations(WebModuleConfigurationOptions configurationOptions)
    {
    }

    public void ConfigureHost(WebApplicationBuilder builder)
    {
        // Configurar ModelState validation
        var modelState = _options.ModelStateValidation;
        if (modelState != null && modelState.Enabled != false)
        {
            builder.Services.Configure<ApiBehaviorOptions>(options =>
            {
                if (modelState.SuppressModelStateInvalidFilter == true)
                {
                    options.SuppressModelStateInvalidFilter = true;
                }

                if (modelState.SuppressMapClientErrors == true)
                {
                    options.SuppressMapClientErrors = true;
                }

                // Configurar títulos e links customizados para erros comuns
                if (_options.ProblemDetails?.CustomErrorTitles != null)
                {
                    foreach (var kvp in _options.ProblemDetails.CustomErrorTitles)
                    {
                        if (options.ClientErrorMapping.ContainsKey(kvp.Key))
                        {
                            options.ClientErrorMapping[kvp.Key].Title = kvp.Value ?? "";
                        }
                    }
                }

                if (_options.ProblemDetails?.CustomErrorLinks != null)
                {
                    foreach (var kvp in _options.ProblemDetails.CustomErrorLinks)
                    {
                        if (options.ClientErrorMapping.ContainsKey(kvp.Key))
                        {
                            options.ClientErrorMapping[kvp.Key].Link = kvp.Value ?? "";
                        }
                    }
                }
            });

            // Adicionar filtro para ModelState inválido
            builder.Services.Configure<MvcOptions>(options =>
            {
                options.Filters.Add<ModelStateInvalidFilter>();
            });
        }
    }

    public void Configure(WebApplication app)
    {
        // Global Exception Handler
        if (_options.GlobalExceptionHandler?.Enabled != false)
        {
            var includeDetails = _options.GlobalExceptionHandler?.IncludeExceptionDetails == true;
            var includeStackTrace = _options.GlobalExceptionHandler?.IncludeStackTrace == true;
            var customErrorPath = _options.GlobalExceptionHandler?.CustomErrorPath;

            if (!string.IsNullOrEmpty(customErrorPath))
            {
                app.UseExceptionHandler(customErrorPath);
            }
            else
            {
                app.UseExceptionHandler(new ExceptionHandlerOptions
                {
                    ExceptionHandler = async context =>
                    {
                        var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
                        if (exceptionHandlerFeature != null)
                        {
                            var exception = exceptionHandlerFeature.Error;
                            var logger = context.RequestServices.GetRequiredService<ILogger<ErrorHandlingModule>>();
                            
                            logger.LogError(exception, "Unhandled exception occurred");

                            var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
                            {
                                Status = (int)HttpStatusCode.InternalServerError,
                                Title = "An error occurred while processing your request.",
                                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                                Instance = context.Request.Path
                            };

                            if (_options.ProblemDetails?.IncludeTraceId != false)
                            {
                                problemDetails.Extensions["traceId"] = context.TraceIdentifier;
                            }

                            if (includeDetails || app.Environment.IsDevelopment())
                            {
                                problemDetails.Detail = exception.Message;
                                
                                if (includeStackTrace || app.Environment.IsDevelopment())
                                {
                                    problemDetails.Extensions["stackTrace"] = exception.StackTrace;
                                }

                                if (exception.InnerException != null)
                                {
                                    problemDetails.Extensions["innerException"] = new
                                    {
                                        message = exception.InnerException.Message,
                                        type = exception.InnerException.GetType().Name
                                    };
                                }
                            }

                            context.Response.StatusCode = problemDetails.Status.Value;
                            context.Response.ContentType = "application/problem+json";

                            var jsonOptions = new JsonSerializerOptions
                            {
                                WriteIndented = app.Environment.IsDevelopment()
                            };

                            var json = JsonSerializer.Serialize(problemDetails, jsonOptions);
                            await context.Response.WriteAsync(json);
                        }
                    }
                });
            }
        }

        // Status Code Pages para erros 4xx e 5xx
        if (_options.ProblemDetails?.Enabled != false)
        {
            app.UseStatusCodePages(async context =>
            {
                var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Status = context.HttpContext.Response.StatusCode,
                    Title = GetErrorTitle(context.HttpContext.Response.StatusCode),
                    Type = GetErrorType(context.HttpContext.Response.StatusCode),
                    Instance = context.HttpContext.Request.Path
                };

                if (_options.ProblemDetails?.IncludeTraceId != false)
                {
                    var activity = Activity.Current;
                    if (activity != null && activity.TraceId != default)
                    {
                        problemDetails.Extensions["traceId"] = activity.TraceId.ToString();
                    }
                    else
                    {
                        problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
                    }
                }

                context.HttpContext.Response.ContentType = "application/problem+json";

                var jsonOptions = new JsonSerializerOptions
                {
                    WriteIndented = app.Environment.IsDevelopment()
                };

                var json = JsonSerializer.Serialize(problemDetails, jsonOptions);
                await context.HttpContext.Response.WriteAsync(json);
            });
        }
    }

    private static string GetErrorTitle(int statusCode)
    {
        return statusCode switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            405 => "Method Not Allowed",
            406 => "Not Acceptable",
            409 => "Conflict",
            422 => "Unprocessable Entity",
            500 => "Internal Server Error",
            501 => "Not Implemented",
            502 => "Bad Gateway",
            503 => "Service Unavailable",
            _ => "An error occurred"
        };
    }

    private static string GetErrorType(int statusCode)
    {
        return statusCode switch
        {
            >= 400 and < 500 => "https://tools.ietf.org/html/rfc7231#section-6.5",
            >= 500 => "https://tools.ietf.org/html/rfc7231#section-6.6",
            _ => "https://tools.ietf.org/html/rfc7231"
        };
    }
}

/// <summary>
/// Filtro para tratar ModelState inválido e retornar Problem Details.
/// </summary>
public class ModelStateInvalidFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var problemDetails = new Microsoft.AspNetCore.Mvc.ValidationProblemDetails(context.ModelState)
            {
                Status = (int)HttpStatusCode.BadRequest,
                Title = "One or more validation errors occurred.",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Instance = context.HttpContext.Request.Path
            };

            var activity = Activity.Current;
            if (activity != null && activity.TraceId != default)
            {
                problemDetails.Extensions["traceId"] = activity.TraceId.ToString();
            }
            else
            {
                problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
            }

            context.Result = new BadRequestObjectResult(problemDetails)
            {
                ContentTypes = { "application/problem+json" }
            };
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // Não precisa fazer nada após a execução
    }
}
