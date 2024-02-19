using Microsoft.CodeAnalysis;
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
            .Collect()
            ;

        var methodDeclarationsAndDiagnostics = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: Parser.CouldBeCslaDataPortalAttribute,
                transform: GetMethodInfoForGeneration
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

    #region Csla methods

    

    private static Result<(PortalOperationToGenerate PortalOperationToGenerate, bool IsValid)> GetMethodInfoForGeneration(GeneratorSyntaxContext ctx, CancellationToken ct) {
        var attributeSyntax = (AttributeSyntax)ctx.Node;

        if (attributeSyntax.Parent?.Parent is not MethodDeclarationSyntax methodDeclaration) {
            return Result<PortalOperationToGenerate>.NotValid();
        }

        if (methodDeclaration.Parent is not ClassDeclarationSyntax classDeclaration) {
            return Result<PortalOperationToGenerate>.NotValid();
        }

        ct.ThrowIfCancellationRequested();

        if (ctx.SemanticModel.GetTypeInfo(attributeSyntax).Type is not { ContainingNamespace.Name: "Csla" } attributeTypeInfo) {
            return Result<PortalOperationToGenerate>.NotValid();
        }

        ct.ThrowIfCancellationRequested();

        if (ctx.SemanticModel.GetDeclaredSymbol(methodDeclaration, ct) is not IMethodSymbol methodSymbol) {
            return Result<PortalOperationToGenerate>.NotValid();
        }

        ct.ThrowIfCancellationRequested();

        if (ctx.SemanticModel.GetDeclaredSymbol(classDeclaration, ct) is not INamedTypeSymbol classSymbol || classSymbol.ContainingNamespace.IsGlobalNamespace) {
            return Result<PortalOperationToGenerate>.NotValid();
        }

        ct.ThrowIfCancellationRequested();

        if (!GeneratorHelper.RecognizedCslaDataPortalAttributes.TryGetValue(attributeTypeInfo.Name, out var dataPortalMethod)) {
            return Result<PortalOperationToGenerate>.NotValid();
        }

        ct.ThrowIfCancellationRequested();

        var objectHasPublicModifier = classDeclaration.Modifiers.Any(x => x.ToString().Equals("public", StringComparison.OrdinalIgnoreCase));
        var methodName = methodDeclaration.Identifier.ToString();

        var hasNullableEnabled = false; // methodSymbol.ReceiverNullableAnnotation != NullableAnnotation.None;
        var (parameters, diagnostics) = GetRelevantParametersForMethod(methodDeclaration, ctx.SemanticModel, ct, dataPortalMethod);

        var objectContainingPortalMethod = new PortalObject(objectHasPublicModifier, $"global::{classSymbol}");

        var errors = new EquatableArray<DiagnosticInfo>([.. diagnostics]);
        return new Result<(PortalOperationToGenerate PortalOperationToGenerate, bool IsValid)>((new PortalOperationToGenerate(methodName, parameters, hasNullableEnabled, dataPortalMethod, objectContainingPortalMethod), true), errors);
    }

    private static (EquatableArray<OperationParameter>, List<DiagnosticInfo>) GetRelevantParametersForMethod(MethodDeclarationSyntax methodSyntax, SemanticModel semanticModel, CancellationToken ct, DataPortalMethod dataPortalMethod) {
        var methodParameters = methodSyntax.ParameterList.Parameters;
        if (methodParameters.Count == 0) {
            return (EquatableArray<OperationParameter>.Empty, []);
        }

        var diagnostics = new List<DiagnosticInfo>();
        var foundParameters = new List<OperationParameter>();
        for (var i = 0; i < methodParameters.Count; i++) {
            ct.ThrowIfCancellationRequested();

            var parameter = methodParameters[i];

            if (IsInjectedParameter(parameter) || parameter.Type is null) {
                continue;
            }

            if (semanticModel.GetTypeInfo(parameter.Type).Type is not ITypeSymbol parameterTypeSymbol) {
                continue;
            }

            var hasPublicModifier = HasPublicVisibility(parameterTypeSymbol, diagnostics, dataPortalMethod, methodSyntax);
            (var parametersFormattedForUsage, var errors) = FormatForParameterUsage(parameter, parameterTypeSymbol);
            diagnostics.AddRange(errors);
            foundParameters.Add(new OperationParameter(parameterTypeSymbol.ContainingNamespace?.ToString() ?? "", parameter.Identifier.ToString(), parametersFormattedForUsage, hasPublicModifier));
        }

        return (new EquatableArray<OperationParameter>([.. foundParameters]), diagnostics);

        static bool IsInjectedParameter(ParameterSyntax parameter) {
            for (var i = 0; i < parameter.AttributeLists.Count; i++) {
                var al = parameter.AttributeLists[i];
                for (var j = 0; j < al.Attributes.Count; j++) {
                    var attr = al.Attributes[j];

                    var attrName = EnsureAttributeSuffix(GeneratorHelper.ExtractAttributeName(attr.Name));
                    if (attrName.Equals("InjectAttribute", StringComparison.Ordinal)) {
                        return true;
                    }
                }
            }

            return false;

            static string EnsureAttributeSuffix(string attributeName) {
                if (string.IsNullOrWhiteSpace(attributeName)) {
                    return "";
                }

                if (attributeName.EndsWith("Attribute")) {
                    return attributeName;
                }

                return $"{attributeName}Attribute";
            }
        }

        static bool HasPublicVisibility(ITypeSymbol typeSymbol, List<DiagnosticInfo> diagnostics, DataPortalMethod dataPortalMethod, MethodDeclarationSyntax methodSyntax) {
            if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol) {
                typeSymbol = arrayTypeSymbol.ElementType;
            }

            if (typeSymbol.DeclaredAccessibility == Accessibility.Private) {
                diagnostics.Add(PrivateClassCanNotBeAParameterDiagnostic.Create(methodSyntax, dataPortalMethod, methodSyntax.Identifier.ToString(), typeSymbol.ToDisplayString()));
            }

            return typeSymbol.DeclaredAccessibility == Accessibility.Public;
        }
    }

    private static (string, IEnumerable<DiagnosticInfo>) FormatForParameterUsage(ParameterSyntax parameter, ITypeSymbol parameterTypeSymbol) {
        var typeString = parameterTypeSymbol.ToString();
        switch (typeString) {
            case "string":
            case "string[]":
            case "bool":
            case "bool[]":
            case "bool?":
            case "byte":
            case "byte[]":
            case "byte?":
            case "sbyte":
            case "sbyte[]":
            case "sbyte?":
            case "char":
            case "char[]":
            case "char?":
            case "decimal":
            case "decimal[]":
            case "decimal?":
            case "double":
            case "double[]":
            case "double?":
            case "float":
            case "float[]":
            case "float?":
            case "int":
            case "int[]":
            case "int?":
            case "uint":
            case "uint[]":
            case "uint?":
            case "long":
            case "long[]":
            case "long?":
            case "ulong":
            case "ulong[]":
            case "ulong?":
            case "short":
            case "short[]":
            case "short?":
            case "ushort":
            case "ushort[]":
            case "ushort?":
            case "object":
            case "object[]":
                return (parameter.ToString(), Array.Empty<DiagnosticInfo>());
        }

        var typeStringBuilder = GetTypeString(parameterTypeSymbol);

        var parameterVariableName = parameter.Identifier.ToString();
        if (parameter.Default is not null) {
            if (parameterTypeSymbol is { TypeKind: TypeKind.Enum } && parameter.Default.Value is MemberAccessExpressionSyntax valueOfEnum) {
                parameterVariableName += $" = {typeStringBuilder}.{valueOfEnum.Name}";
            } else {
                parameterVariableName += $" {parameter.Default}";
            }
            //const int NullLiteralExpression = 8754; // See https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.syntaxkind?view=roslyn-dotnet-4.7.0#microsoft-codeanalysis-csharp-syntaxkind-nullliteralexpression
            //if (parameter.Default is EqualsValueClauseSyntax { Value.RawKind: NullLiteralExpression }) {
        }

        return ($"{typeStringBuilder} {parameterVariableName}", Array.Empty<DiagnosticInfo>());

        static StringBuilder GetTypeString(ITypeSymbol typeSymbol, StringBuilder? sb = null) {
            sb ??= new();

            sb.Append("global::");
            if (!typeSymbol.ContainingNamespace.IsGlobalNamespace) {
                sb.Append(typeSymbol.ContainingNamespace).Append(".");
            }

            sb.Append(GetTypeWithHierarchy(typeSymbol));

            if (typeSymbol is INamedTypeSymbol { TypeArguments.Length: > 0 } namedTypeSymbol) {
                sb.Append("<");

                for (var i = 0; i < namedTypeSymbol.TypeArguments.Length; i++) {
                    if (i > 0) {
                        sb.Append(", ");
                    }

                    sb = GetTypeString(namedTypeSymbol.TypeArguments[i], sb);
                }

                sb.Append(">");
            }

            return sb;

            static StringBuilder GetTypeWithHierarchy(ITypeSymbol? typeSymbol, StringBuilder? sb = null) {
                if (typeSymbol is null) {
                    return sb ?? new();
                }

                sb ??= new();

                if (sb.Length > 0) {
                    sb.Insert(0, ".");
                }

                sb.Insert(0, typeSymbol.Name);

                return GetTypeWithHierarchy(typeSymbol.ContainingType, sb);
            }
        }
    }

    #endregion

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