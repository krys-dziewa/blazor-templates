# AGENTS.md

This repository is a .NET 10 codebase.

This file contains instructions for AI coding agents and automated development tools working in this repository.

## Repository conventions

Repository-wide build configuration is defined in:

- `global.json`
- `Directory.Build.props`
- `Directory.Build.targets`
- `Directory.Packages.props`
- `.editorconfig`

Do not duplicate settings from these files into individual projects unless a project intentionally requires an override.

## .NET SDK

Use the SDK selected by `global.json`.

Before making changes, verify the environment with:

```bash
dotnet --info
dotnet --version
```

Do not change `global.json` merely because another SDK is installed locally.

The repository intentionally allows compatible .NET 10 SDK roll-forward according to the policy defined in `global.json`.

Do not upgrade the repository to a newer major .NET version unless explicitly requested.

## Restore

Restore dependencies from the repository root:

```bash
dotnet restore
```

If package lock files are used, CI-style validation should use:

```bash
dotnet restore --locked-mode
```

Do not edit generated NuGet lock files manually.

## Build

Build the complete repository from the repository root:

```bash
dotnet build --no-restore
```

A normal build is expected to complete without warnings.

Warnings are treated as errors unless a project explicitly documents an exception.

Do not disable warnings globally to make a build pass.

Do not add `NoWarn`, `WarningsNotAsErrors`, or analyzer suppressions unless the warning has been reviewed and the suppression is justified.

## Tests

Run all tests with:

```bash
dotnet test --no-build
```

When changing a specific component, run its relevant tests first, then run the complete test suite before considering the change complete.

Prefer adding or updating tests when behavior changes.

Bug fixes should include a regression test whenever practical.

Tests should verify externally observable behavior rather than internal implementation details.

## Formatting

Follow `.editorconfig`.

Use:

```bash
dotnet format
```

when formatting changes are required.

Avoid unrelated formatting changes.

Do not reformat entire files unless necessary.

Keep diffs focused on the requested change.

## Package management

Package versions are centrally managed in:

```text
Directory.Packages.props
```

Project files should normally contain:

```xml
<PackageReference Include="Package.Name" />
```

without a `Version` attribute.

Do not add package versions directly to individual `.csproj` files unless there is a documented reason.

Do not use `VersionOverride` unless explicitly required and permitted by repository configuration.

When adding a dependency:

1. Prefer packages maintained by Microsoft or established upstream projects.
2. Add the package version to `Directory.Packages.props`.
3. Add the corresponding `PackageReference` only to projects that need it.
4. Restore and build the repository.
5. Run relevant tests.
6. Review NuGet vulnerability/audit output.

Avoid introducing new dependencies when equivalent functionality already exists in the BCL or in dependencies already used by the repository.

## Project files

Keep `.csproj` files minimal.

Prefer SDK defaults and repository-wide configuration over project-local configuration.

Do not add properties such as these without a concrete reason:

```xml
<LangVersion>...</LangVersion>
<Nullable>...</Nullable>
<ImplicitUsings>...</ImplicitUsings>
<TreatWarningsAsErrors>...</TreatWarningsAsErrors>
```

These are expected to be managed centrally when applicable.

## C# conventions

Use modern C# appropriate for .NET 10.

Prefer clear, idiomatic C# over clever abstractions.

General guidelines:

- Enable and respect nullable reference type analysis.
- Avoid the null-forgiving operator (`!`) unless nullability is genuinely proven.
- Prefer immutable data where practical.
- Prefer `record` or `record struct` for value-like data when appropriate.
- Prefer pattern matching when it improves clarity.
- Prefer collection expressions when they improve readability.
- Prefer `async` APIs for I/O-bound work.
- Pass `CancellationToken` through asynchronous call chains where cancellation is meaningful.
- Avoid `.Result`, `.Wait()`, and other sync-over-async patterns.
- Dispose `IDisposable` and `IAsyncDisposable` resources correctly.
- Prefer dependency injection over service locator patterns.
- Keep public APIs intentional and minimal.
- Avoid unnecessary allocations in performance-sensitive code, but do not prematurely optimize ordinary application code.

## Async code

Async methods should normally end with `Async` unless they are implementations of established interfaces or framework conventions that use another name.

Prefer:

```csharp
await SomeOperationAsync(cancellationToken);
```

over blocking calls.

Library code should follow the repository's existing `ConfigureAwait` convention rather than introducing a new convention locally.

Do not add `Task.Run` merely to make synchronous I/O appear asynchronous.

## Cancellation

Public asynchronous operations that may perform meaningful work should generally accept a `CancellationToken`.

Propagate the token to downstream operations whenever supported.

Do not replace a caller-provided token with `CancellationToken.None`.

## Logging

Use structured logging.

Prefer:

```csharp
logger.LogInformation(
    "Processed order {OrderId} for customer {CustomerId}",
    orderId,
    customerId);
```

over string interpolation:

```csharp
logger.LogInformation(
    $"Processed order {orderId} for customer {customerId}");
```

Do not log secrets, credentials, access tokens, API keys, or sensitive personal data.

Use appropriate log levels and avoid noisy logging in hot paths.

## Exceptions

Use exceptions for exceptional conditions, not normal control flow.

Do not catch exceptions unless you can:

- handle them,
- add meaningful context,
- translate them at an architectural boundary, or
- perform required cleanup.

Do not silently swallow exceptions.

Preserve the original exception when wrapping:

```csharp
throw new SomeException("Useful context.", exception);
```

Avoid:

```csharp
throw exception;
```

Use:

```csharp
throw;
```

when rethrowing the current exception.

## Dependency injection

Register services through the existing composition root.

Match service lifetimes to their actual ownership and usage.

Be especially careful when introducing dependencies from singleton services to scoped services.

Avoid resolving services manually from `IServiceProvider` unless the architecture specifically requires it.

## Configuration

Use the .NET configuration and options patterns already established in the repository.

Prefer strongly typed options over repeatedly accessing configuration values by string key.

Validate configuration at startup when invalid configuration would prevent the application from operating correctly.

Never commit real secrets.

Use environment variables, user secrets, CI secret stores, or the deployment platform's secret-management mechanism.

## APIs

For HTTP APIs:

- preserve established route conventions,
- use appropriate HTTP status codes,
- validate input at the application boundary,
- avoid exposing internal exception details,
- keep request and response contracts stable unless a breaking change is intentional,
- update API tests when contracts change.

Do not silently introduce breaking API changes.

## Database changes

Follow the existing persistence conventions.

For Entity Framework Core repositories:

- keep migrations focused,
- review generated migrations before committing,
- avoid destructive schema changes unless explicitly required,
- do not modify an existing migration that may already have been deployed,
- add a new migration for subsequent schema changes.

Do not run destructive database commands against shared environments.

## Security

Treat all external input as untrusted.

Pay particular attention to:

- authentication,
- authorization,
- path handling,
- SQL/database queries,
- HTTP redirects,
- deserialization,
- file uploads,
- command execution,
- secrets,
- cryptography.

Do not introduce custom cryptographic algorithms.

Do not disable certificate validation.

Do not weaken authentication or authorization checks to make tests pass.

Avoid constructing SQL, shell commands, URLs, or file paths through unsafe string concatenation.

## Source generators and generated files

Do not manually edit generated source files.

Modify the source, template, schema, generator, or configuration responsible for producing them.

If generated output is committed to the repository, regenerate it using the established repository tooling.

## Public API changes

When changing a library's public API:

1. Prefer backward-compatible changes.
2. Consider source and binary compatibility.
3. Update tests.
4. Update documentation or examples where applicable.
5. Clearly identify intentional breaking changes.

Do not make public members merely to simplify testing. Prefer testing through the intended public surface or using appropriate internal visibility mechanisms already used by the repository.

## Comments

Prefer readable code over comments that restate the implementation.

Add comments when they explain:

- why something unusual is necessary,
- an important invariant,
- a non-obvious workaround,
- compatibility constraints,
- performance trade-offs.

Do not add comments that simply translate code into English.

## Documentation

Update documentation when behavior, configuration, setup, public APIs, or operational procedures change.

Examples and commands in documentation should be runnable when practical.

## Changes and scope

Keep changes tightly scoped to the requested task.

Do not opportunistically:

- rename unrelated types,
- move unrelated files,
- reformat unrelated code,
- change package versions unnecessarily,
- rewrite working components,
- introduce new architectural patterns.

If a larger refactor is genuinely required, keep it separate from the functional change when practical.

## Validation before completion

Before considering work complete, perform the checks that apply to the change.

A typical validation sequence is:

```bash
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet format --verify-no-changes
```

If the repository is large, targeted builds and tests may be used while iterating, but the broadest practical validation should be performed before completion.

Also review:

```bash
git diff
git status
```

Make sure the final diff contains only intentional changes.

## Do not modify

Unless explicitly requested, do not modify:

- SDK roll-forward policy,
- target framework major version,
- CI/CD credentials,
- package feeds,
- signing configuration,
- release/versioning infrastructure,
- generated artifacts,
- security policies,
- deployment infrastructure.

## Agent behavior

Before changing code:

1. Inspect the relevant project and nearby implementation.
2. Look for existing patterns before introducing new ones.
3. Read relevant tests.
4. Prefer extending existing abstractions over creating parallel ones.

While changing code:

1. Make the smallest coherent change that solves the problem.
2. Preserve existing naming and architectural conventions.
3. Add tests for new behavior where appropriate.
4. Avoid unrelated cleanup.

After changing code:

1. Build the affected projects.
2. Run relevant tests.
3. Run repository-wide validation when practical.
4. Inspect the final diff for accidental changes.
5. Report any validation step that could not be completed.

Never claim that a build, test, formatter, or command succeeded unless it was actually run successfully.

## Repository-specific instructions

More specific `AGENTS.md` files may exist in subdirectories.

When working on files beneath such a directory, follow both this file and the nearest applicable `AGENTS.md`.

More specific instructions take precedence over more general ones when they conflict.
