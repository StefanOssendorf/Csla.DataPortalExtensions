using Microsoft.CodeAnalysis;

namespace Ossendorf.Csla.DataPortalExtensionGenerator.Tests;

// Copied from https://github.com/VerifyTests/Verify.SourceGenerators/issues/67#issuecomment-1536710180
internal sealed class RunResultWithIgnoreList {
    public required GeneratorDriverRunResult Result { get; init; }
    public HashSet<string> IgnoredFiles { get; init; } = new();
}