﻿using Csla;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Ossendorf.Csla.DataPortalExtensionGenerator.Tests;

internal class TestHelper {

    private const string ClassToGenerateExtensionsInto = @$"
namespace GeneratorTests {{
    [Ossendorf.Csla.DataPortalExtensionGenerator.DataPortalExtensionsAttribute]
    public static partial class DataPortalExtensions {{
    }}
}}";

    public static Task Verify(string cslaSource) => Verify(cslaSource, "");

    public static Task Verify(string cslaSource, string additionalSource) => Verify(cslaSource, additionalSource, 2);

    public static Task Verify(string cslaSource, string additionalSource, int expectedFileCount) => Verify(cslaSource, additionalSource, s => s, expectedFileCount);

    public static Task Verify(string cslaSource, Func<SettingsTask, SettingsTask> configureVerify) => Verify(cslaSource, "", configureVerify, 2);

    public static Task Verify(string cslaSource, string additionalSource, Func<SettingsTask, SettingsTask> configureVerify, int expectedFileCount) {

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

        var generator = new DataPortalExtensionGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var _);

        outputCompilation.GetDiagnostics().Should().BeEmpty();
        return configureVerify(
            Verifier.Verify(CreateResultFromRun(driver, expectedFileCount))
                .UseDirectory("Snapshots")
                .ScrubLinesContaining(StringComparison.Ordinal, ".GeneratedCode(\"Ossendorf.Csla.Dataportal")
        //.AutoVerify()
        );
    }

    private static RunResultWithIgnoreList CreateResultFromRun(GeneratorDriver driver, int expectedFileCount) {
        var result = driver.GetRunResult();
        _ = result.GeneratedTrees.Length.Should().Be(expectedFileCount, "The generated code must contain the attribute and the generated extension class.");
        return new RunResultWithIgnoreList {
            Result = result,
            IgnoredFiles = { "DataPortalExtensionsAttribute.g.cs" }
        };
    }

    public static void Diagnostic(string portalExtensionClass, string expectedDiagnosticId) 
        => Diagnostic(portalExtensionClass, "", expectedDiagnosticId);

    public static void Diagnostic(string portalExtensionClass, string cslaClass, string expectedDiagnosticId) {
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
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

        var generator = new DataPortalExtensionGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);
        diagnostics.Should().OnlyContain(d => d.Id == expectedDiagnosticId);
    }
}