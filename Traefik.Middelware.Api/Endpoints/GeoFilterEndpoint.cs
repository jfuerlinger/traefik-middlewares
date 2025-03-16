using Microsoft.AspNetCore.Mvc;
using Traefik.Middelware.Api.Services;

namespace Traefik.Middelware.Api.Endpoints;

public static class GeoFilterEndpoint
{
    public static IEndpointRouteBuilder MapGeoFilterEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/geo-filter", async (
            [FromQuery(Name = "ip")] string? ipFromQueryString,
            [FromQuery] string allowedCountries,
            [FromHeader(Name = "X-Forwarded-For")] string? ipFromHeader,
            HttpContext context,
            GeoFilterService geoFilterService,
            ILogger<Program> logger,
            CancellationToken cancellationToken) =>
        {
            var ip = ipFromHeader
                        ?? ipFromQueryString
                        ?? context?.Connection?.RemoteIpAddress?.ToString()
                        ?? throw new InvalidOperationException("Can't resolve ip to check!");

            logger.LogDebug("Checking IP '{ip}' against '{allowedCountries}' ...", ip, allowedCountries);

            if (ip.StartsWith("192.168.") || ip.StartsWith("172."))
            {
                logger.LogInformation("Allowed - internal ip ('{ip}')", ip);
                return Results.Ok("allowed - internal ip");
            }

            var countries = allowedCountries
                                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Distinct()
                                .Select(entry => entry.ToUpper())
                                .Order();

            var isAllowed = await geoFilterService.IsAllowedAsync(
                ip, countries,
                cancellationToken);

            return isAllowed
                ? Results.Ok("allowed")
                : Results.StatusCode(StatusCodes.Status403Forbidden);

        })
        .WithName("geo-filter")
        .CacheOutput("GeoFilterPolicy");

        return app;
    }
}
