using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Ossendorf.Csla.DataPortalExtensionGenerator.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
        var optionsAndDiagnostics = GetGeneratorOptions(context);

        var options = optionsAndDiagnostics
            .Select((o, _) => o.Value);

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
            optionsAndDiagnostics.SelectMany((o, _) => o.Errors),
            static (ctx, info) => ctx.ReportDiagnostic(info)
        );

        context.RegisterSourceOutput(
            source: classesToGenerateInto,
            action: Emitter.EmitExtensionClass
        );
    }

    private static IncrementalValueProvider<Result<GeneratorOptions>> GetGeneratorOptions(IncrementalGeneratorInitializationContext context) {
        return context.AnalyzerConfigOptionsProvider.Select((options, _) => {

            if (!TryGetGlobalOption(ConfigConstants.MethodPrefix, out var methodPrefix) || methodPrefix is null) {
                methodPrefix = "";
            }

            if (!TryGetGlobalOption(ConfigConstants.MethodSuffix, out var methodSuffix) || methodSuffix is null) {
                methodSuffix = "";
            }

            var errors = new List<DiagnosticInfo>();
            var nullableContextOptions = NullableContextOptions.Enable;
            if (TryGetGlobalOption(ConfigConstants.NullableContext, out var nullabilityContext)) {
                if (nullabilityContext.Equals("Disable", StringComparison.OrdinalIgnoreCase)) {
                    nullableContextOptions = NullableContextOptions.Disable;
                } else if (!nullabilityContext.Equals("Enable", StringComparison.OrdinalIgnoreCase)) {
                    errors.Add(NullableContextValueDiagnostic.Create(nullabilityContext));
                }
            }

            var suppressWarningCS8669 = false;
            if (TryGetGlobalOption(ConfigConstants.SuppressWarningCS8669, out var suppressWarningString)) {
                if (!bool.TryParse(suppressWarningString, out suppressWarningCS8669)) {
                    suppressWarningCS8669 = false;
                    errors.Add(SuppressWarningCS8669ValueDiagnostic.Create(suppressWarningString));
                }
            }

            return new Result<GeneratorOptions>(new GeneratorOptions(methodPrefix, methodSuffix, nullableContextOptions, suppressWarningCS8669), new EquatableArray<DiagnosticInfo>([.. errors]));

            bool TryGetGlobalOption(string key, [NotNullWhen(true)] out string? value) => options.GlobalOptions.TryGetValue($"build_property.{key}", out value) && !string.IsNullOrWhiteSpace(value);
        });
    }
}