namespace Ossendorf.Csla.DataPortalExtensionGenerator;

internal readonly record struct GeneratorOptions {
    public readonly string MethodPrefix;
    public readonly string MethodSuffix;

    public GeneratorOptions(string methodPrefix, string methodSuffix) {
        MethodPrefix = methodPrefix;
        MethodSuffix = methodSuffix;
    }
}