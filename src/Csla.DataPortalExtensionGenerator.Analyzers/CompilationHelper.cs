using Microsoft.CodeAnalysis;

namespace Ossendorf.Csla.DataPortalExtensionGenerator.Analyzers;

internal static class CompilationHelper {

    internal static INamedTypeSymbol? ResolveType(Compilation compilation, string metadataName)
        => compilation.GetTypeByMetadataName(metadataName);
}