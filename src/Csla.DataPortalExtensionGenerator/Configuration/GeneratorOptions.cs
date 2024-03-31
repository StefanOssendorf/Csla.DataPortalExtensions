using Microsoft.CodeAnalysis;

namespace Ossendorf.Csla.DataPortalExtensionGenerator.Configuration;

internal readonly record struct GeneratorOptions {
    public readonly string MethodPrefix;
    public readonly string MethodSuffix;
    public readonly NullableContextOptions NullableContextOptions;
    public readonly bool SuppressWarningCS8669;

    public GeneratorOptions(string methodPrefix, string methodSuffix, NullableContextOptions nullableContextOptions, bool suppressWarningCS8669) {
        MethodPrefix = methodPrefix;
        MethodSuffix = methodSuffix;
        NullableContextOptions = nullableContextOptions;
        SuppressWarningCS8669 = suppressWarningCS8669;
    }
}