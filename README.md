# MyProject

`MyProject` is a .NET 10 solution organized around centralized build configuration, package management, consistent code quality rules, and reproducible builds.

## Requirements

Install the .NET SDK version selected by [`global.json`](./global.json).

Verify your environment with:

```bash
dotnet --version
dotnet --info
```

The repository is configured to use a compatible .NET 10 SDK according to the roll-forward policy defined in `global.json`.

## Repository structure

```text
.
├── src/
│   ├── MyProject.App/
│   └── MyProject.Core/
│
├── tests/
│   └── MyProject.Core.Tests/
│
├── AGENTS.md
├── Directory.Build.props
├── Directory.Build.targets
├── Directory.Packages.props
├── global.json
├── MyProject.slnx
└── README.md
```

### Repository configuration

The repository uses several root-level files to keep configuration centralized:

| File | Purpose |
| --- | --- |
| `global.json` | Selects the .NET SDK and SDK roll-forward policy |
| `Directory.Build.props` | Defines repository-wide MSBuild and compiler settings |
| `Directory.Build.targets` | Contains repository-wide custom build targets when needed |
| `Directory.Packages.props` | Centrally manages NuGet package versions |
| `.editorconfig` | Defines formatting and code-style conventions |
| `AGENTS.md` | Instructions for AI coding agents and automated development tools |
| `MyProject.slnx` | Solution definition |

Individual project files should remain as small as practical and inherit shared configuration from these files.

## Getting started

Clone the repository and restore dependencies:

```bash
git clone <repository-url>
cd MyProject
