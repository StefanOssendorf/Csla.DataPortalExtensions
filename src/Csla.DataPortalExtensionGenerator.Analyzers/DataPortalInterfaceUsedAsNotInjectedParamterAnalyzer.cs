using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Ossendorf.Csla.DataPortalExtensionGenerator.Analyzers;

/// <summary>
/// Analyzer which reports the absence of the InjectAttribute on a CSLA.NET method parameter of type IDataPortal or IChildDataPortal.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DataPortalInterfaceUsedAsNotInjectedParamterAnalyzer : DiagnosticAnalyzer {

    /// <summary>
    /// The diagnostic id.
    /// </summary>
    public const string DiagnosticId = Constants.DiagnosticId.DataPortalInterfaceUsedAsNotInjectedParameter;
    private const string Category = Constants.Category.Usage;

    private static readonly LocalizableString _title = new LocalizableResourceString(nameof(Resources.DPEG1001Title), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString _messageFormat = new LocalizableResourceString(nameof(Resources.DPEG1001MessageFormat), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString _description = new LocalizableResourceString(nameof(Resources.DPEG1001Description), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor _rule = new(DiagnosticId, _title, _messageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: _description);

    private static readonly ImmutableArray<DiagnosticDescriptor> _supportedDiagnostics = ImmutableArray.Create(_rule);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _supportedDiagnostics;

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationStart => {
            var compilation = compilationStart.Compilation;

            var dataPortal = CompilationHelper.ResolveType(compilation, "Csla.IDataPortal`1");
            if (dataPortal is null) {
                return;
            }

            var childDataPortal = CompilationHelper.ResolveType(compilation, "Csla.IChildDataPortal`1");
            if (childDataPortal is null) {
                return;
            }

            var portalAttributeTypes = ResolvePortalAttributeTypes(compilation);
            if (portalAttributeTypes.IsEmpty) {
                return;
            }

            var injectAttribute = CompilationHelper.ResolveType(compilation, "Csla.InjectAttribute")!;

            compilationStart.RegisterSymbolAction(ctx => AnalyzePortalMethod(ctx, dataPortal, childDataPortal, portalAttributeTypes, injectAttribute), SymbolKind.Method);
        });
    }

    private static readonly ImmutableArray<string> _portalAttributeMetadataNames = [
        "Csla.CreateAttribute",
        "Csla.FetchAttribute",
        "Csla.InsertAttribute",
        "Csla.UpdateAttribute",
        "Csla.ExecuteAttribute",
        "Csla.DeleteAttribute",
        "Csla.DeleteSelfAttribute",
        "Csla.CreateChildAttribute",
        "Csla.FetchChildAttribute",
        "Csla.InsertChildAttribute",
        "Csla.UpdateChildAttribute",
        "Csla.DeleteSelfChildAttribute",
        "Csla.ExecuteChildAttribute"
    ];

    private static ImmutableHashSet<INamedTypeSymbol> ResolvePortalAttributeTypes(Compilation compilation) {
        var builder = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var metadataName in _portalAttributeMetadataNames) {
            var type = compilation.GetTypeByMetadataName(metadataName);
            if (type is not null) {
                builder.Add(type);
            }
        }

        return builder.ToImmutable();
    }

    private static void AnalyzePortalMethod(SymbolAnalysisContext context, INamedTypeSymbol dataPortal, INamedTypeSymbol childDataPortal, ImmutableHashSet<INamedTypeSymbol> portalAttributeTypes, INamedTypeSymbol injectAttribute) {
        if (context.IsGeneratedCode) {
            return;
        }

        var methodSymbol = (IMethodSymbol)context.Symbol;

        var methodAttributes = methodSymbol.GetAttributes();
        if (methodAttributes.IsDefaultOrEmpty) {
            return;
        }

        if (!HasPortalAttribute(methodAttributes, portalAttributeTypes, context.CancellationToken)) {
            return;
        }

        foreach (var parameter in methodSymbol.Parameters) {
            var parameterOriginalType = parameter.Type.OriginalDefinition;

            if (!SymbolEqualityComparer.Default.Equals(parameterOriginalType, dataPortal) &&
                !SymbolEqualityComparer.Default.Equals(parameterOriginalType, childDataPortal)) {
                continue;
            }

            if (HasInjectAttribute(parameter, injectAttribute, context.CancellationToken)) {
                continue;
            }

            var parameterSyntax = parameter.DeclaringSyntaxReferences[0].GetSyntax();
            context.ReportDiagnostic(Diagnostic.Create(_rule, parameterSyntax.GetLocation(), parameter.Name));
        }
    }

    private static bool HasPortalAttribute(ImmutableArray<AttributeData> attributes, ImmutableHashSet<INamedTypeSymbol> portalAttributeTypes, CancellationToken ct) {
        for (var i = 0; i < attributes.Length; i++) {
            ct.ThrowIfCancellationRequested();

            if (attributes[i].AttributeClass is not null && portalAttributeTypes.Contains(attributes[i].AttributeClass!)) {
                return true;
            }
        }

        return false;
    }

    private static bool HasInjectAttribute(IParameterSymbol parameter, INamedTypeSymbol injectAttribute, CancellationToken ct) {
        var attributes = parameter.GetAttributes();
        if (attributes.IsDefaultOrEmpty) {
            return false;
        }

        for (var i = 0; i < attributes.Length; i++) {
            ct.ThrowIfCancellationRequested();

            if (SymbolEqualityComparer.Default.Equals(attributes[i].AttributeClass, injectAttribute)) {
                return true;
            }
        }

        return false;
    }
}