namespace BlazorBff.Client.Weather;

/// <summary>
/// Abstraction that lets the same component run in every render mode: the server registers an
/// implementation that reads the data directly, WebAssembly registers one that calls the BFF API.
/// </summary>
public interface IWeatherForecastService
{
    Task<WeatherForecast[]> GetForecastsAsync(CancellationToken cancellationToken = default);
}