using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Ossendorf.Csla.DataPortalExtensionGenerator.Diagnostics;
internal static class NotPartialDiagnostic {
    internal const string Id = "DPEGEN001";
    internal const string Message = $"The target of the {GeneratorHelper.MarkerAttributeNameWithSuffix} must be declared as partial.";
    internal const string Title = "Must be partial";

    public static DiagnosticInfo Create(ClassDeclarationSyntax syntax)
        => new(new DiagnosticDescriptor(Id, Title, Message, "Usage", defaultSeverity: DiagnosticSeverity.Error, isEnabledByDefault: true), syntax.GetLocation());
}