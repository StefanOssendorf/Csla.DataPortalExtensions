﻿using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.CodeAnalysis;

namespace Ossendorf.Csla.DataPortalExtensionGenerator.Tests.Helper;

internal static class SourceGenTester {

    public static Task Verify(string cslaSource) => Verify(cslaSource, true);

    public static Task Verify(string cslaSource, TestAnalyzerConfigOptionsProvider globalCompilerOptions) => Verify(cslaSource, "", t => t, true, globalCompilerOptions, []);

    public static Task Verify(string cslaSource, bool enableNullableContext) => Verify(cslaSource, "", enableNullableContext);

    public static Task Verify(string cslaSource, string additionalSource) => Verify(cslaSource, additionalSource, true);

    public static Task Verify(string cslaSource, string additionalSource, IEnumerable<string> generatorFilesToIgnore) => Verify(cslaSource, additionalSource, s => s, true, TestAnalyzerConfigOptionsProvider.Empty, generatorFilesToIgnore);

    public static Task Verify(string cslaSource, string additionalSource, bool enableNullableContext) => Verify(cslaSource, additionalSource, s => s, enableNullableContext, TestAnalyzerConfigOptionsProvider.Empty, []);

    public static Task Verify(string cslaSource, Func<SettingsTask, SettingsTask> configureVerify) => Verify(cslaSource, "", configureVerify, true, TestAnalyzerConfigOptionsProvider.Empty, []);

    public static Task Verify(string cslaSource, string additionalSource, Func<SettingsTask, SettingsTask> configureVerify, bool enableNullableContext, TestAnalyzerConfigOptionsProvider globalCompilerOptions, IEnumerable<string> generatorFilesToIgnore) {

        var (driver, compilation) = TestHelper.SetupSourceGenerator(cslaSource, additionalSource, enableNullableContext, globalCompilerOptions);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        using (new AssertionScope()) {
            outputCompilation.GetDiagnostics().Should().BeEmpty();
            diagnostics.Should().BeEmpty();
        }

        var relativeSnapshotPath = Path.Combine("..", "Snapshots");
        return configureVerify(
            Verifier.Verify(CreateResultFromRun(driver, generatorFilesToIgnore))
                .UseDirectory(relativeSnapshotPath)
                .ScrubLinesContaining(StringComparison.Ordinal, ".GeneratedCode(\"Ossendorf.Csla.Dataportal")
            //.AutoVerify()
        );
    }

    private static RunResultWithIgnoreList CreateResultFromRun(GeneratorDriver driver, IEnumerable<string> generatorFilesToIgnore) {
        var filesToIgnore = new HashSet<string>(generatorFilesToIgnore) {
            "DataPortalExtensionsAttribute.g.cs",
            "GeneratorTests.DataPortalExtensions_CodeGenIndicationAttributes.g.cs"
        };

        var result = driver.GetRunResult();
        return new RunResultWithIgnoreList {
            Result = result,
            IgnoredFiles = filesToIgnore
        };
    }
}