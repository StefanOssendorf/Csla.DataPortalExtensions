using Microsoft.CodeAnalysis;
using System.Globalization;

namespace Ossendorf.Csla.DataPortalExtensionGenerator.Diagnostics;

internal static class NullableContextValueDiagnostic {
    internal const string Message = $"The value '{{0}}' for setting '{ConfigConstants.NullableContext}' is not known. Only 'Enable' and 'Disable' are allowed. Default of 'Enable' is used.";
    internal const string Title = "Nullable context value unknown";

    public static DiagnosticInfo Create(string configValue)
        => new(new DiagnosticDescriptor(DiagnosticId.DPEGEN003.ToStringFast(), Title, string.Format(CultureInfo.InvariantCulture, Message, configValue), "Usage", defaultSeverity: DiagnosticSeverity.Warning, isEnabledByDefault: true), null);
}
