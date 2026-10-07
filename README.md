# KDSoftware.Blazor.Templates

A collection of opinionated Blazor project templates for building modern .NET applications.

The package includes templates for common application architectures, including:

- Blazor Web App with **Interactive Auto** render mode
- **Backend-for-Frontend (BFF)** architecture
- **OpenID Connect (OIDC)** authentication
- Sensible defaults for modern .NET and Blazor applications

## Installation

Install the templates from NuGet:

```bash
dotnet new install KDSoftware.Blazor.Templates
```

List the available templates:

```bash
dotnet new list KDSoftware
```

Create a project using one of the installed templates:

```bash
dotnet new <template-name> -n MyApplication
```

## Templates

### KDSoftware Blazor App (`kdsoftware-blazor-app`)

Adds two projects to an **existing** solution: `MyApplication` (ASP.NET Core server) and `MyApplication.Client`
(WebAssembly). The server acts as a Backend-for-Frontend:

- pages are statically server-rendered by default for the fastest first paint; individual pages and components opt in
  to `InteractiveServer`, `InteractiveWebAssembly` or `InteractiveAuto`;
- optional OpenID Connect sign-in handled entirely on the server with a secure cookie session; access tokens are kept
  and refreshed on the server and never reach the browser;
- endpoints under `/api` require an `X-CSRF` header, which the WebAssembly `HttpClient` sends automatically.

Run the template from the folder that should contain the projects (for example `src`). The projects are added to the
nearest `.sln`/`.slnx` file:

```bash
cd src
dotnet new kdsoftware-blazor-app -n MyApplication
dotnet new kdsoftware-blazor-app -n MyApplication --auth none --central-package-management false
```

| Option | Description |
| --- | --- |
| `-au`, `--auth <oidc\|none>` | Authentication mode. Default: `oidc`. |
| `--session-store <cookie\|redis>` | Where the OIDC session (claims and tokens) is stored: in the encrypted cookie or in Valkey/Redis. Only with `--auth oidc`. Default: `cookie`. |
| `--central-package-management <true\|false>` | Whether the solution manages package versions in `Directory.Packages.props`. Default: `true`. |
| `--http-port`, `--https-port` | Ports used in `launchSettings.json`. Generated when omitted. |
| `--no-restore` | Skips the automatic restore after the projects are created. |

With central package management, `Directory.Packages.props` must define these packages (the template prints the
list after creation):

```xml
<PackageVersion Include="Microsoft.AspNetCore.Components.WebAssembly" Version="10.0.12" />
<PackageVersion Include="Microsoft.AspNetCore.Components.WebAssembly.Server" Version="10.0.12" />
<PackageVersion Include="Microsoft.Web.LibraryManager.Build" Version="3.0.114" />
<!-- --auth oidc only -->
<PackageVersion Include="Microsoft.AspNetCore.Authentication.OpenIdConnect" Version="10.0.12" />
<PackageVersion Include="Microsoft.AspNetCore.Components.WebAssembly.Authentication" Version="10.0.12" />
<!-- --session-store redis only -->
<PackageVersion Include="Microsoft.AspNetCore.DataProtection.StackExchangeRedis" Version="10.0.12" />
<PackageVersion Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="10.0.12" />
```

**Client libraries.** Bootstrap is declared in `libman.json` and restored into `wwwroot/lib` by
[LibMan](https://learn.microsoft.com/aspnet/core/client-side/libman/) during the build. Consider adding `wwwroot/lib/`
to your `.gitignore`.

**Authentication.** With `--auth oidc` the Development configuration points at the public
[Duende IdentityServer demo](https://demo.duendesoftware.com) (user `bob`, password `bob`) so the app works out of the box.
Configure your identity provider in the `Authentication:Oidc` section (`Authority`, `ClientId`, `ClientSecret`,
`Scopes`), keep the client secret in user secrets or a secret store, and register `https://<host>/signin-oidc` and
`https://<host>/signout-callback-oidc` as redirect URIs. The configuration is validated at startup.

**Session store.** By default the whole session (claims plus access, refresh and id tokens) lives in the encrypted
session cookie. With `--session-store redis` the cookie carries only an opaque session key and the session ticket is
stored, encrypted with ASP.NET Core Data Protection, in [Valkey](https://valkey.io) or Redis. This keeps the cookie
small, lets you revoke sessions on the server (signing out deletes the entry) and, because the Data Protection keys
are stored in the same server, lets every instance read the session cookie. Configure the server with the
`ConnectionStrings:SessionStore` setting (a [StackExchange.Redis connection string](https://stackexchange.github.io/StackExchange.Redis/Configuration),
for example `valkey:6379,password=...`); the app refuses to start without it. Development uses `localhost:6379`:

```bash
podman run -d -p 6379:6379 docker.io/valkey/valkey
```

**Containers.** The server project contains a `Containerfile`. Build it with the solution root as context so that
solution-level files such as `Directory.Packages.props` are available:

```bash
podman build -f src/MyApplication/Containerfile --build-arg PROJECT_PATH=src/MyApplication/MyApplication.csproj -t myapplication .
```

The container listens on HTTP port 8080. Behind a TLS-terminating reverse proxy, set
`ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` so that the app sees the original scheme, which OIDC redirects and the
`__Host-` session cookie require.

## Updating

```bash
dotnet new update
```

## Uninstalling

```bash
dotnet new uninstall KDSoftware.Blazor.Templates
```

## Development

The template sources live in `src/KDSoftware.Blazor.Templates/content`. Install a template directly from the
source folder to try local changes:

```bash
dotnet new install ./src/KDSoftware.Blazor.Templates/content/BlazorApp --force
```

Build, test and pack from the repository root:

```bash
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet pack src/KDSoftware.Blazor.Templates --no-restore -o artifacts
```

The tests compare the generated output with the snapshots in `tests/KDSoftware.Blazor.Templates.Tests/Snapshots`
and add every template variant to a new solution and build it. Tests that instantiate and build the template are marked
`Category=Integration` and need access to NuGet; exclude them with `--filter "Category!=Integration"`.

When a template change is intentional, the failing snapshot test writes a `*.received` folder next to the
`*.verified` folder. Review the differences and copy the received files over the verified ones.

## License

See [LICENSE](LICENSE) for details.
