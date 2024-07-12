using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Ossendorf.Csla.DataPortalExtensionGenerator.Diagnostics;
using Ossendorf.Csla.DataPortalExtensionGenerator.Internals;
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

    public static Result<(PortalOperationToGenerate PortalOperationToGenerate, bool IsValid)> GetPortalMethods(GeneratorAttributeSyntaxContext ctx, DataPortalMethod dataPortalMethod, CancellationToken ct) {

        var methodDeclaration = (MethodDeclarationSyntax)ctx.TargetNode;
        if (!GetPortalObject(ctx.TargetNode.Parent, ctx.SemanticModel, ct, out var portalObject)) {
            return Result<PortalOperationToGenerate>.NotValid();
        }

        ct.ThrowIfCancellationRequested();

        var diagnostics = new List<DiagnosticInfo>();
        var parameters = GetRelevantParametersForMethod(methodDeclaration, ctx.SemanticModel, ct, dataPortalMethod, diagnostics);
        var errors = new EquatableArray<DiagnosticInfo>([.. diagnostics]);

        var methodName = methodDeclaration.Identifier.ToString();
        return new Result<(PortalOperationToGenerate PortalOperationToGenerate, bool IsValid)>((new PortalOperationToGenerate(methodName, parameters, dataPortalMethod, portalObject.Value), true), errors);
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

            if (semanticModel.GetDeclaredSymbol(parameter, ct) is not IParameterSymbol parameterDeclaredSymbol) {
                continue;
            }

            var hasPublicModifier = HasPublicVisibility(parameterTypeSymbol, diagnostics, dataPortalMethod, methodSyntax);
            var parametersFormattedForUsage = FormatForParameterUsage(parameter, parameterTypeSymbol, parameterDeclaredSymbol);
            foundParameters.Add(new OperationParameter(parameterTypeSymbol.ContainingNamespace?.ToString() ?? "", parameter.Identifier.ToString(), parametersFormattedForUsage, hasPublicModifier));
        }

        ct.ThrowIfCancellationRequested();
        return new EquatableArray<OperationParameter>([.. foundParameters]);

        static bool IsInjectedParameter(ParameterSyntax parameter) {
            for (var i = 0; i < parameter.AttributeLists.Count; i++) {
                var al = parameter.AttributeLists[i];
                for (var j = 0; j < al.Attributes.Count; j++) {
                    var attr = al.Attributes[j];

                    var attrName = EnsureAttributeSuffix(ExtractAttributeName(attr.Name));
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

    private static string FormatForParameterUsage(ParameterSyntax parameter, ITypeSymbol parameterTypeSymbol, IParameterSymbol parameterDeclaredSymbol) {
        var typeString = parameterTypeSymbol.ToString();
#pragma warning disable IDE0066 // Convert switch statement to expression, This is more readable than the switch-expression
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
#pragma warning restore IDE0066 // Convert switch statement to expression

        return new StringBuilder()
            .AppendType(parameterTypeSymbol, parameterDeclaredSymbol)
            .Append(" ")
            .AppendVariableName(parameter)
            .AppendDefaultValue(parameter, parameterTypeSymbol, parameterDeclaredSymbol)
            .ToString();
    }

    #endregion

    private static string ExtractAttributeName(NameSyntax? name) {
        return name switch {
            SimpleNameSyntax ins => ins.Identifier.Text,
            QualifiedNameSyntax qns => qns.Right.Identifier.Text,
            _ => ""
        };
    }
}