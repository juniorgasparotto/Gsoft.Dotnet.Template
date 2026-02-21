using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Shared.UI.Api.Base.Controllers.Diagnostics;

[ApiController]
[Route("diagnostics/endpoints")]
public class EndpointsController : ControllerBase
{
    private readonly ILogger<EndpointsController> _logger;
    private readonly EndpointDataSource _endpointDataSource;

    public EndpointsController(ILogger<EndpointsController> logger, EndpointDataSource endpointDataSource)
    {
        _logger = logger;
        _endpointDataSource = endpointDataSource;
    }

    /// <summary>
    /// Lista todos os endpoints registrados na aplicação
    /// GET /diagnostics/endpoints
    /// </summary>
    [HttpGet]
    public IActionResult GetAllEndpoints()
    {
        var endpoints = _endpointDataSource.Endpoints
            .Select(endpoint =>
            {
                var routeEndpoint = endpoint as RouteEndpoint;
                var httpMethods = endpoint.Metadata
                    .OfType<Microsoft.AspNetCore.Routing.HttpMethodMetadata>()
                    .FirstOrDefault()
                    ?.HttpMethods ?? new[] { "ANY" };

                return new
                {
                    RoutePattern = routeEndpoint?.RoutePattern?.RawText ?? endpoint.DisplayName,
                    HttpMethods = httpMethods,
                    DisplayName = endpoint.DisplayName,
                    Order = routeEndpoint?.Order ?? 0,
                    Metadata = endpoint.Metadata
                        .Where(m => m != null)
                        .Select(m => m.GetType().Name)
                        .ToList()
                };
            })
            .OrderBy(e => e.RoutePattern)
            .ToList();

        _logger.LogInformation("Endpoints diagnostics accessed - Total: {Count}", endpoints.Count);

        return Ok(new
        {
            TotalEndpoints = endpoints.Count,
            Endpoints = endpoints
        });
    }
}
