using Csla;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace Ossendorf.Csla.DataPortalExtensionGenerator.Analyzers.Test; 
public static partial class CSharpAnalyzerVerifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new() {
    public class Test : CSharpAnalyzerTest<TAnalyzer, XUnitVerifier> {
        public Test() {
            SolutionTransforms.Add((solution, projectId) => {
                var compilationOptions = solution.GetProject(projectId)!.CompilationOptions!;
                compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings));
                solution = solution.WithProjectCompilationOptions(projectId, compilationOptions);

                var references = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
                    .Select(a => MetadataReference.CreateFromFile(a.Location))
                    .Concat(new[] {
                        MetadataReference.CreateFromFile(typeof(FetchAttribute).Assembly.Location)
                    });
                solution = solution.WithProjectMetadataReferences(projectId, references);
                return solution;
            });

        }
    }
}
