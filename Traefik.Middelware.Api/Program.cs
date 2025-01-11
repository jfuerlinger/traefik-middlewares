using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Traefik.Middelware.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddHttpClient();
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


app.MapGet("/geo-filter", async (
    [FromQuery] string ip,
    [FromQuery] string allowedCountries,
    GeoFilterService geoFilterService) =>
{
    var countries = allowedCountries
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Distinct()
                        .Select(entry => entry.ToUpper())
                        .Order();

    var isAllowed = await geoFilterService.IsAllowedAsync(ip, countries);

    return isAllowed
        ? Results.Ok("allowed")
        : Results.StatusCode(StatusCodes.Status403Forbidden);
})
.WithName("geo-filter");

app.Run();

