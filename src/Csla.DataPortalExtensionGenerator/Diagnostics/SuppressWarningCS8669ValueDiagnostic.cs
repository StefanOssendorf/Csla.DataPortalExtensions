using Microsoft.CodeAnalysis;
using Ossendorf.Csla.DataPortalExtensionGenerator.Configuration;
using System.Globalization;

namespace Ossendorf.Csla.DataPortalExtensionGenerator.Diagnostics;

internal static class SuppressWarningCS8669ValueDiagnostic {
    internal const string Message = $"The value '{{0}}' for setting '{ConfigConstants.SuppressWarningCS8669}' is not parseable to boolean. Default of false is used.";
    internal const string Title = "CS8669 value unknown";

    public static DiagnosticInfo Create(string configValue)
        => new(new DiagnosticDescriptor(DiagnosticId.DPEGEN004.ToStringFast(), Title, string.Format(CultureInfo.InvariantCulture, Message, configValue), "Usage", defaultSeverity: DiagnosticSeverity.Warning, isEnabledByDefault: true), null);
}