using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
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
                createChangedSolution: ct => AddInjectAttribute(context.Document, diagnostic.Location.SourceSpan, ct),
                equivalenceKey: nameof(CodeFixResources.DataPortalNotInjected)
            ),
            diagnostic
        );

        return Task.CompletedTask;
    }

    private static async Task<Solution> AddInjectAttribute(Document document, TextSpan diagnosticSpan, CancellationToken ct) {
        var root = await document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
        if (root is null) {
            return document.Project.Solution;
        }

        if (root.FindNode(diagnosticSpan) is not ParameterSyntax parameterNode) {
            return document.Project.Solution;
        }

        var injectAttribute = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("Inject"));
        var attributeList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(injectAttribute))
            .WithTrailingTrivia(SyntaxFactory.ElasticSpace);

        var newParameter = parameterNode.AddAttributeLists(attributeList);
        var newRoot = root.ReplaceNode(parameterNode, newParameter);

        return document.WithSyntaxRoot(newRoot).Project.Solution;
    }
}