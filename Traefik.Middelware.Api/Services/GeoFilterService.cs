namespace Traefik.Middelware.Api.Services
{
    public class GeoFilterService(
        IHttpClientFactory httpClientFactory,
        ILogger<GeoFilterService> logger)
    {

        public async Task<bool> IsAllowedAsync(
            string ip,
            IEnumerable<string> allowedCountries)
        {
            var country = await GetCountryCodeAsync(ip);
            var isAllowed = allowedCountries.Contains(country);

            logger.LogInformation($"IP '{ip}' from '{country}' is {(isAllowed ? "allowed" : "not allowed")}");

            return isAllowed;
        }

        private async Task<string> GetCountryCodeAsync(string ip)
        {
            var httpClient = httpClientFactory.CreateClient();

            try
            {
                var response = await httpClient.GetFromJsonAsync<IPApiResponse>($"http://ip-api.com/json/{ip}");
                return response?.CountryCode ?? throw new InvalidOperationException("Error at country lookup!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error at country lookup!");
                throw new InvalidOperationException("Error at country lookup!", ex);
            }
        }

        record IPApiResponse(string Status, string CountryCode, string Country);
    }
}
