﻿using Csla;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Ossendorf.Csla.DataPortalExtensionGenerator.Tests;

internal class TestHelper {

    private const string ClassToGenerateExtensionsInto = @$"
namespace GeneratorTests {{
    [Ossendorf.Csla.DataPortalExtensionGenerator.DataPortalExtensionsAttribute]
    public static partial class DataPortalExtensions {{
    }}
}}";

    public static Task Verify(string cslaSource) => Verify(cslaSource, true);

    public static Task Verify(string cslaSource, TestAnalyzerConfigOptionsProvider globalCompilerOptions) => Verify(cslaSource, "", t => t, 2, true, globalCompilerOptions);

    public static Task Verify(string cslaSource, bool enableNullableContext) => Verify(cslaSource, "", enableNullableContext);

    public static Task Verify(string cslaSource, string additionalSource) => Verify(cslaSource, additionalSource, true);

    public static Task Verify(string cslaSource, string additionalSource, bool enableNullableContext) => Verify(cslaSource, additionalSource, 2, enableNullableContext);

    public static Task Verify(string cslaSource, string additionalSource, int expectedFileCount) => Verify(cslaSource, additionalSource, expectedFileCount, true);

    public static Task Verify(string cslaSource, string additionalSource, int expectedFileCount, bool enableNullableContext) => Verify(cslaSource, additionalSource, s => s, expectedFileCount, enableNullableContext);

    public static Task Verify(string cslaSource, Func<SettingsTask, SettingsTask> configureVerify) => Verify(cslaSource, "", configureVerify, 2, true);

    public static Task Verify(string cslaSource, string additionalSource, Func<SettingsTask, SettingsTask> configureVerify, int expectedFileCount, bool enableNullableContext, TestAnalyzerConfigOptionsProvider? globalCompilerOptions = null) {

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
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(enableNullableContext ? NullableContextOptions.Enable : NullableContextOptions.Disable)
            );

        var generator = new DataPortalExtensionGenerator();

        var driver = CSharpGeneratorDriver.Create(generator).WithUpdatedAnalyzerConfigOptions(globalCompilerOptions ?? TestAnalyzerConfigOptionsProvider.Empty);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        using (new AssertionScope()) {
            outputCompilation.GetDiagnostics().Should().BeEmpty();
            diagnostics.Should().BeEmpty();
        }

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

    public static void Diagnostic(string portalExtensionClass, string cslaClass, string expectedDiagnosticId) 
        => Diagnostic(portalExtensionClass, cslaClass, null, diagnostics => diagnostics.Should().OnlyContain(d => d.Id == expectedDiagnosticId), d => { });

    public static void Diagnostic(string validExtensionClass, string cslaClass, string expectedDiagnosticId, TestAnalyzerConfigOptionsProvider globalCompilerOptions)
        => Diagnostic(validExtensionClass, cslaClass, globalCompilerOptions, diagnostics => diagnostics.Should().OnlyContain(d => d.Id == expectedDiagnosticId), d => { });

    public static void Diagnostic(string portalExtensionClass, string cslaClass, AnalyzerConfigOptionsProvider? globalConfigOptionsProvider, Action<IEnumerable<Diagnostic>> sourceGenReportedDiagnostics, Action<IEnumerable<Diagnostic>> compilerReportedDiagnostics, NullableContextOptions nullableContextOptions = NullableContextOptions.Enable) {
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