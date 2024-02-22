using NetEscapades.EnumGenerators;

namespace Ossendorf.Csla.DataPortalExtensionGenerator.Diagnostics;

[EnumExtensions]
internal enum DiagnosticId {
    // NotPartialDiagnostic
    DPEGEN001,
    // PrivateClassCanNotBeAParameterDiagnostic
    DPEGEN002,
    // NullableContextValueDiagnostic
    DPEGEN003,
    // SuppressWarningCS8669ValueDiagnostic
    DPEGEN004,
}
