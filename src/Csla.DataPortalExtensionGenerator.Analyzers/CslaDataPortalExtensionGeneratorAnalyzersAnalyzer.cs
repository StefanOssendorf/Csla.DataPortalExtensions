using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;

namespace Ossendorf.Csla.DataPortalExtensionGenerator.Analyzers {
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NotDataPortalExtensionMethodUsedAnalyzer : DiagnosticAnalyzer {
        public const string DiagnosticId = "DPEG1000";

        private static readonly string Title = "Csla data portal method is used";
        private static readonly string MessageFormat = "The data portal method '{0}' should not be used. Use a generated extension method instead.";
        private static readonly string Description = "The data portal method should not be used. Instead the type and compile safe extension method from the data portal extension generator should be used.";
        private const string Category = "Usage";

        private static readonly DiagnosticDescriptor _rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(_rule);

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context) {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterOperationAction(AnalyzeOperation, OperationKind.Invocation);
        }

        private void AnalyzeOperation(OperationAnalysisContext context) {
            var targetMethod = ((IInvocationOperation)context.Operation).TargetMethod;

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

            context.ReportDiagnostic(Diagnostic.Create(_rule, targetMethod.Locations.First(), methodName));
        }
    }
}
