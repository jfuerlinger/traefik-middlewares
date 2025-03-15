using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Traefik.Middelware.Api.Services
{
    public class GeoFilterService(
        IHttpClientFactory httpClientFactory,
        HybridCache cache,
        ILogger<GeoFilterService> logger) : IHealthCheck
    {
        private const string HEALTH_CHECK_IP = "8.8.8.8";

        public async Task<bool> IsAllowedAsync(
            string ip,
            IEnumerable<string> allowedCountries,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(ip))
            {
                throw new ArgumentNullException(nameof(ip));
            }

            var country = await cache.GetOrCreateAsync(
                ip,
                async token => await GetCountryCodeAsync(ip, token),
                cancellationToken: cancellationToken);

            var isAllowed = allowedCountries.Contains(country);

            logger.LogInformation($"IP '{ip}' from '{country}' is {(isAllowed ? "allowed" : "not allowed")}");

            if (!isAllowed)
            {
                logger.LogWarning($"Illegal Access from '{ip}' ({country}) detected!");
            }

            return isAllowed;
        }

        private async Task<string> GetCountryCodeAsync(
            string ip,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(ip))
            {
                throw new ArgumentNullException(nameof(ip));
            }

            bool isLoggingForRequestEnabled = !ip.Equals("8.8.8.8");

            var httpClient = httpClientFactory.CreateClient();

            try
            {
                if (isLoggingForRequestEnabled)
                {
                    logger.LogInformation($"Fetching country info for ip '{ip}' ...");
                }

                var response = await httpClient.GetFromJsonAsync<IPApiResponse>($"http://ip-api.com/json/{ip}",
                    cancellationToken);

                var country = response?.CountryCode ?? throw new InvalidOperationException($"Error at country lookup!\n{response}");
                if (isLoggingForRequestEnabled)
                {
                    logger.LogInformation($"  -> '{country}'");
                }
                return country;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error at country lookup!");
                throw new InvalidOperationException("Error at country lookup!", ex);
            }
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await GetCountryCodeAsync(HEALTH_CHECK_IP, cancellationToken);
                return HealthCheckResult.Healthy("All good");
            }
            catch
            {
                return HealthCheckResult.Unhealthy("Error at country lookup!");
            }
        }

        record IPApiResponse(string Status, string CountryCode, string Country);
    }
}
