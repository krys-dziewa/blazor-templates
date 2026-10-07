#if (UseOidc)
using KDSoftware.BlazorApp.Authentication;
#endif
using KDSoftware.BlazorApp.Components;

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
    .AddAdditionalAssemblies(typeof(KDSoftware.BlazorApp.Client._Imports).Assembly);

#if (UseOidc)
app.MapLoginAndLogout();

#endif
// Map endpoints called by WebAssembly components under BffApiApplicationBuilderExtensions.ApiPathPrefix (/api);
// they are protected by UseBffApiProtection.

app.Run();