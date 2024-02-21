using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Ossendorf.Csla.DataPortalExtensionGenerator.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Ossendorf.Csla.DataPortalExtensionGenerator;
internal static class Parser {
    #region Extension class

    public static Result<ClassForExtensions> GetExtensionClass(GeneratorAttributeSyntaxContext ctx, CancellationToken ct) {
        _ = ct;

        var hasPartialModifier = false;
        var classSyntax = (ClassDeclarationSyntax)ctx.TargetNode;
        for (var i = 0; i < classSyntax.Modifiers.Count; i++) {
            if (classSyntax.Modifiers[i].IsKind(SyntaxKind.PartialKeyword)) {
                hasPartialModifier = true;
                break;
            }
        }

        EquatableArray<DiagnosticInfo> errors;
        if (!hasPartialModifier) {
            errors = new EquatableArray<DiagnosticInfo>([NotPartialDiagnostic.Create(classSyntax)]);
        } else {
            errors = default;
        }

        var classSymbol = (INamedTypeSymbol)ctx.TargetSymbol;
        var @namespace = classSymbol.ContainingNamespace.IsGlobalNamespace ? "" : classSymbol.ContainingNamespace.ToString();
        var name = classSymbol.Name;

        return new Result<ClassForExtensions>(new ClassForExtensions(name, @namespace, hasPartialModifier), errors);
    }

    #endregion

    #region Csla methods

    public static bool CouldBeCslaDataPortalAttribute(SyntaxNode node, CancellationToken _) {

        if (node is not AttributeSyntax attribute) {
            return false;
        }

        var name = GeneratorHelper.ExtractAttributeName(attribute.Name);

        return name is not null && GeneratorHelper.RecognizedCslaDataPortalAttributes.Keys.Contains(name);
    }

    public static Result<(PortalOperationToGenerate PortalOperationToGenerate, bool IsValid)> GetPortalMethods(GeneratorSyntaxContext ctx, CancellationToken ct) {
        var attributeSyntax = (AttributeSyntax)ctx.Node;

        if (attributeSyntax.Parent?.Parent is not MethodDeclarationSyntax methodDeclaration) {
            return Result<PortalOperationToGenerate>.NotValid();
        }

        ct.ThrowIfCancellationRequested();

        if (ctx.SemanticModel.GetTypeInfo(attributeSyntax).Type is not { ContainingNamespace.Name: "Csla" } attributeTypeInfo) {
            return Result<PortalOperationToGenerate>.NotValid();
        }

        ct.ThrowIfCancellationRequested();

        if (!GeneratorHelper.RecognizedCslaDataPortalAttributes.TryGetValue(attributeTypeInfo.Name, out var dataPortalMethod)) {
            return Result<PortalOperationToGenerate>.NotValid();
        }

        ct.ThrowIfCancellationRequested();

        if (!GetPortalObject(methodDeclaration.Parent, ctx.SemanticModel, ct, out var portalObject)) {
            return Result<PortalOperationToGenerate>.NotValid();
        }

        var diagnostics = new List<DiagnosticInfo>();
        var parameters = GetRelevantParametersForMethod(methodDeclaration, ctx.SemanticModel, ct, dataPortalMethod, diagnostics);
        var errors = new EquatableArray<DiagnosticInfo>([.. diagnostics]);

        var hasNullableEnabled = false;
        var methodName = methodDeclaration.Identifier.ToString();
        return new Result<(PortalOperationToGenerate PortalOperationToGenerate, bool IsValid)>((new PortalOperationToGenerate(methodName, parameters, hasNullableEnabled, dataPortalMethod, portalObject.Value), true), errors);
    }

    private static bool GetPortalObject(SyntaxNode? classDeclarationNode, SemanticModel semanticModel, CancellationToken cancellationToken, [NotNullWhen(true)] out PortalObject? portalObject) {

        if (classDeclarationNode is not ClassDeclarationSyntax classDeclaration) {
            portalObject = null;
            return false;
        }

        if (semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken) is not INamedTypeSymbol classSymbol || classSymbol.ContainingNamespace.IsGlobalNamespace) {
            portalObject = null;
            return false;
        }

        var objectHasPublicModifier = classDeclaration.Modifiers.Any(x => x.ToString().Equals("public", StringComparison.OrdinalIgnoreCase));
        portalObject = new PortalObject(objectHasPublicModifier, $"global::{classSymbol}");
        return true;
    }

    private static EquatableArray<OperationParameter> GetRelevantParametersForMethod(MethodDeclarationSyntax methodSyntax, SemanticModel semanticModel, CancellationToken ct, DataPortalMethod dataPortalMethod, List<DiagnosticInfo> diagnostics) {
        var methodParameters = methodSyntax.ParameterList.Parameters;
        if (methodParameters.Count == 0) {
            return EquatableArray<OperationParameter>.Empty;
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

            var hasPublicModifier = HasPublicVisibility(parameterTypeSymbol, diagnostics, dataPortalMethod, methodSyntax);
            var parametersFormattedForUsage = FormatForParameterUsage(parameter, parameterTypeSymbol);
            foundParameters.Add(new OperationParameter(parameterTypeSymbol.ContainingNamespace?.ToString() ?? "", parameter.Identifier.ToString(), parametersFormattedForUsage, hasPublicModifier));
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
}