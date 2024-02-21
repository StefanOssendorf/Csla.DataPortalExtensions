﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Ossendorf.Csla.DataPortalExtensionGenerator.Diagnostics;
using System.Collections.Immutable;
using System.Text;

namespace Ossendorf.Csla.DataPortalExtensionGenerator;

/// <summary>
/// The data portal extension source generator.
/// </summary>
[Generator]
public sealed partial class DataPortalExtensionGenerator : IIncrementalGenerator {

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        AddMarkerAttribute(context);
        AddCodeGenerator(context);
    }

    private static void AddMarkerAttribute(IncrementalGeneratorInitializationContext context)
        => context.RegisterPostInitializationOutput(ctx => ctx.AddSource("DataPortalExtensionsAttribute.g.cs", SourceText.From(GeneratorHelper.MarkerAttribute, Encoding.UTF8)));

    private static void AddCodeGenerator(IncrementalGeneratorInitializationContext context) {
        var options = GetGeneratorOptions(context);

        var extensionClassesAndDiagnostics = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: GeneratorHelper.FullyQalifiedNameOfMarkerAttribute,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: Parser.GetExtensionClass
            );

        var extensionClassDeclaration = extensionClassesAndDiagnostics
            .Select((r, _) => r.Value)
            .Collect();

        var methodDeclarationsAndDiagnostics = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: Parser.CouldBeCslaDataPortalAttribute,
                transform: Parser.GetPortalMethods
            );

        var methodDeclarations = methodDeclarationsAndDiagnostics
            .Where(static m => m.Value.IsValid)
            .Select((m, _) => m.Value.PortalOperationToGenerate)
            .Collect();

        var classesToGenerateInto = extensionClassDeclaration.Combine(methodDeclarations).Combine(options);

        context.RegisterSourceOutput(
            extensionClassesAndDiagnostics.SelectMany((r, _) => r.Errors),
            static (ctx, info) => ctx.ReportDiagnostic(info)
        );
        context.RegisterSourceOutput(
            methodDeclarationsAndDiagnostics.SelectMany((m,_) => m.Errors),
            static (ctx, info) => ctx.ReportDiagnostic(info)
        );

        context.RegisterSourceOutput(
            classesToGenerateInto, 
            (spc, extensionClass) => GenerateExtensionMethods(spc, in extensionClass)
        );
    }

    private static IncrementalValueProvider<GeneratorOptions> GetGeneratorOptions(IncrementalGeneratorInitializationContext context) {
        return context.AnalyzerConfigOptionsProvider.Select((options, _) => {

            if (!options.GlobalOptions.TryGetValue("build_property.DataPortalExtensionGen_MethodPrefix", out var methodPrefix) || methodPrefix is null) {
                methodPrefix = "";
            }

            if (!options.GlobalOptions.TryGetValue("build_property.DataPortalExtensionGen_MethodSuffix", out var methodSuffix) || methodSuffix is null) {
                methodSuffix = "";
            }

            return new GeneratorOptions(methodPrefix, methodSuffix);
        });
    }

    private static void GenerateExtensionMethods(SourceProductionContext context, in ((ImmutableArray<ClassForExtensions> Classes, ImmutableArray<PortalOperationToGenerate> Methods) ClassesAndMethods, GeneratorOptions Options) data) {
        var classes = data.ClassesAndMethods.Classes;
        var methods = data.ClassesAndMethods.Methods;
        if (classes.IsDefaultOrEmpty || methods.IsDefaultOrEmpty) {
            return;
        }

        foreach (var extensionClass in classes) {
            context.CancellationToken.ThrowIfCancellationRequested();

            if (!extensionClass.HasPartialModifier) {
                //context.ReportDiagnostic(Diagnostic.Create( )
                continue;
            }

            var code = GenerateCode(in extensionClass, in methods, in data.Options, context.CancellationToken);
            var typeNamespace = extensionClass.Namespace;
            if (!string.IsNullOrWhiteSpace(typeNamespace)) {
                typeNamespace += ".";
            }

            context.AddSource($"{typeNamespace}{extensionClass.Name}.g.cs", code);
        }

        static string GenerateCode(in ClassForExtensions extensionClass, in ImmutableArray<PortalOperationToGenerate> methods, in GeneratorOptions options, CancellationToken ct) {

            var ns = extensionClass.Namespace;
            var name = extensionClass.Name;

            var methodString = new StringBuilder().AppendMethodsGroupedByClass(in methods, in options, ct);

            var sb = new StringBuilder()
                .AppendLine("// <auto-generated />")

                //.AppendNullableContextDependingOnTarget(/*extensionClass.NullableAnnotation*/ NullableAnnotation.None)

                .AppendLine()
                .AppendLine(string.IsNullOrWhiteSpace(ns) ? "" : $@"namespace {ns}")
                .AppendLine("{")

                .Append("    [global::System.CodeDom.Compiler.GeneratedCode(\"Ossendorf.Csla.DataportalExtensionsGenerator\", \"").Append(GeneratorHelper.VersionString).AppendLine("\")]")
                .AppendLine("    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = \"Generated by the Ossendorf.Csla.DataPortalExtensionsGenerators source generator.\")]")
                .AppendLine($"    static partial class {name}")
                .AppendLine("    {")

                .Append(methodString)

                .AppendLine("    }")
                .AppendLine("}");

            return sb.ToString();
        }
    }
}