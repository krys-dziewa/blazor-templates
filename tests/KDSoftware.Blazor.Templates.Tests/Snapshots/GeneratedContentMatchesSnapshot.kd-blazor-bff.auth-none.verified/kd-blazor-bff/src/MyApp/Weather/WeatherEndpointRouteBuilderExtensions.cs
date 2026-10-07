namespace MyApp.Weather;

internal static class WeatherEndpointRouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapWeatherApi(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet(
            $"{BffApiApplicationBuilderExtensions.ApiPathPrefix}/weather",
            static (IWeatherForecastService weatherForecasts, CancellationToken cancellationToken) =>
                weatherForecasts.GetForecastsAsync(cancellationToken));
    }
}