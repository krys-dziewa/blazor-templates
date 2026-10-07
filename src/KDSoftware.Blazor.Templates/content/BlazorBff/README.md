# BlazorBff

A Blazor Web App in which the ASP.NET Core server is a **Backend-for-Frontend (BFF)**:

<!--#if (UseOidc) -->
- it signs users in with **OpenID Connect** and keeps the session in a secure, HTTP-only cookie;
- access and refresh tokens stay on the server and are never sent to the browser;
<!--#endif -->
- it exposes the API surface used by WebAssembly components under `/api`;
- it forwards selected `/api/...` routes to downstream services with [YARP](https://microsoft.github.io/reverse-proxy/).

## Projects

| Project | Purpose |
| --- | --- |
| `src/BlazorBff` | Server / BFF host. Static SSR pages, Interactive Server components, API endpoints, reverse proxy. |
| `src/BlazorBff.Client` | Components that can run in WebAssembly (Interactive WebAssembly / Auto) and shared contracts. |

## Running

```bash
dotnet run --project src/BlazorBff --launch-profile https
```

<!--#if (UseOidc) -->
In Development the app is configured against the public [Duende IdentityServer demo](https://demo.duendesoftware.com)
(user `bob`, password `bob`), so sign-in and the downstream API call work out of the box.

## Configuring your identity provider

Configure the `Authentication:Oidc` section:

| Key | Description |
| --- | --- |
| `Authority` | Issuer URL of the identity provider. |
| `ClientId` | Client ID of this application. |
| `ClientSecret` | Client secret. Keep it out of source control (see below). |
| `Scopes` | Additional scopes, e.g. API scopes. `openid`, `profile` and `offline_access` are always requested. |

Register the redirect URIs `https://<host>/signin-oidc` and `https://<host>/signout-callback-oidc` with the identity provider.

Store the client secret with user secrets during development:

```bash
dotnet user-secrets set "Authentication:Oidc:ClientSecret" "<secret>" --project src/BlazorBff
```

and with environment variables (`Authentication__Oidc__ClientSecret`) or a secret store in deployed environments.
The configuration is validated at startup.

<!--#endif -->
## Render modes

There is no global render mode: every page is **static server-side rendering** unless it opts in. This gives the
fastest first paint and does not require a WebSocket connection or a WebAssembly download.

| Render mode | Where to put the component | Use it for |
| --- | --- | --- |
| Static SSR (+ `[StreamRendering]`) | `BlazorBff` | Content pages and forms. Slow data is streamed in after the first paint. |
| `InteractiveServer` | `BlazorBff` | Interactive UI that needs server-only resources. |
| `InteractiveAuto` / `InteractiveWebAssembly` | `BlazorBff.Client` | SPA-like UI that should keep working in the browser and offload the server. |

Components that can run in several render modes depend on an abstraction (see `IWeatherForecastService`): the server
registers an implementation that accesses the data directly, and WebAssembly registers one that calls the BFF API.
Use `[PersistentState]` so that data loaded during prerendering is not fetched again when the component becomes interactive.

## BFF API

- Every request under `/api` must carry the `X-CSRF` header. Browsers cannot send custom headers cross-origin without
  a CORS preflight, which protects the cookie-authenticated API against cross-site request forgery. The WebAssembly
  `HttpClient` adds the header through `CsrfHeaderHandler`.
- Add local endpoints for WebAssembly components next to `MapWeatherApi` in `Program.cs`.
- Add downstream APIs as routes and clusters in the `ReverseProxy` section of `appsettings.json`.
<!--#if (UseOidc) -->
  Proxied requests get the user's access token; the session cookie is never forwarded.
<!--#endif -->
