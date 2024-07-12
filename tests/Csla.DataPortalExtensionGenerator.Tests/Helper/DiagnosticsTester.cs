using Csla;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Ossendorf.Csla.DataPortalExtensionGenerator.Tests.Helper;

internal static class DiagnosticsTester {
    public static void Verify(string portalExtensionClass, string expectedDiagnosticId)
        => Verify(portalExtensionClass, "", expectedDiagnosticId);

    public static void Verify(string portalExtensionClass, string cslaClass, string expectedDiagnosticId)
        => Verify(portalExtensionClass, cslaClass, null, diagnostics => diagnostics.Should().OnlyContain(d => d.Id == expectedDiagnosticId), d => { });

    public static void Verify(string validExtensionClass, string cslaClass, string expectedDiagnosticId, TestAnalyzerConfigOptionsProvider globalCompilerOptions)
        => Verify(validExtensionClass, cslaClass, globalCompilerOptions, diagnostics => diagnostics.Should().OnlyContain(d => d.Id == expectedDiagnosticId), d => { });

    public static void Verify(string portalExtensionClass, string cslaClass, AnalyzerConfigOptionsProvider? globalConfigOptionsProvider, Action<IEnumerable<Diagnostic>> sourceGenReportedDiagnostics, Action<IEnumerable<Diagnostic>> compilerReportedDiagnostics, NullableContextOptions nullableContextOptions = NullableContextOptions.Enable) {
        var syntaxTrees = new List<SyntaxTree>() {
            CSharpSyntaxTree.ParseText(portalExtensionClass), // ExtensionClassTree
        };
        if (!string.IsNullOrWhiteSpace(cslaClass)) {
            syntaxTrees.Add(CSharpSyntaxTree.ParseText(cslaClass)); // CslaContainingTypeTree
        }

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Concat(new[] {
                MetadataReference.CreateFromFile(typeof(FetchAttribute).Assembly.Location)
            });

        var compilation = CSharpCompilation.Create(
                assemblyName: "GeneratorTests",
                syntaxTrees: syntaxTrees,
                references: references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(nullableContextOptions)
            );

        var generator = new DataPortalExtensionGenerator();

        var driver = CSharpGeneratorDriver.Create(generator).WithUpdatedAnalyzerConfigOptions(globalConfigOptionsProvider ?? TestAnalyzerConfigOptionsProvider.Empty);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);
        using (new AssertionScope()) {
            sourceGenReportedDiagnostics(diagnostics);
            compilerReportedDiagnostics(outputCompilation.GetDiagnostics());
        }
    }
}
