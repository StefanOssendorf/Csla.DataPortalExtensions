using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Collections.Immutable;
using System.Composition;

namespace Ossendorf.Csla.DataPortalExtensionGenerator.Analyzers;

/// <summary>
/// Codefix to add the [Inject] attribute to the IDataPortal/IChildDataPortal parameter.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DataPortalInterfaceUsedAsNotInjectedParamterAnalyzerCodeFixProvider)), Shared]
public class DataPortalInterfaceUsedAsNotInjectedParamterAnalyzerCodeFixProvider : CodeFixProvider {

    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => ImmutableArray.Create(DataPortalInterfaceUsedAsNotInjectedParamterAnalyzer.DiagnosticId);

    /// <inheritdoc />
    public sealed override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public sealed override Task RegisterCodeFixesAsync(CodeFixContext context) {
        var diagnostic = context.Diagnostics.First();

        context.RegisterCodeFix(
            CodeAction.Create(
                title: CodeFixResources.DataPortalNotInjected,
                createChangedSolution: ct => AddInjectAttribute(ct),
                equivalenceKey: nameof(CodeFixResources.DataPortalNotInjected)
            ),
            diagnostic
        );

        return Task.CompletedTask;
    }

    private Task<Solution> AddInjectAttribute(CancellationToken ct) => throw new NotImplementedException();
}
