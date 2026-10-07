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

### Blazor BFF Web App (`kd-blazor-bff`)

A Blazor Web App solution (`MyApplication` server + `MyApplication.Client` WebAssembly project) where the
ASP.NET Core server acts as a Backend-for-Frontend:

- pages are statically server-rendered by default for the fastest first paint; individual pages and components opt in
  to `InteractiveServer`, `InteractiveWebAssembly` or `InteractiveAuto`;
- optional OpenID Connect sign-in handled entirely on the server with a secure cookie session; tokens never reach the browser;
- `/api` endpoints for WebAssembly components, protected against CSRF, and a YARP reverse proxy that forwards
  configured routes to downstream APIs with the user's access token.

```bash
dotnet new kd-blazor-bff -n MyApplication
dotnet new kd-blazor-bff -n MyApplication --auth none
```

| Option | Description |
| --- | --- |
| `-au`, `--auth <oidc\|none>` | Authentication mode. Default: `oidc`. |
| `--http-port`, `--https-port` | Ports used in `launchSettings.json`. Generated when omitted. |
| `--no-restore` | Skips the automatic restore after the solution is created. |

With `--auth oidc` the Development configuration points at the public
[Duende IdentityServer demo](https://demo.duendesoftware.com) (user `bob`, password `bob`) so the app works out of the box.
The generated `README.md` explains how to configure your own identity provider.

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
dotnet new install ./src/KDSoftware.Blazor.Templates/content/BlazorBff --force
```

Build, test and pack from the repository root:

```bash
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet pack src/KDSoftware.Blazor.Templates --no-restore -o artifacts
```

The tests compare the generated output with the snapshots in `tests/KDSoftware.Blazor.Templates.Tests/Snapshots`
and build every template variant. Tests that instantiate and build the template are marked
`Category=Integration` and need access to NuGet; exclude them with `--filter "Category!=Integration"`.

When a template change is intentional, the failing snapshot test writes a `*.received` folder next to the
`*.verified` folder. Review the differences and copy the received files over the verified ones.

## License

See [LICENSE](LICENSE) for details.
