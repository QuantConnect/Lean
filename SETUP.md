# Local Development Setup

This guide walks you through setting up the QuantConnect Lean project locally.

## Prerequisites

- **macOS** with Homebrew installed
- **.NET 10.0 SDK or higher**

## Installation Steps

### 1. Install .NET SDK

Install .NET 10.0 using Homebrew:

```bash
brew install dotnet@10
```

Verify the installation:

```bash
dotnet --version
```

Expected output: `10.0.107` or higher

### 2. Restore Dependencies

Navigate to the project root and restore NuGet packages:

```bash
cd Lean
dotnet restore QuantConnect.Lean.sln
```

This downloads all required NuGet packages for the solution.

### 3. Build the Project

Build the solution in Release mode:

```bash
dotnet build QuantConnect.Lean.sln -c Release
```

A successful build will show:
- `0 Error(s)`
- `Time Elapsed: ~5 minutes` (depending on your machine)

### 4. Verify Build Success

After building, you should have compiled binaries. The build process creates output in `bin/Release/` directories across all projects.

## Project Structure

- **Algorithm** - Core algorithm framework
- **Engine** - Backtesting and live trading engine
- **Brokerages** - Broker integrations
- **Data** - Market data handling
- **Indicators** - Technical indicators
- **Tests** - Unit and integration tests
- **Launcher** - CLI application launcher

## Running Tests

To run the test suite:

```bash
dotnet test QuantConnect.Lean.sln -c Release
```

## Using the Lean CLI

After setup, you can use the official Lean CLI tool:

```bash
pip install lean
lean backtest
lean research
lean live
```

See the [project README](./readme.md) for more CLI commands.

## Troubleshooting

### .NET SDK Version Mismatch

If you see error `NETSDK1045`:
```
The current .NET SDK does not support targeting .NET 10.0
```

Ensure you have .NET 10.0 installed:
```bash
dotnet --list-sdks
```

If not installed, run:
```bash
brew install dotnet@10
```

### NuGet Package Vulnerabilities

You may see warnings about vulnerable packages (e.g., `System.Drawing.Common`, `DotNetZip`). These are pre-existing dependencies and don't block builds. They're noted for awareness when deploying to production.

### Slow Initial Build

The first build takes ~5 minutes. Subsequent builds are faster due to incremental compilation.

## Next Steps

1. Create your first algorithm in the `Algorithm.CSharp` project
2. Run backtests using the Lean CLI
3. Check out [QuantConnect Documentation](https://www.quantconnect.com/docs) for algorithm development guides
