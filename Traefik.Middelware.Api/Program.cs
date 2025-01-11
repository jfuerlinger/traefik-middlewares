using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Traefik.Middelware.Api.Caching;
using Traefik.Middelware.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHttpClient();

#pragma warning disable EXTEXP0018 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
builder.Services.AddHybridCache();
#pragma warning restore EXTEXP0018 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

builder.Services.AddOutputCache(options =>
{
    options.DefaultExpirationTimeSpan = TimeSpan.FromSeconds(60);
    options.AddPolicy("GeoFilterPolicy", GeoFilterPolicy.Instance);
});

builder.Services.AddHealthChecks()
                    .AddCheck<GeoFilterService>("IP-API Check");

builder.Services.AddScoped<GeoFilterService>();

var app = builder.Build();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description
            }),
            duration = report.TotalDuration
        });

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(result);
    }
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


app.UseHttpsRedirection();
app.UseOutputCache();

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

    logger.LogDebug($"Checking IP '{ip}' against '{allowedCountries}' ...");

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

app.Run();

