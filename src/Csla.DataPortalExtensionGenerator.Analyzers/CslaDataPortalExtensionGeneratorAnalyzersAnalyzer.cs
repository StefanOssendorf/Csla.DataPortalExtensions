using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;

namespace Ossendorf.Csla.DataPortalExtensionGenerator.Analyzers;

/// <summary>
/// Analyzer which reports the usage of the IDataPortal methods instead of the generated extension methods.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NotDataPortalExtensionMethodUsedAnalyzer : DiagnosticAnalyzer {
    /// <summary>
    /// The diagnostic id.
    /// </summary>
    public const string DiagnosticId = "DPEG1000";

    private static readonly LocalizableString _title = new LocalizableResourceString(nameof(Resources.DPEG1000Title), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString _messageFormat = new LocalizableResourceString(nameof(Resources.DPEG1000MessageFormat), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString _description = new LocalizableResourceString(nameof(Resources.DPEG1000Description), Resources.ResourceManager, typeof(Resources));
    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor _rule = new(DiagnosticId, _title, _messageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: _description);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(_rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context) {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterOperationAction(AnalyzeOperation, OperationKind.Invocation);
    }

    private void AnalyzeOperation(OperationAnalysisContext context) {
        var invocationExpression = (IInvocationOperation)context.Operation;
        var targetMethod = invocationExpression.TargetMethod;

        var methodName = targetMethod.Name;

        string expectedType;
        switch (methodName) {
            case "CreateAsync":
            case "DeleteAsync":
            case "ExecuteAsync":
            case "FetchAsync":
            case "UpdateAsync":
                expectedType = "IDataPortal";
                break;
            case "CreateChildAsync":
            case "FetchChildAsync":
            case "UpdateChildAsync":
                expectedType = "IChildDataPortal";
                break;
            default:
                return;
        }

        context.CancellationToken.ThrowIfCancellationRequested();

        if (targetMethod.ContainingType is not { ContainingNamespace.Name: "Csla" } cslaType) {
            return;
        }

        context.CancellationToken.ThrowIfCancellationRequested();

        if (cslaType.Name != expectedType) {
            return;
        }

        context.CancellationToken.ThrowIfCancellationRequested();

        context.ReportDiagnostic(Diagnostic.Create(_rule, invocationExpression.Syntax.GetLocation(), methodName));
    }
}
