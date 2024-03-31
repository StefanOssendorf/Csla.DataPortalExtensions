using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Ossendorf.Csla.DataPortalExtensionGenerator.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ossendorf.Csla.DataPortalExtensionGenerator;

internal class ExtensionClassGeneratorPart {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static IncrementalValuesProvider<ClassForExtensions> GetExtensionClasses(IncrementalGeneratorInitializationContext context) {
        var extensionClassesAndDiagnostics = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: typeof(DataPortalExtensionsAttribute).FullName,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: Parser.GetExtensionClass
            );

        context.RegisterSourceOutput(
            extensionClassesAndDiagnostics.SelectMany((r, _) => r.Errors),
            static (ctx, info) => ctx.ReportDiagnostic(info)
        );

        return extensionClassesAndDiagnostics.Select((r, _) => r.Value);
    }
}