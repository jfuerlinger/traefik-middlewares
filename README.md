# Traefik.Middleware

## Geo-Blocking

Simple Traefik middleware to provide geo-blocking based on the caller's IP.

### Example

Use the following URL to test the geo-blocking middleware:

[http://<server>:<port>/geo-filter?ip=87.248.119.251&allowedCountries=at,de,ie](https://localhost:32773/geo-filter?ip=87.248.119.251&allowedCountries=at,de,ie)

### Query Parameters

- `ip`: The IP address of the caller to be checked.
- `allowedCountries`: A comma-separated list of country codes (ISO 3166-1 Alpha-2) that are allowed access.