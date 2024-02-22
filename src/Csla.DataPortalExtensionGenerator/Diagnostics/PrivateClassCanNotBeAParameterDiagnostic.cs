using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Globalization;

namespace Ossendorf.Csla.DataPortalExtensionGenerator.Diagnostics;

internal static class PrivateClassCanNotBeAParameterDiagnostic {
    internal const string Message = "The {0} method for {1} has a paramter of type {2} which is private and can't be used for generating an extension method.";
    internal const string Title = "CSLA method parameters must not be private types.";

    public static DiagnosticInfo Create(MethodDeclarationSyntax syntax, DataPortalMethod dataPortalMethod, string methodName, string invalidClassType)
        => new(new DiagnosticDescriptor(DiagnosticId.DPEGEN002.ToStringFast(), Title, string.Format(CultureInfo.InvariantCulture, Message, dataPortalMethod.ToStringFast(), methodName, invalidClassType), "Usage", defaultSeverity: DiagnosticSeverity.Error, isEnabledByDefault: true), syntax.GetLocation());
}