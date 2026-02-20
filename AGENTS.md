# Repository Guidelines

## Project Structure & Module Organization
Core engine modules live in top-level folders such as `Engine/`, `Common/`, `Brokerages/`, `Indicators/`, and `Algorithm.Framework/`. Reference and regression algorithms are in `Algorithm.CSharp/` and `Algorithm.Python/`. Executable entry points and tools are in `Launcher/`, `Research/`, `ToolBox/`, `Api/`, and `Optimizer.Launcher/`. Tests are centralized in `Tests/` and generally mirror production areas (for example, `Tests/Engine/...` and `Tests/Api/...`). Documentation assets are under `Documentation/`, and runtime config templates are in files like `Launcher/config.json` and `*/config.example.json`.

## Build, Test, and Development Commands
Use these commands from the repository root:

```bash
dotnet build QuantConnect.Lean.sln /p:Configuration=Debug /p:WarningLevel=1
```
Builds the full solution.

```bash
cd Launcher/bin/Debug && dotnet QuantConnect.Lean.Launcher.dll --config ./config.json
```
Runs the LEAN launcher locally.

```bash
dotnet test Tests/QuantConnect.Tests.csproj --filter "TestCategory!=TravisExclude&TestCategory!=ResearchRegressionTests"
```
Runs the primary CI-aligned test suite.

```bash
dotnet test Tests/QuantConnect.Tests.csproj --filter "TestCategory=RegressionTests"
python run_syntax_check.py
```
Runs regression tests and Python algorithm syntax/type checks.

## Coding Style & Naming Conventions
Follow `.editorconfig`: UTF-8, spaces (no tabs), final newline, default 4-space indentation, and 2-space indentation for `*.json`, `*.yml`, and `*.csproj`. Follow Microsoft C# style conventions (PascalCase for public types/members, camelCase for locals/parameters). Keep classes and modules focused on one responsibility. For framework modules, avoid non-essential logging/charting logic. Keep Python algorithm filenames descriptive and consistent with existing `*Algorithm.py` patterns.

## Testing Guidelines
NUnit is the test framework (`Tests/QuantConnect.Tests.csproj`). Add tests in the matching domain folder and keep file names ending in `*Tests.cs`. Use `[Test]`, `[TestCase]`, and test categories to keep runs targetable. All bug fixes and features should include tests covering normal paths plus edge cases; there is no fixed coverage percentage, but missing tests will block review.

## Commit & Pull Request Guidelines
Use concise, imperative commit subjects (for example, `Fix ...`, `Add ...`, `Refactor ...`), optionally with prefixes like `feat:`/`fix:`. Mirror repository history by appending issue/PR references when possible, e.g., `Fix combo order queue affinity (#9293)`. Create topic branches from `master` using patterns like `bug-123-short-description` or `feature-123-short-description`. PRs should clearly describe the problem, scope, test evidence, and any config or data implications, and link the related issue.
