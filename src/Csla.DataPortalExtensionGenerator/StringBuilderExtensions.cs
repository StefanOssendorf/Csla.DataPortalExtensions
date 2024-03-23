using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Ossendorf.Csla.DataPortalExtensionGenerator.Configuration;
using Ossendorf.Csla.DataPortalExtensionGenerator.Internals;
using System.Collections.Immutable;
using System.Text;

namespace Ossendorf.Csla.DataPortalExtensionGenerator;

internal static class StringBuilderExtensions {
    private static SymbolDisplayFormat FullyQualifiedFormat { get; } = SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    public static StringBuilder AppendMethodsGroupedByClass(this StringBuilder sb, in ImmutableArray<PortalOperationToGenerate> foundOperations, in GeneratorOptions options, CancellationToken ct) {
        const string intendation = "        ";
        const string thisArgumentName = "__dpeg_source";

        var groupedByClass = foundOperations.Cast<PortalOperationToGenerate>().GroupBy(o => o.Object).ToImmutableArray();

        foreach (var operationsByClass in groupedByClass) {
            ct.ThrowIfCancellationRequested();

            if (!operationsByClass.Any()) {
                continue;
            }

            var boName = operationsByClass.Key.GloballyQualifiedName;

            foreach (var operation in operationsByClass) {
                ct.ThrowIfCancellationRequested();

                var childPrefix = IsChildMethod(operation.PortalMethod) ? "Child" : "";
                string returnType;
                if (operation.PortalMethod is DataPortalMethod.Delete) {
                    returnType = "Task";
                } else {
                    returnType = $"Task<{boName}>";
                }

                var (parameters, arguments) = GetParametersAndArgumentsToUse(operation.Parameters, ct);

                var visibilityModifier = operationsByClass.Key.HasPublicModifier && operation.Parameters.All(p => p.IsPublic) ? "public" : "internal";

                _ = sb.Append(intendation)
                    .Append(visibilityModifier).Append(" static ")
                    .Append("global::System.Threading.Tasks.").Append(returnType).Append(" ").Append(options.MethodPrefix).Append(operation.MethodName).Append(options.MethodSuffix)
                    .Append("(this global::Csla.I").Append(childPrefix).Append("DataPortal<").Append(boName).Append("> ")
                    .Append(thisArgumentName).Append(parameters).Append(")")
                    .Append(" => ").Append(thisArgumentName).Append(".").Append(operation.PortalMethod.ToStringFast()).Append("Async")
                    .Append("(").Append(arguments).Append(");").AppendLine();
            }
        }

        return sb;

        static bool IsChildMethod(DataPortalMethod portalMethod) {
            return portalMethod switch {
                DataPortalMethod.FetchChild or DataPortalMethod.CreateChild => true,
                DataPortalMethod.Fetch or DataPortalMethod.Delete or DataPortalMethod.Create or DataPortalMethod.Execute => false,
                _ => throw new InvalidOperationException($"Unknown dataportal method {portalMethod}"),
            };
        }
    }

    private static (StringBuilder Parameters, StringBuilder Arguments) GetParametersAndArgumentsToUse(EquatableArray<OperationParameter> parameters, CancellationToken ct) {
        var parametersBuilder = new StringBuilder();
        var argumentsBuilder = new StringBuilder();
        if (parameters.Count == 0) {
            return (parametersBuilder, argumentsBuilder);
        }

        foreach (var parameter in parameters) {
            ct.ThrowIfCancellationRequested();

            if (parametersBuilder.Length > 0) {
                parametersBuilder.Append(", ");
                argumentsBuilder.Append(", ");
            }

            parametersBuilder.Append(parameter.ParameterFormatted);
            argumentsBuilder.Append(parameter.ArgumentFormatted);
        }

        if (parametersBuilder.Length > 0) {
            parametersBuilder.Insert(0, ", ");
        }

        return (parametersBuilder, argumentsBuilder);
    }

    public static StringBuilder AppendNamespace(this StringBuilder sb, in ClassForExtensions classForExtensions)
        => sb.AppendLine(string.IsNullOrWhiteSpace(classForExtensions.Namespace) ? "" : $@"namespace {classForExtensions.Namespace}");

    public static StringBuilder AppendClassDeclaration(this StringBuilder sb, in ClassForExtensions classForExtensions)
        => sb.AppendLine($"    static partial class {classForExtensions.Name}");

    public static StringBuilder AppendNullableContext(this StringBuilder sb, in GeneratorOptions options) {
        var directiveValue = options.NullableContextOptions.AnnotationsEnabled() ? "enable" : "disable";
        sb.AppendLine($"#nullable {directiveValue}");
        if (options.SuppressWarningCS8669) {
            sb.AppendLine("#pragma warning disable CS8669 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context. Auto-generated code requires an explicit '#nullable' directive in source.");
        }

        return sb;
    }

    public static StringBuilder AppendType(this StringBuilder sb, ITypeSymbol typeSymbol, IParameterSymbol parameterDeclaredSymbol) {
        sb.Append(typeSymbol.ToDisplayString(FullyQualifiedFormat));
        if (parameterDeclaredSymbol.NullableAnnotation == NullableAnnotation.Annotated && !(sb[^1] == '?')) {
            sb.Append("?");
        }

        return sb;
    }

    public static StringBuilder AppendVariableName(this StringBuilder sb, ParameterSyntax parameter) 
        => sb.Append(parameter.Identifier.ToString());

    public static StringBuilder AppendDefaultValue(this StringBuilder sb, ParameterSyntax parameter, ITypeSymbol parameterTypeSymbol, IParameterSymbol parameterDeclaredSymbol) {
        if (parameter.Default is null) {
            return sb;
        }

        if (parameterTypeSymbol is { TypeKind: TypeKind.Enum } && parameter.Default.Value is MemberAccessExpressionSyntax valueOfEnum) {
            sb.Append(" = ").AppendType(parameterTypeSymbol, parameterDeclaredSymbol).Append(".").Append(valueOfEnum.Name);
        } else {
            sb.Append(" ").Append(parameter.Default);
        }

        return sb;

        //const int NullLiteralExpression = 8754; // See https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.syntaxkind?view=roslyn-dotnet-4.7.0#microsoft-codeanalysis-csharp-syntaxkind-nullliteralexpression
        //if (parameter.Default is EqualsValueClauseSyntax { Value.RawKind: NullLiteralExpression }) {
    }
}