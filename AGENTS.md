# OpenCode Agent Context: QuantConnect Lean

## Project Overview

QuantConnect Lean is an open-source, event-driven algorithmic trading engine written primarily in C#.
It supports backtesting and live trading across multiple asset classes and brokerages.

- **Repository**: `arif-b-khan/Lean` (fork of `QuantConnect/Lean`)
- **Language**: C# (.NET 10.0)
- **Solution**: `QuantConnect.Lean.sln`

## Key Directories

| Directory | Purpose |
|-----------|---------|
| `Algorithm` | Core algorithm framework and base classes |
| `Algorithm.CSharp` | C# example algorithms |
| `Algorithm.Python` | Python algorithm integration |
| `Algorithm.Framework` | Algorithm framework modules (alpha, risk, execution, portfolio) |
| `Engine` | Backtesting and live trading engine |
| `Brokerages` | Brokerage integrations |
| `Common` | Shared utilities, data structures, and constants |
| `Data` | Market data handling and providers |
| `Indicators` | Technical indicators |
| `Launcher` | CLI application launcher |
| `Tests` | Unit and integration tests |
| `ToolBox` | Auxiliary tools and data downloaders |
| `Report` | Reporting and statistics generation |

## Build & Test Commands

```bash
# Restore dependencies
dotnet restore QuantConnect.Lean.sln

# Build Release
dotnet build QuantConnect.Lean.sln -c Release

# Run tests
dotnet test QuantConnect.Lean.sln -c Release
```

## Coding Conventions

- Follow Microsoft C# guidelines.
- Use **4 spaces** for indentation (soft tabs).
- Framework modules should follow the single-responsibility principle.
- Avoid logging or charting inside reusable framework modules.
- New features must include unit tests covering expected and edge cases.

## Local Development

See `SETUP.md` for full local setup instructions, including .NET SDK installation.

## Credentials

For launcher/brokerage development, credentials are managed via .NET user secrets in `Launcher/`.
See `SECRETS_SETUP.md` for details.

## Useful Links

- [Lean Documentation](https://www.quantconnect.com/docs)
- [Lean Forum](https://www.quantconnect.com/forum/discussions/1/lean)
