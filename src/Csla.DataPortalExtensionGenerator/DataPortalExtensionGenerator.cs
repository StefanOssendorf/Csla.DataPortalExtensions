using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace Ossendorf.Csla.DataPortalExtensionGenerator;

/// <summary>
/// The data portal extension source generator.
/// </summary>
[Generator]
public class DataPortalExtensionGenerator : IIncrementalGenerator {

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        AddMarkerAttribute(context);
        AddCodeGenerator(context);
    }

    private void AddMarkerAttribute(IncrementalGeneratorInitializationContext context)
        => context.RegisterPostInitializationOutput(ctx => ctx.AddSource("DataPortalExtensionsAttribute.g.cs", SourceText.From(GeneratorHelper.MarkerAttribute, Encoding.UTF8)));

    private void AddCodeGenerator(IncrementalGeneratorInitializationContext context) {
        var extensionClassDeclaration = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: GeneratorHelper.FullyQalifiedNameOfMarkerAttribute,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: GetClassToGenerateInto
            )
            .Collect();

        var methodDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: CouldBeCslaDataPortalAttribute,
                transform: GetMethodInfoForGeneration
            )
            .Where(m => m is not null)!
            .Collect();

        var options = GetGeneratorOptions(context);

        var classesToGenerateInto = extensionClassDeclaration.Combine(methodDeclarations).Combine(options);

        context.RegisterSourceOutput(classesToGenerateInto, (spc, extensionClass) => GenerateExtensionMethods(spc, in extensionClass));
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

    #region Extension method class

    private static ClassForExtensions GetClassToGenerateInto(GeneratorAttributeSyntaxContext ctx, CancellationToken ct) {
        _ = ct;

        var classSymbol = (INamedTypeSymbol)ctx.TargetSymbol;

        var nameSpace = classSymbol.ContainingNamespace.IsGlobalNamespace ? "" : classSymbol.ContainingNamespace.ToString();
        var name = classSymbol.Name;

        return new ClassForExtensions(name, nameSpace);
    }

    #endregion

    #region Csla methods

    private static bool CouldBeCslaDataPortalAttribute(SyntaxNode node, CancellationToken _) {

        if (node is not AttributeSyntax attribute) {
            return false;
        }

        var name = GeneratorHelper.ExtractAttributeName(attribute.Name);

        return name is not null && GeneratorHelper.RecognizedCslaDataPortalAttributes.Keys.Contains(name);
    }

    private PortalOperationToGenerate? GetMethodInfoForGeneration(GeneratorSyntaxContext ctx, CancellationToken ct) {
        var attributeSyntax = (AttributeSyntax)ctx.Node;

        if (attributeSyntax.Parent?.Parent is not MethodDeclarationSyntax methodDeclaration) {
            return null;
        }

        if (methodDeclaration.Parent is not ClassDeclarationSyntax classDeclaration) {
            return null;
        }

        ct.ThrowIfCancellationRequested();

        if (ctx.SemanticModel.GetTypeInfo(attributeSyntax).Type is not { ContainingNamespace.Name: "Csla" } attributeTypeInfo) {
            return null;
        }

        ct.ThrowIfCancellationRequested();

        if (ctx.SemanticModel.GetDeclaredSymbol(methodDeclaration, ct) is not IMethodSymbol methodSymbol) {
            return null;
        }

        ct.ThrowIfCancellationRequested();

        if (ctx.SemanticModel.GetDeclaredSymbol(classDeclaration, ct) is not INamedTypeSymbol classSymbol || classSymbol.ContainingNamespace.IsGlobalNamespace) {
            return null;
        }

        ct.ThrowIfCancellationRequested();

        if (!GeneratorHelper.RecognizedCslaDataPortalAttributes.TryGetValue(attributeTypeInfo.Name, out var dataPortalMethod)) {
            return null;
        }

        ct.ThrowIfCancellationRequested();

        var objectHasPublicModifier = classDeclaration.Modifiers.Any(x => x.ToString().Equals("public", StringComparison.OrdinalIgnoreCase));
        var methodName = methodDeclaration.Identifier.ToString();

        var hasNullableEnabled = false; // methodSymbol.ReceiverNullableAnnotation != NullableAnnotation.None;
        var parameters = GetRelevantParametersForMethod(methodDeclaration, ctx.SemanticModel, ct);

        var objectContainingPortalMethod = new PortalObject(objectHasPublicModifier, $"global::{classSymbol}");
        return new PortalOperationToGenerate(methodName, parameters, hasNullableEnabled, dataPortalMethod, objectContainingPortalMethod);
    }

    private EquatableArray<OperationParameter> GetRelevantParametersForMethod(MethodDeclarationSyntax methodSymbol, SemanticModel semanticModel, CancellationToken ct) {
        var methodParameters = methodSymbol.ParameterList.Parameters;
        if (methodParameters.Count == 0) {
            return new EquatableArray<OperationParameter>();
        }

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

            var hasPublicModifier = HasPublicVisibility(parameterTypeSymbol);
            foundParameters.Add(new OperationParameter(parameterTypeSymbol.ContainingNamespace?.ToString() ?? "", parameter.Identifier.ToString(), FormatForParameterUsage(parameter, parameterTypeSymbol), hasPublicModifier));
        }

        return new EquatableArray<OperationParameter>([.. foundParameters]);

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
        }

        static bool HasPublicVisibility(ITypeSymbol typeSymbol) {
            if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol) {
                typeSymbol = arrayTypeSymbol.ElementType;
            }

            return typeSymbol.DeclaredAccessibility == Accessibility.Public;
        }
    }

    private static string FormatForParameterUsage(ParameterSyntax parameter, ITypeSymbol parameterTypeSymbol) {
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
                return parameter.ToString();
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

        return $"{typeStringBuilder} {parameterVariableName}";

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

    private static void GenerateExtensionMethods(SourceProductionContext context, in ((ImmutableArray<ClassForExtensions> Classes, ImmutableArray<PortalOperationToGenerate?> Methods) ClassesAndMethods, GeneratorOptions Options) data) {
        var classes = data.ClassesAndMethods.Classes;
        var methods = data.ClassesAndMethods.Methods;
        if (classes.IsDefaultOrEmpty || methods.IsDefaultOrEmpty) {
            return;
        }

        foreach (var extensionClass in classes) {
            context.CancellationToken.ThrowIfCancellationRequested();

            var code = GenerateCode(in extensionClass, in methods, in data.Options, context.CancellationToken);
            var typeNamespace = extensionClass.Namespace;
            if (!string.IsNullOrWhiteSpace(typeNamespace)) {
                typeNamespace += ".";
            }

            context.AddSource($"{typeNamespace}{extensionClass.Name}.g.cs", code);
        }

        static string GenerateCode(in ClassForExtensions extensionClass, in ImmutableArray<PortalOperationToGenerate?> methods, in GeneratorOptions options, CancellationToken ct) {

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

    private static string EnsureAttributeSuffix(string attributeName) {
        if (string.IsNullOrWhiteSpace(attributeName)) {
            return "";
        }

        if (attributeName.EndsWith("Attribute")) {
            return attributeName;
        }

        return $"{attributeName}Attribute";
    }
}