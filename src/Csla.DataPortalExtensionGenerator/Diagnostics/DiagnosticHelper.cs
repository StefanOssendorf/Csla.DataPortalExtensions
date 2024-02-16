using Microsoft.CodeAnalysis;

namespace Ossendorf.Csla.DataPortalExtensionGenerator.Diagnostics;

internal static class DiagnosticHelper {
    public static void ReportDiagnostic(this SourceProductionContext ctx, DiagnosticInfo info)
        => ctx.ReportDiagnostic(CreateDiagnostic(info));
    private static Diagnostic CreateDiagnostic(DiagnosticInfo info) {
        var diagnostic = Diagnostic.Create(info.Descriptor, info.Location);
        return diagnostic;
    }
}