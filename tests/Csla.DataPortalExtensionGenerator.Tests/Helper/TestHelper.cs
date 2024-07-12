using Csla;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Ossendorf.Csla.DataPortalExtensionGenerator.Tests.Helper;

internal static class TestHelper {
    private const string ClassToGenerateExtensionsInto = @$"
namespace GeneratorTests {{
    [Ossendorf.Csla.DataPortalExtensionGenerator.DataPortalExtensionsAttribute]
    public static partial class DataPortalExtensions {{
    }}
}}";

    public static (GeneratorDriver Driver, CSharpCompilation Compilation) SetupSourceGenerator(string cslaSource, string additionalSource, bool enableNullableContext, TestAnalyzerConfigOptionsProvider globalCompilerOptions) {
        var syntaxTrees = new List<SyntaxTree>() {
            CSharpSyntaxTree.ParseText(ClassToGenerateExtensionsInto), // ExtensionClassTree
            CSharpSyntaxTree.ParseText(cslaSource) // CslaContainingTypeTree
        };

        if (!string.IsNullOrWhiteSpace(additionalSource)) {
            syntaxTrees.Add(CSharpSyntaxTree.ParseText(additionalSource));
        }

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Concat(new[] {
                MetadataReference.CreateFromFile(typeof(FetchAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(DataPortalExtensionsAttribute).Assembly.Location)
            });

        var compilation = CSharpCompilation.Create(
                assemblyName: "GeneratorTests",
                syntaxTrees: syntaxTrees,
                references: references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(enableNullableContext ? NullableContextOptions.Enable : NullableContextOptions.Disable)
                .WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic> { { "CS1701", ReportDiagnostic.Suppress } })
            );

        var generator = new DataPortalExtensionGenerator().AsSourceGenerator();

        var driverOptions = new GeneratorDriverOptions(disabledOutputs: IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true);
        var driver = CSharpGeneratorDriver.Create([generator], driverOptions: driverOptions).WithUpdatedAnalyzerConfigOptions(globalCompilerOptions);

        return (driver, compilation);
    }
}