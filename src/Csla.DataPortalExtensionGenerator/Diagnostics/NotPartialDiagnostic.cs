using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Ossendorf.Csla.DataPortalExtensionGenerator.Diagnostics;
internal static class NotPartialDiagnostic {
    internal const string Message = $"The target of the {GeneratorHelper.MarkerAttributeNameWithSuffix} must be declared as partial.";
    internal const string Title = "Must be partial";

    public static DiagnosticInfo Create(ClassDeclarationSyntax syntax)
        => new(new DiagnosticDescriptor(DiagnosticId.DPEGEN001.ToStringFast(), Title, Message, "Usage", defaultSeverity: DiagnosticSeverity.Warning, isEnabledByDefault: true), syntax.GetLocation());
}