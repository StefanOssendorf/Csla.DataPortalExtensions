# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build

# Test
dotnet test

# Run a single test class
dotnet test --filter "FullyQualifiedName~GeneratorSnapshotTests"

# Pack NuGet packages
dotnet pack -c Release -o artifacts/
```

### Updating snapshots

Tests use Verify.SourceGenerators for snapshot-based testing. When generator output changes intentionally, update snapshots by running:

```bash
VERIFY_AUTO_APPROVE_ALL=true dotnet test
```

Snapshots live in `tests/Csla.DataPortalExtensionGenerator.Tests/Snapshots/`.

## Architecture

This is a **Roslyn incremental source generator** (`IIncrementalGenerator`) that generates strongly-typed extension methods for CSLA.NET's `IDataPortal<T>` and `IChildDataPortal<T>` interfaces. Consuming projects decorate a `partial static class` with `[DataPortalExtensions]`, and the generator produces extension methods wrapping each CSLA portal method tagged with `[Fetch]`, `[FetchChild]`, `[Create]`, `[CreateChild]`, `[Delete]`, or `[Execute]`.

### Source projects (`src/`)

| Project | Purpose |
|---------|---------|
| `Csla.DataPortalExtensionGenerator` | Main source generator — `IIncrementalGenerator` entry point |
| `Csla.DataPortalExtensionGenerator.Attributes` | `[DataPortalExtensions]` marker attribute (netstandard2.0) |
| `Csla.DataPortalExtensionGenerator.Analyzers` | Roslyn analyzers for portal usage correctness |
| `Csla.DataPortalExtensionGenerator.Analyzers.CodeFixes` | Code fix providers for analyzer diagnostics |
| `Csla.DataPortalExtensionGenerator.Analyzers.Package` | Packaging project that bundles analyzers into the NuGet |

### Generator pipeline (main project)

- **`Parser.cs`** — Walks syntax trees, finds classes with `[DataPortalExtensions]`, extracts methods with CSLA attributes into `PortalOperationToGenerate` records
- **`Emitter.cs`** — Takes parsed metadata, renders C# source for the extension methods
- **`GeneratorOptions.cs`** / **`OptionsGeneratorPart.cs`** — Reads MSBuild properties into a `GeneratorOptions` record passed through the pipeline
- **`Diagnostics/`** — Typed diagnostic descriptors (e.g., `NotPartialDiagnostic`, `PrivateClassCanNotBeAParameterDiagnostic`)
- **`Internals/EquatableArray<T>`** — Value-equality array wrapper required for incremental generator caching correctness

### Tests (`tests/`)

- **`Csla.DataPortalExtensionGenerator.Tests`** — xUnit + Verify snapshot tests for the generator output (`GeneratorSnapshotTests`, `DiagnosticTests`, `SourceGeneratorStageCachingTests`)
- **`Csla.DataPortalExtensionGenerator.Analyzers.Test`** — Analyzer tests
- **`Csla.DataPortalExtensionGenerator.Tests.CsprojConfig`** — Tests for MSBuild property configuration

## MSBuild configuration properties

Consuming projects can set these properties to customize generated method names:

```xml
<PropertyGroup>
  <DataPortalExtensionGen_MethodPrefix>Prefix</DataPortalExtensionGen_MethodPrefix>
  <DataPortalExtensionGen_MethodSuffix>Suffix</DataPortalExtensionGen_MethodSuffix>
  <DataPortalExtensionGen_NullableContext>Enable</DataPortalExtensionGen_NullableContext>
  <DataPortalExtensionGen_SuppressWarningCS8669>true</DataPortalExtensionGen_SuppressWarningCS8669>
</PropertyGroup>
```

Property names are defined in `ConfigConstants.cs`. Generated methods are intentionally not async-suffixed by design.

## Versioning

Uses MinVer — version is derived from git tags with prefix `v` (e.g., `v1.2.3`). Minimum version: `0.1`, pre-release identifier: `preview.0`.
