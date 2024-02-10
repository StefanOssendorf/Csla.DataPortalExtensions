using Csla;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Ossendorf.Csla.DataPortalExtensionsGenerator.Tests;

internal class TestHelper {

    private const string ClassToGenerateExtensionsInto = @$"
namespace GeneratorTests {{
    [{GeneratorHelper.FullyQalifiedNameOfMarkerAttribute}]
    public static partial class DataPortalExtensions {{
    }}
}}";

    public static Task Verify(string cslaSource) => Verify(cslaSource, s => s);

    public static Task Verify(string cslaSource, string additionalSource) => Verify(cslaSource, additionalSource, s => s);

    public static Task Verify(string cslaSource, Func<SettingsTask, SettingsTask> configureVerify) => Verify(cslaSource, "", configureVerify);

    public static Task Verify(string cslaSource, string additionalSource, Func<SettingsTask, SettingsTask> configureVerify) {

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
                MetadataReference.CreateFromFile(typeof(FetchAttribute).Assembly.Location)
            });

        var compilation = CSharpCompilation.Create(
                assemblyName: "GeneratorTests",
                syntaxTrees: syntaxTrees,
                references: references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

        var generator = new DataPortalExtensionsGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var _);

        outputCompilation.GetDiagnostics().Should().BeEmpty();
        return configureVerify(
            Verifier.Verify(CreateResultFromRun(driver))
                .UseDirectory("Snapshots")
                .ScrubLinesContaining(StringComparison.Ordinal, ".GeneratedCode(\"Ossendorf.Csla.Dataportal")
        //.AutoVerify()
        );
    }

    private static RunResultWithIgnoreList CreateResultFromRun(GeneratorDriver driver) {
        var result = driver.GetRunResult();
        _ = result.GeneratedTrees.Length.Should().Be(2, "The generated code must contain the attribute and the generated extenion class.");
        return new RunResultWithIgnoreList {
            Result = result,
            IgnoredFiles = { "DataPortalExtensionsAttribute.g.cs" }
        };
    }
}