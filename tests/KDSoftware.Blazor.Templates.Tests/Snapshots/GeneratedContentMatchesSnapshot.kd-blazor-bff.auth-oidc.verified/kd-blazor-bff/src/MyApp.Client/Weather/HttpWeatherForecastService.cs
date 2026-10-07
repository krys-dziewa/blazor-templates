using System.Net.Http.Json;

namespace MyApp.Client.Weather;

internal sealed class HttpWeatherForecastService(HttpClient httpClient) : IWeatherForecastService
{
    public async Task<WeatherForecast[]> GetForecastsAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<WeatherForecast[]>("api/weather", cancellationToken) ?? [];
    }
}