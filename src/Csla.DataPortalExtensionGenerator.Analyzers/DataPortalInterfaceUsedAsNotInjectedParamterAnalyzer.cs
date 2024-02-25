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
    public const string DiagnosticId = Constants.DiagnosticId.DataPortalInterfaceUsedAsNotInjectedParamter;
    private const string Category = Constants.Category.Usage;

    private static readonly LocalizableString _title = new LocalizableResourceString(nameof(Resources.DPEG1001Title), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString _messageFormat = new LocalizableResourceString(nameof(Resources.DPEG1001MessageFormat), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString _description = new LocalizableResourceString(nameof(Resources.DPEG1001Description), Resources.ResourceManager, typeof(Resources));

    private static readonly DiagnosticDescriptor _rule = new(DiagnosticId, _title, _messageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: _description);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(_rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzePortalMethod, SymbolKind.Method);
    }

    private static readonly ImmutableHashSet<string> _portalAttributes = ImmutableHashSet.Create(
        "CreateAttribute",
        "FetchAttribute",
        "InsertAttribute",
        "UpdateAttribute",
        "ExecuteAttribute",
        "DeleteAttribute",
        "DeleteSelfAttribute",
        "CreateChildAttribute",
        "FetchChildAttribute",
        "InsertChildAttribute",
        "UpdateChildAttribute",
        "DeleteSelfChildAttribute",
        "ExecuteChildAttribute"
    );

    private void AnalyzePortalMethod(SymbolAnalysisContext context) {
        if (context.IsGeneratedCode) {
            return;
        }

        var methodSymbol = (IMethodSymbol)context.Symbol;

        var methodAttributes = methodSymbol.GetAttributes();
        if (methodAttributes.IsDefaultOrEmpty) {
            return;
        }

        if (!HasCslaAttribute(methodSymbol, _portalAttributes.Contains, context.CancellationToken)) {
            return;
        }

        foreach (var parameter in methodSymbol.Parameters) {

            if (parameter.Type is not { ContainingNamespace.Name: "Csla", Name: "IDataPortal" or "IChildDataPortal" }) {
                continue;
            }

            if (HasCslaAttribute(parameter, static name => name == "InjectAttribute", context.CancellationToken)) {
                continue;
            }

            var parameterSyntax = parameter.DeclaringSyntaxReferences.First().GetSyntax();
            context.ReportDiagnostic(Diagnostic.Create(_rule, parameterSyntax.GetLocation(), parameter.Name));
        }
    }

    private static bool HasCslaAttribute(ISymbol symbol, Func<string, bool> isAttribute, CancellationToken ct) {
        var methodAttributes = symbol.GetAttributes();
        if (methodAttributes.IsDefaultOrEmpty) {
            return false;
        }

        for (var i = 0; i < methodAttributes.Length; i++) {
            ct.ThrowIfCancellationRequested();

            var methodAttribute = methodAttributes[i];
            if (methodAttribute.AttributeClass is { ContainingNamespace.Name: "Csla" } cslaAttribute && isAttribute(cslaAttribute.Name)) {
                return true;
            }
        }

        return false;
    }
}
