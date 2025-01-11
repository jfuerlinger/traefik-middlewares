using Microsoft.AspNetCore.Mvc;
using Traefik.Middelware.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddHttpClient();

builder.Services.AddScoped<GeoFilterService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();


app.MapGet("/geo-filter", (
    [FromQuery] string ip,
    [FromQuery] string allowedCountries,
    GeoFilterService geoFilterService) =>
{
    var countries = allowedCountries
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Distinct()
                        .Select(entry => entry.ToUpper())
                        .Order();

    return geoFilterService.IsAllowedAsync(ip, countries);
})
.WithName("geo-filter");

app.Run();

