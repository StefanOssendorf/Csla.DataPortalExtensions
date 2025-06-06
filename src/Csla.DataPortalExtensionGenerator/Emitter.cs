﻿using Microsoft.CodeAnalysis;
using Ossendorf.Csla.DataPortalExtensionGenerator.Configuration;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;

namespace Ossendorf.Csla.DataPortalExtensionGenerator;

internal static class Emitter {

    public static void EmitClassWithSourceGenIndicationAttributes(SourceProductionContext context, (ClassForExtensions ExtensionClass, GeneratorOptions Options) data) {
        var extensionClass = data.ExtensionClass;
        var typeNamespace = extensionClass.Namespace;
        if (!string.IsNullOrWhiteSpace(typeNamespace)) {
            typeNamespace += ".";
        }

        var fileName = $"{typeNamespace}{extensionClass.Name}_CodeGenIndicationAttributes.g.cs";
        var code = new StringBuilder()
            .AppendLine("// <auto-generated />")
            .AppendNamespace(in extensionClass)
            .AppendLine("{")
            .Append("    [global::System.CodeDom.Compiler.GeneratedCode(\"Ossendorf.Csla.DataportalExtensionsGenerator\", \"").Append(GeneratorHelper.VersionString).AppendLine("\")]")
            .Append("    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(")
            .AppendExcludeFromCodeCoverageJustification(data.Options)
            .AppendLine(")]")
            .AppendClassDeclaration(in extensionClass)
            .AppendLine("    {")
            .AppendLine("    }")
            .AppendLine("}")
            .ToString();
        context.AddSource(fileName, code);
    }

    public static void EmitClassForCreateAndExecute(SourceProductionContext context, (((ClassForExtensions Class, ImmutableArray<PortalOperationToGenerate> Creates) ClassAndCreates, ImmutableArray<PortalOperationToGenerate> Executes) CslaObjectData, GeneratorOptions Options) data) {
        if (data.CslaObjectData.Executes.IsDefaultOrEmpty && data.CslaObjectData.ClassAndCreates.Creates.IsDefaultOrEmpty) {
            return;
        }

        context.CancellationToken.ThrowIfCancellationRequested();

        EmitClassForAttribute(context, (data.CslaObjectData.ClassAndCreates, data.Options));
        EmitClassForAttribute(context, ((data.CslaObjectData.ClassAndCreates.Class, data.CslaObjectData.Executes), data.Options));
        EmitClassForExecuteCommand(context, data.CslaObjectData.ClassAndCreates.Class, data.CslaObjectData.ClassAndCreates.Creates, data.CslaObjectData.Executes, data.Options, context.CancellationToken);
    }

    private static void EmitClassForExecuteCommand(SourceProductionContext context, ClassForExtensions @class, ImmutableArray<PortalOperationToGenerate> creates, ImmutableArray<PortalOperationToGenerate> executes, GeneratorOptions options, CancellationToken ct) {
        if (creates.IsDefaultOrEmpty || executes.IsDefaultOrEmpty) {
            return;
        }

        context.CancellationToken.ThrowIfCancellationRequested();

        var code = GenerateCode(in @class, in options, sb => sb.AppendCreateAndExecuteMethods(executes, creates, options, ct));
        var file = GetFileName(@class, "ExecuteCommand");

        context.AddSource(file, code);
    }

    public static void EmitClassForAttribute(SourceProductionContext context, ((ClassForExtensions Class, ImmutableArray<PortalOperationToGenerate> Methods) ClassesAndMethods, GeneratorOptions Options) data) {
        var methods = data.ClassesAndMethods.Methods;
        if (methods.IsDefaultOrEmpty) {
            return;
        }

        context.CancellationToken.ThrowIfCancellationRequested();

        var extensionClass = data.ClassesAndMethods.Class;

        var code = GeneratePortalOperationCode(in extensionClass, in methods, in data.Options, context.CancellationToken);
        var fileName = GetFileName(in extensionClass, data.ClassesAndMethods.Methods[0].PortalMethod);

        context.AddSource(fileName, code);
    }

    private static string GeneratePortalOperationCode(in ClassForExtensions extensionClass, in ImmutableArray<PortalOperationToGenerate> methods, in GeneratorOptions options, in CancellationToken ct) {
        var tmpMethods = methods;
        var tmpOptions = options;
        var tmpCt = ct;

        return GenerateCode(in extensionClass, in options, sb => sb.AppendMethodsGroupedByClass(tmpMethods, tmpOptions, tmpCt));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GenerateCode(in ClassForExtensions extensionClass, in GeneratorOptions options, Func<StringBuilder, StringBuilder> addClassContent) {
        return new StringBuilder()
            .AppendLine("// <auto-generated />")
            .AppendNullableContext(in options)
            .AppendLine()
            .AppendNamespace(in extensionClass)
            .AppendLine("{")
            .AppendClassDeclaration(in extensionClass)
            .AppendLine("    {")
            .AddClassContent(addClassContent)
            .AppendLine("    }")
            .AppendLine("}")
            .ToString();
    }

    private static string GetFileName(in ClassForExtensions extensionClass, DataPortalMethod portalMethod)
        => GetFileName(extensionClass, portalMethod.ToStringFast());

    private static string GetFileName(in ClassForExtensions extensionClass, string fileSuffix) {
        var typeNamespace = extensionClass.Namespace;
        if (!string.IsNullOrWhiteSpace(typeNamespace)) {
            typeNamespace += ".";
        }

        return $"{typeNamespace}{extensionClass.Name}_{fileSuffix}.g.cs";
    }
}