using Csla;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Ossendorf.Csla.DataPortalExtensionGenerator.Internals;
using System.Collections;
using System.Collections.Immutable;
using System.Reflection;

namespace Ossendorf.Csla.DataPortalExtensionGenerator.Tests.Helper;

internal class TestHelper {

    private const string ClassToGenerateExtensionsInto = @$"
namespace GeneratorTests {{
    [Ossendorf.Csla.DataPortalExtensionGenerator.DataPortalExtensionsAttribute]
    public static partial class DataPortalExtensions {{
    }}
}}";

    public static Task Verify(string cslaSource) => Verify(cslaSource, true);

    public static Task Verify(string cslaSource, TestAnalyzerConfigOptionsProvider globalCompilerOptions) => Verify(cslaSource, "", t => t, true, globalCompilerOptions, []);

    public static Task Verify(string cslaSource, bool enableNullableContext) => Verify(cslaSource, "", enableNullableContext);

    public static Task Verify(string cslaSource, string additionalSource) => Verify(cslaSource, additionalSource, true);

    public static Task Verify(string cslaSource, string additionalSource, IEnumerable<string> generatorFilesToIgnore) => Verify(cslaSource, additionalSource, s => s, true, TestAnalyzerConfigOptionsProvider.Empty, generatorFilesToIgnore);

    public static Task Verify(string cslaSource, string additionalSource, bool enableNullableContext) => Verify(cslaSource, additionalSource, s => s, enableNullableContext, TestAnalyzerConfigOptionsProvider.Empty, []);

    public static Task Verify(string cslaSource, Func<SettingsTask, SettingsTask> configureVerify) => Verify(cslaSource, "", configureVerify, true, TestAnalyzerConfigOptionsProvider.Empty, []);

    public static Task Verify(string cslaSource, string additionalSource, Func<SettingsTask, SettingsTask> configureVerify, bool enableNullableContext, TestAnalyzerConfigOptionsProvider globalCompilerOptions, IEnumerable<string> generatorFilesToIgnore) {

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

        var clonedCompilation = compilation.Clone();

        var trackingNames = typeof(TrackingNames)
                        .GetFields()
                        .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string))
                        .Select(x => (string)x.GetRawConstantValue())
                        .Where(x => !string.IsNullOrEmpty(x))
                        .ToArray();

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var runResult1 = driver.GetRunResult();
        var runResult2 = driver.RunGenerators(clonedCompilation).GetRunResult();
        AssertRunsEqual(runResult1, runResult2, trackingNames!);
        runResult2.Results[0]
                    .TrackedOutputSteps
                    .SelectMany(x => x.Value) // step executions
                    .SelectMany(x => x.Outputs) // execution results
                    .Should()
                    .OnlyContain(x => x.Reason == IncrementalStepRunReason.Cached);

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

    public static Task VerfiyStageCaching() {

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

    private static void AssertRunsEqual(GeneratorDriverRunResult runResult1, GeneratorDriverRunResult runResult2, string[] trackingNames) {
        // We're given all the tracking names, but not all the
        // stages will necessarily execute, so extract all the 
        // output steps, and filter to ones we know about
        var trackedSteps1 = GetTrackedSteps(runResult1, trackingNames);
        var trackedSteps2 = GetTrackedSteps(runResult2, trackingNames);

        // Both runs should have the same tracked steps
        trackedSteps1.Should()
                     .NotBeEmpty()
                     .And.HaveSameCount(trackedSteps2)
                     .And.ContainKeys(trackedSteps2.Keys);

        // Get the IncrementalGeneratorRunStep collection for each run
        foreach (var (trackingName, runSteps1) in trackedSteps1) {
            // Assert that both runs produced the same outputs
            var runSteps2 = trackedSteps2[trackingName];
            AssertEqual(runSteps1, runSteps2, trackingName);
        }

        // Local function that extracts the tracked steps
        static Dictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> GetTrackedSteps(GeneratorDriverRunResult runResult, string[] trackingNames)
            => runResult
                    .Results[0] // We're only running a single generator, so this is safe
                    .TrackedSteps // Get the pipeline outputs
                    .Where(step => trackingNames.Contains(step.Key)) // filter to known steps
                    .ToDictionary(x => x.Key, x => x.Value); // Convert to a dictionary
    }

    private static void AssertEqual(ImmutableArray<IncrementalGeneratorRunStep> runSteps1, ImmutableArray<IncrementalGeneratorRunStep> runSteps2, string stepName) {
        runSteps1.Should().HaveSameCount(runSteps2);

        for (var i = 0; i < runSteps1.Length; i++) {
            var runStep1 = runSteps1[i];
            var runStep2 = runSteps2[i];

            // The outputs should be equal between different runs
            IEnumerable<object> outputs1 = runStep1.Outputs.Select(x => x.Value);
            IEnumerable<object> outputs2 = runStep2.Outputs.Select(x => x.Value);

            outputs1.Should()
                    .Equal(outputs2, $"because {stepName} should produce cacheable outputs");

            // Therefore, on the second run the results should always be cached or unchanged!
            // - Unchanged is when the _input_ has changed, but the output hasn't
            // - Cached is when the the input has not changed, so the cached output is used 
            runStep2.Outputs.Should()
                .OnlyContain(
                    x => x.Reason == IncrementalStepRunReason.Cached || x.Reason == IncrementalStepRunReason.Unchanged,
                    $"{stepName} expected to have reason {IncrementalStepRunReason.Cached} or {IncrementalStepRunReason.Unchanged}");

            // Make sure we're not using anything we shouldn't
            AssertObjectGraph(runStep1, stepName);
        }
    }

    private static void AssertObjectGraph(IncrementalGeneratorRunStep runStep, string stepName) {
        // Including the stepName in error messages to make it easy to isolate issues
        var because = $"{stepName} shouldn't contain banned symbols";
        var visited = new HashSet<object>();

        // Check all of the outputs - probably overkill, but why not
        foreach (var (obj, _) in runStep.Outputs) {
            Visit(obj);
        }

        void Visit(object node) {
            // If we've already seen this object, or it's null, stop.
            if (node is null || !visited.Add(node)) {
                return;
            }

            // Make sure it's not a banned type
            node.Should()
                .NotBeOfType<Compilation>(because)
                .And.NotBeOfType<ISymbol>(because)
                .And.NotBeOfType<SyntaxNode>(because);

            // Examine the object
            Type type = node.GetType();
            if (type.IsPrimitive || type.IsEnum || type == typeof(string)) {
                return;
            }

            // If the object is a collection, check each of the values
            if (node is IEnumerable collection and not string) {
                foreach (object element in collection) {
                    // recursively check each element in the collection
                    Visit(element);
                }

                return;
            }

            // Recursively check each field in the object
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
                object fieldValue = field.GetValue(node);
                Visit(fieldValue);
            }
        }
    }
}