﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Globalization;

namespace Ossendorf.Csla.DataPortalExtensionGenerator.Diagnostics;

internal static class PrivateClassCanNotBeAParameterDiagnostic {
    internal const string Id = "DPEGEN002";
    internal const string Message = "The {0} method for {1} has a paramter of type {2} which is private and can't be used for generating an extension method.";
    internal const string Title = "CSLA method parameters must not be private types.";

    public static DiagnosticInfo Create(MethodDeclarationSyntax syntax, DataPortalMethod dataPortalMethod, string methodName, string invalidClassType)
        => new(new DiagnosticDescriptor(Id, Title, string.Format(CultureInfo.InvariantCulture, Message, dataPortalMethod.ToStringFast(), methodName, invalidClassType), "Usage", defaultSeverity: DiagnosticSeverity.Warning, isEnabledByDefault: true), syntax.GetLocation());
}