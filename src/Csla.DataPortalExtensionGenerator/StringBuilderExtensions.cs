using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Ossendorf.Csla.DataPortalExtensionGenerator.Configuration;
using Ossendorf.Csla.DataPortalExtensionGenerator.Internals;
using System.Collections.Immutable;
using System.Text;

namespace Ossendorf.Csla.DataPortalExtensionGenerator;

internal static class StringBuilderExtensions {
    private const string Intendation = "    ";
    private const string TwoIntendations = $"{Intendation}{Intendation}";
    private const string ThisDataPortalArgumentName = "__dpeg_source";

    private static SymbolDisplayFormat FullyQualifiedFormat { get; } = SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    public static StringBuilder AppendMethodsGroupedByClass(this StringBuilder sb, in ImmutableArray<PortalOperationToGenerate> foundOperations, in GeneratorOptions options, CancellationToken ct) {

        var groupedByClass = foundOperations.Cast<PortalOperationToGenerate>().GroupBy(o => o.Object).ToImmutableArray();

        foreach (var operationsByClass in groupedByClass) {
            ct.ThrowIfCancellationRequested();

            if (operationsByClass.Key.IsAbstract) {
                continue;
            }

            var effectiveOperationsOfClass = GetOperationsOfClass(operationsByClass, groupedByClass);
            if (effectiveOperationsOfClass.IsDefaultOrEmpty) {
                continue;
            }

            var boName = operationsByClass.Key.GloballyQualifiedName;

            foreach (var operation in effectiveOperationsOfClass) {
                ct.ThrowIfCancellationRequested();

                var childPrefix = IsChildMethod(operation.PortalMethod) ? "Child" : "";
                string returnType;
                if (operation.PortalMethod is DataPortalMethod.Delete) {
                    returnType = "Task";
                } else {
                    returnType = $"Task<{boName}>";
                }

                var (parameters, arguments) = GetParametersAndArgumentsToUse(operation.Parameters, ct);

                var visibilityModifier = GetVisibilityModifier(operationsByClass.Key, operation);

                _ = sb.Append(TwoIntendations)
                    .Append(visibilityModifier).Append(" static ")
                    .Append("global::System.Threading.Tasks.").Append(returnType).Append(" ").Append(options.MethodPrefix).Append(operation.MethodName).Append(options.MethodSuffix)
                    .Append("(this global::Csla.I").Append(childPrefix).Append("DataPortal<").Append(boName).Append("> ")
                    .Append(ThisDataPortalArgumentName).Append(parameters).Append(")")
                    .Append(" => ").Append(ThisDataPortalArgumentName).Append(".").Append(operation.PortalMethod.ToStringFast()).Append("Async")
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

    private static ImmutableArray<PortalOperationToGenerate> GetOperationsOfClass(IGrouping<PortalObject, PortalOperationToGenerate> operationsByClass, ImmutableArray<IGrouping<PortalObject, PortalOperationToGenerate>> groupedByClass) {
        return
        [
            .. operationsByClass,
            .. GetOperationsOfBaseClass(operationsByClass.Key, operationsByClass.Key.BaseClass, groupedByClass),
        ];

        static IEnumerable<PortalOperationToGenerate> GetOperationsOfBaseClass(PortalObject actualClass, string baseClassName, ImmutableArray<IGrouping<PortalObject, PortalOperationToGenerate>> knownClasses) {
            if (string.IsNullOrWhiteSpace(baseClassName)) {
                yield break;
            }

            var methodsOfBaseClass = knownClasses.FirstOrDefault(x => x.Key.GloballyQualifiedName == baseClassName);
            if (methodsOfBaseClass is null) {
                yield break;
            }

            foreach (var item in methodsOfBaseClass) {
                yield return new PortalOperationToGenerate(actualClass, item);
            }

            foreach (var item in GetOperationsOfBaseClass(actualClass, methodsOfBaseClass.Key.BaseClass, knownClasses)) {
                yield return item;
            }
        }
    }

    private static string GetVisibilityModifier(PortalObject operationsByClass, PortalOperationToGenerate operation)
        => operationsByClass.HasPublicModifier && operation.Parameters.All(p => p.IsPublic) ? "public" : "internal";

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
    }

    public static StringBuilder AddClassContent(this StringBuilder sb, Func<StringBuilder, StringBuilder> addClassContent) => addClassContent(sb);

    public static StringBuilder AppendCreateAndExecuteMethods(this StringBuilder sb, ImmutableArray<PortalOperationToGenerate> executes, ImmutableArray<PortalOperationToGenerate> creates, GeneratorOptions options, CancellationToken ct) {
        var groupedCreates = creates.GroupBy(c => c.Object).ToImmutableDictionary(k => k.Key, v => v.ToImmutableArray());
        var groupedExecutes = executes.GroupBy(e => e.Object).ToImmutableDictionary(k => k.Key, v => v.ToImmutableArray());

        const string thisCommandArgumentName = "__dpeg_command";
        const string localVariableCommandName = "__dpeg_tmp_cmd";
        foreach (var executesOfClass in groupedExecutes) {
            ct.ThrowIfCancellationRequested();
            if (!groupedCreates.TryGetValue(executesOfClass.Key, out var createsOfClass)) {
                continue;
            }

            var boName = executesOfClass.Key.GloballyQualifiedName;

            for (var i = 0; i < executesOfClass.Value.Length; i++) {
                var executeOfClass = executesOfClass.Value[i];

                if (i == 0) {
                    var visibilityModifier = GetVisibilityModifier(executesOfClass.Key, default);

                    _ = sb.Append(TwoIntendations)
                            .Append(visibilityModifier).Append(" static ")
                            .Append("global::System.Threading.Tasks.Task<").Append(boName).Append("> ").AppendMethodName(executeOfClass, options)
                            .Append("(this global::Csla.IDataPortal<").Append(boName).Append("> ")
                            .Append(ThisDataPortalArgumentName).Append(", ").Append(boName).Append(" ").Append(thisCommandArgumentName).Append(")")
                            .Append(" => ").Append(ThisDataPortalArgumentName).Append(".").Append(DataPortalMethod.Execute.ToStringFast()).Append("Async(").Append(thisCommandArgumentName).Append(");")
                            .AppendLine();
                }

                ct.ThrowIfCancellationRequested();
                foreach (var createOfClass in createsOfClass) {
                    ct.ThrowIfCancellationRequested();

                    if (executeOfClass.Parameters.Count != 0) {
                        continue;
                    }

                    var visibilityModifier = GetVisibilityModifier(executesOfClass.Key, createOfClass);
                    var (createParameters, createArguments) = GetParametersAndArgumentsToUse(createOfClass.Parameters, ct);
                    
                    _ = sb.Append(TwoIntendations)
                        .Append(visibilityModifier).Append(" static async ")
                        .Append("global::System.Threading.Tasks.Task<").Append(boName).Append("> ").AppendMethodName("CreateAndExecuteCommand", options)
                        .Append("(this global::Csla.IDataPortal<").Append(boName).Append("> ").Append(ThisDataPortalArgumentName).Append(createParameters).AppendLine(") {")
                        .Append(TwoIntendations).Append(Intendation).Append("var ").Append(localVariableCommandName).Append(" = await ").Append(ThisDataPortalArgumentName).Append(".").AppendMethodName(createOfClass, options).Append("(")
                        .Append(createArguments).AppendLine(");")
                        .Append(TwoIntendations).Append(Intendation).Append("return await ").Append(ThisDataPortalArgumentName).Append(".").AppendMethodName(executeOfClass, options)
                        .Append("(").Append(localVariableCommandName).AppendLine(");")
                        .Append(TwoIntendations).AppendLine("}");
                }
            }
        }

        ct.ThrowIfCancellationRequested();

        return sb;
    }

    private static StringBuilder AppendMethodName(this StringBuilder sb, PortalOperationToGenerate portalOperationToGenerate, GeneratorOptions options)
        => sb.AppendMethodName(portalOperationToGenerate.MethodName, options);

    private static StringBuilder AppendMethodName(this StringBuilder sb, string methodName, GeneratorOptions options)
        => sb.Append(options.MethodPrefix).Append(methodName).Append(options.MethodSuffix);

}