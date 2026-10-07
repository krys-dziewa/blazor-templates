#if (UseOidc)
using BlazorBff.Authentication;
#endif
using BlazorBff.Components;
using BlazorBff.Weather;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
#if (UseOidc)
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization();
#else
    .AddInteractiveWebAssemblyComponents();
#endif

#if (UseOidc)
builder.Services.AddOidcBffAuthentication();
#endif
builder.Services.AddBffReverseProxy(builder.Configuration);
builder.Services.AddScoped<IWeatherForecastService, ServerWeatherForecastService>();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseBffApiProtection();
#if (UseOidc)
app.UseAuthentication();
app.UseAuthorization();
#endif
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(BlazorBff.Client._Imports).Assembly);

#if (UseOidc)
app.MapLoginAndLogout();
#endif
app.MapWeatherApi();
app.MapReverseProxy();

app.Run();