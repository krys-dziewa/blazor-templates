using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);

#if (UseOidc)
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();

#endif
// Same-origin requests to the BFF automatically include the session cookie.
builder.Services.AddScoped(_ => new HttpClient(new CsrfHeaderHandler(new HttpClientHandler()))
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
});

await builder.Build().RunAsync();