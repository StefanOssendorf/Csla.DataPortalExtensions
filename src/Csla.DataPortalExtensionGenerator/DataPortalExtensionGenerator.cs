using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Ossendorf.Csla.DataPortalExtensionGenerator.Diagnostics;
using System.Text;

namespace Ossendorf.Csla.DataPortalExtensionGenerator;

/// <summary>
/// The data portal extension source generator.
/// </summary>
[Generator]
public sealed partial class DataPortalExtensionGenerator : IIncrementalGenerator {

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        AddMarkerAttribute(context);
        AddCodeGenerator(context);
    }

    private static void AddMarkerAttribute(IncrementalGeneratorInitializationContext context)
        => context.RegisterPostInitializationOutput(ctx => ctx.AddSource("DataPortalExtensionsAttribute.g.cs", SourceText.From(GeneratorHelper.MarkerAttribute, Encoding.UTF8)));

    private static void AddCodeGenerator(IncrementalGeneratorInitializationContext context) {
        var options = GetGeneratorOptions(context);

        var extensionClassesAndDiagnostics = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: GeneratorHelper.FullyQalifiedNameOfMarkerAttribute,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: Parser.GetExtensionClass
            );

        var extensionClassDeclaration = extensionClassesAndDiagnostics
            .Select((r, _) => r.Value);

        var methodDeclarationsAndDiagnostics = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: Parser.CouldBeCslaDataPortalAttribute,
                transform: Parser.GetPortalMethods
            );

        var methodDeclarations = methodDeclarationsAndDiagnostics
            .Where(static m => m.Value.IsValid)
            .Select((m, _) => m.Value.PortalOperationToGenerate)
            .Collect();

        var classesToGenerateInto = extensionClassDeclaration.Combine(methodDeclarations).Combine(options);

        context.RegisterSourceOutput(
            extensionClassesAndDiagnostics.SelectMany((r, _) => r.Errors),
            static (ctx, info) => ctx.ReportDiagnostic(info)
        );
        context.RegisterSourceOutput(
            methodDeclarationsAndDiagnostics.SelectMany((m, _) => m.Errors),
            static (ctx, info) => ctx.ReportDiagnostic(info)
        );

        context.RegisterSourceOutput(
            source: classesToGenerateInto,
            action: Emitter.EmitExtensionClass
        );
    }

    private static IncrementalValueProvider<GeneratorOptions> GetGeneratorOptions(IncrementalGeneratorInitializationContext context) {
        return context.AnalyzerConfigOptionsProvider.Select((options, _) => {

            if (!options.GlobalOptions.TryGetValue("build_property.DataPortalExtensionGen_MethodPrefix", out var methodPrefix) || methodPrefix is null) {
                methodPrefix = "";
            }

            if (!options.GlobalOptions.TryGetValue("build_property.DataPortalExtensionGen_MethodSuffix", out var methodSuffix) || methodSuffix is null) {
                methodSuffix = "";
            }

            return new GeneratorOptions(methodPrefix, methodSuffix);
        });
    }
}