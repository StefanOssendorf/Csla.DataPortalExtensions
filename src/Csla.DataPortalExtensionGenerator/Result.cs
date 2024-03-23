using Ossendorf.Csla.DataPortalExtensionGenerator.Diagnostics;
using Ossendorf.Csla.DataPortalExtensionGenerator.Internals;

namespace Ossendorf.Csla.DataPortalExtensionGenerator;

internal record Result<TValue>(TValue Value, EquatableArray<DiagnosticInfo> Errors)
    where TValue: IEquatable<TValue>? {
    public static Result<(TValue, bool)> NotValid() => new((default!, false), EquatableArray<DiagnosticInfo>.Empty);
}
