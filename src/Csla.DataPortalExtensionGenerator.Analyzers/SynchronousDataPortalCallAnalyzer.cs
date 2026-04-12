using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;

namespace Ossendorf.Csla.DataPortalExtensionGenerator.Analyzers;

/// <summary>
/// Analyzer which reports the usage of synchronous IDataPortal or IChildDataPortal methods instead of the generated extension methods.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SynchronousDataPortalCallAnalyzer : DiagnosticAnalyzer {
    /// <summary>
    /// The diagnostic id.
    /// </summary>
    public const string DiagnosticId = Constants.DiagnosticId.SynchronousDataPortalCallUsed;
    private const string Category = Constants.Category.Usage;

    private static readonly LocalizableString _title = new LocalizableResourceString(nameof(Resources.DPEG1002Title), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString _messageFormat = new LocalizableResourceString(nameof(Resources.DPEG1002MessageFormat), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString _description = new LocalizableResourceString(nameof(Resources.DPEG1002Description), Resources.ResourceManager, typeof(Resources));

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

            compilationStart.RegisterOperationAction(ctx => AnalyzeOperation(ctx, dataPortal, childDataPortal), OperationKind.Invocation);
        });
    }

    private static void AnalyzeOperation(OperationAnalysisContext context, INamedTypeSymbol dataPortal, INamedTypeSymbol childDataPortal) {
        var invocationExpression = (IInvocationOperation)context.Operation;
        var targetMethod = invocationExpression.TargetMethod;

        var methodName = targetMethod.Name;

        INamedTypeSymbol expectedType;
        switch (methodName) {
            case "Create":
            case "Delete":
            case "Execute":
            case "Fetch":
                expectedType = dataPortal;
                break;
            case "CreateChild":
            case "FetchChild":
                expectedType = childDataPortal;
                break;
            default:
                return;
        }

        context.CancellationToken.ThrowIfCancellationRequested();

        if (!SymbolEqualityComparer.Default.Equals(targetMethod.ContainingType.OriginalDefinition, expectedType)) {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(_rule, invocationExpression.Syntax.GetLocation(), methodName));
    }
}