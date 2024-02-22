using Microsoft.CodeAnalysis;

namespace Ossendorf.Csla.DataPortalExtensionGenerator.Diagnostics;

internal record DiagnosticInfo(DiagnosticDescriptor Descriptor, Location? Location);