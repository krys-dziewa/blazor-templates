# MyApp

A Blazor Web App in which the ASP.NET Core server is a **Backend-for-Frontend (BFF)**:

- it exposes the API surface used by WebAssembly components under `/api`;
- it forwards selected `/api/...` routes to downstream services with [YARP](https://microsoft.github.io/reverse-proxy/).

## Projects

| Project | Purpose |
| --- | --- |
| `src/MyApp` | Server / BFF host. Static SSR pages, Interactive Server components, API endpoints, reverse proxy. |
| `src/MyApp.Client` | Components that can run in WebAssembly (Interactive WebAssembly / Auto) and shared contracts. |

## Running

```bash
dotnet run --project src/MyApp --launch-profile https
```

## Render modes

There is no global render mode: every page is **static server-side rendering** unless it opts in. This gives the
fastest first paint and does not require a WebSocket connection or a WebAssembly download.

| Render mode | Where to put the component | Use it for |
| --- | --- | --- |
| Static SSR (+ `[StreamRendering]`) | `MyApp` | Content pages and forms. Slow data is streamed in after the first paint. |
| `InteractiveServer` | `MyApp` | Interactive UI that needs server-only resources. |
| `InteractiveAuto` / `InteractiveWebAssembly` | `MyApp.Client` | SPA-like UI that should keep working in the browser and offload the server. |

Components that can run in several render modes depend on an abstraction (see `IWeatherForecastService`): the server
registers an implementation that accesses the data directly, and WebAssembly registers one that calls the BFF API.
Use `[PersistentState]` so that data loaded during prerendering is not fetched again when the component becomes interactive.

## BFF API

- Every request under `/api` must carry the `X-CSRF` header. Browsers cannot send custom headers cross-origin without
  a CORS preflight, which protects the cookie-authenticated API against cross-site request forgery. The WebAssembly
  `HttpClient` adds the header through `CsrfHeaderHandler`.
- Add local endpoints for WebAssembly components next to `MapWeatherApi` in `Program.cs`.
- Add downstream APIs as routes and clusters in the `ReverseProxy` section of `appsettings.json`.
