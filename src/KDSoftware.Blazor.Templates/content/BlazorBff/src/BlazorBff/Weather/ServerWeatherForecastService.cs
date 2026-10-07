namespace BlazorBff.Weather;

/// <summary>
/// Server-side implementation used by static SSR, prerendering and Interactive Server components.
/// It is also exposed to WebAssembly components through the <c>/api/weather</c> BFF endpoint.
/// </summary>
internal sealed class ServerWeatherForecastService : IWeatherForecastService
{
    private static readonly string[] Summaries =
        ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

    public async Task<WeatherForecast[]> GetForecastsAsync(CancellationToken cancellationToken = default)
    {
        // Simulates a slow data source to demonstrate streaming rendering.
        await Task.Delay(500, cancellationToken);

        DateOnly startDate = DateOnly.FromDateTime(DateTime.Now);
        return
        [
            .. Enumerable.Range(1, 5).Select(index => new WeatherForecast(
                startDate.AddDays(index),
                Random.Shared.Next(-20, 55),
                Summaries[Random.Shared.Next(Summaries.Length)]))
        ];
    }
}