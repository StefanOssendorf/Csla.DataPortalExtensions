using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Ossendorf.Csla.DataPortalExtensionGenerator.Configuration;
using Ossendorf.Csla.DataPortalExtensionGenerator.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;

namespace Ossendorf.Csla.DataPortalExtensionGenerator;

/// <summary>
/// The data portal extension source generator.
/// </summary>
[Generator]
public sealed partial class DataPortalExtensionGenerator : IIncrementalGenerator {

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
        => AddCodeGenerator(context);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AddCodeGenerator(IncrementalGeneratorInitializationContext context) {
        var options = OptionsGeneratorPart.GetGeneratorOptions(context);
        var extensionClassDeclaration = ExtensionClassGeneratorPart.GetExtensionClasses(context);
        var fetches = GetFetches(context);
        var fetchChilds= GetFetchChilds(context);
        var creates = GetCreates(context);
        var createChilds = GetCreateChilds(context);
        var deletes = GetDeletes(context);
        var executes = GetExecutes(context);

        RegisterCodeGenAttributesSources(context, extensionClassDeclaration);
        RegisterAttributeSourceOutput(context, extensionClassDeclaration, fetches , options);
        RegisterAttributeSourceOutput(context, extensionClassDeclaration, fetchChilds , options);
        RegisterAttributeSourceOutput(context, extensionClassDeclaration, createChilds , options);
        RegisterAttributeSourceOutput(context, extensionClassDeclaration, deletes , options);

        RegisterCreateAndExecuteSourceOutput(context, extensionClassDeclaration, creates, executes, options);

        static void RegisterAttributeSourceOutput(IncrementalGeneratorInitializationContext ctx, IncrementalValuesProvider<ClassForExtensions> classes, IncrementalValuesProvider<PortalOperationToGenerate> methods, IncrementalValueProvider<GeneratorOptions> options) {
            var combined = classes.Combine(methods.Collect()).Combine(options);

            ctx.RegisterSourceOutput(
                combined,
                Emitter.EmitClassForAttribute
            );
        }

        static void RegisterCreateAndExecuteSourceOutput(IncrementalGeneratorInitializationContext ctx, IncrementalValuesProvider<ClassForExtensions> classes, IncrementalValuesProvider<PortalOperationToGenerate> creates, IncrementalValuesProvider<PortalOperationToGenerate> executes, IncrementalValueProvider<GeneratorOptions> options) {
            var combined = classes
                .Combine(creates.Collect())
                .Combine(executes.Collect())
                .Combine(options);

            ctx.RegisterSourceOutput(
                combined,
                Emitter.EmitClassForCreateAndExecute
            );
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void RegisterCodeGenAttributesSources(IncrementalGeneratorInitializationContext context, IncrementalValuesProvider<ClassForExtensions> extensionClassDeclaration) {
        context.RegisterSourceOutput(
            extensionClassDeclaration,
            Emitter.EmitClassWithSourceGenIndicationAttributes
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IncrementalValuesProvider<PortalOperationToGenerate> GetFetches(IncrementalGeneratorInitializationContext context) 
        => GetOperationsToGenerateByCslaAttribute(context, QualifiedCslaAttributes.Fetch, DataPortalMethod.Fetch);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IncrementalValuesProvider<PortalOperationToGenerate> GetFetchChilds(IncrementalGeneratorInitializationContext context)
        => GetOperationsToGenerateByCslaAttribute(context, QualifiedCslaAttributes.FetchChild, DataPortalMethod.FetchChild);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IncrementalValuesProvider<PortalOperationToGenerate> GetCreates(IncrementalGeneratorInitializationContext context)
        => GetOperationsToGenerateByCslaAttribute(context, QualifiedCslaAttributes.Create, DataPortalMethod.Create);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IncrementalValuesProvider<PortalOperationToGenerate> GetCreateChilds(IncrementalGeneratorInitializationContext context)
        => GetOperationsToGenerateByCslaAttribute(context, QualifiedCslaAttributes.CreateChild, DataPortalMethod.CreateChild);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IncrementalValuesProvider<PortalOperationToGenerate> GetDeletes(IncrementalGeneratorInitializationContext context)
        => GetOperationsToGenerateByCslaAttribute(context, QualifiedCslaAttributes.Delete, DataPortalMethod.Delete);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IncrementalValuesProvider<PortalOperationToGenerate> GetExecutes(IncrementalGeneratorInitializationContext context)
        => GetOperationsToGenerateByCslaAttribute(context, QualifiedCslaAttributes.Execute, DataPortalMethod.Execute);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IncrementalValuesProvider<PortalOperationToGenerate> GetOperationsToGenerateByCslaAttribute(IncrementalGeneratorInitializationContext context, string qualifiedCslaAttribute, DataPortalMethod dataPortalMethod) {
        var operationsAndDiagnostics = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: qualifiedCslaAttribute,
                predicate: IsMethodDeclarationSyntax,
                transform: (ctx, ct) => Parser.GetPortalMethods(ctx, dataPortalMethod, ct)
            );

        context.RegisterSourceOutput(
            operationsAndDiagnostics.SelectMany((r, _) => r.Errors),
            static (ctx, info) => ctx.ReportDiagnostic(info)
        );

        return operationsAndDiagnostics
            .Where(r => r.Value.IsValid)
            .Select((r, _) => r.Value.PortalOperationToGenerate);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsMethodDeclarationSyntax(SyntaxNode node, CancellationToken _) => node is MethodDeclarationSyntax;
}