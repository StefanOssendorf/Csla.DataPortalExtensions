# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
dotnet build
dotnet test
dotnet test --filter "FullyQualifiedName~GeneratorSnapshotTests"
dotnet pack -c Release -o artifacts/
```

### Updating snapshots

Tests use Verify.SourceGenerators for snapshot-based testing. When generator output changes intentionally, update snapshots by running:

```bash
VERIFY_AUTO_APPROVE_ALL=true dotnet test
```

Snapshots live in `tests/Csla.DataPortalExtensionGenerator.Tests/Snapshots/`.

## Workflows

### Making changes to the codebase
When making any kind of change:
1. Work with up-to-date master branch
2. Create a new branch following the scheme for each type of task
   1. Feature Branch: feature/<gh issue numbber>-<short description>
   2. Bug Branch: bug/<gh issue number>-<short description>
   3. Code clean up Branch: cleanup/<gh issue number>-<short description>
   4. Everything else: misc/<gh issue number>-<short description>
3. Plan the changes, including new/updated methods, AnalyzersReleases.*.md modification
4. Implement the changes
5. Add passing tests to verify it's working

## Architecture

This is a **Roslyn incremental source generator** (`IIncrementalGenerator`) that generates strongly-typed extension methods for CSLA.NET's `IDataPortal<T>` and `IChildDataPortal<T>` interfaces. Consuming projects decorate a `partial static class` with `[DataPortalExtensions]`, and the generator produces extension methods wrapping each CSLA portal method tagged with `[Fetch]`, `[FetchChild]`, `[Create]`, `[CreateChild]`, `[Delete]`, or `[Execute]`. Annotate a method with `[GenerateNoDataPortalExtension]` to exclude it from generation.

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

### Diagnostics

Analyzer diagnostics are tracked in `src/Csla.DataPortalExtensionGenerator.Analyzers/AnalyzerReleases.Shipped.md` and `AnalyzerReleases.Unshipped.md`. Generator diagnostics (DPEGEN001–004) are defined in `src/Csla.DataPortalExtensionGenerator/Diagnostics/`.

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

## Code style

Conventions are enforced via `.editorconfig`. A `pre-commit` git hook in `.githooks/` runs `dotnet format` automatically on every commit. Activate it once per clone with:

```bash
git config core.hooksPath .githooks
```

## Versioning

Uses MinVer — version is derived from git tags with prefix `v` (e.g., `v1.2.3`). Minimum version: `0.1`, pre-release identifier: `preview.0`.
