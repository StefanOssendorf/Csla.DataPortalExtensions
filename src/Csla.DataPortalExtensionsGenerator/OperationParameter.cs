namespace Ossendorf.Csla.DataPortalExtensionsGenerator;

internal readonly record struct OperationParameter {
    public readonly string Namespace;
    public readonly string ArgumentFormatted;
    public readonly string ParameterFormatted;

    public OperationParameter(string @namespace, string argumentFormatted, string parameterFormatted) {
        Namespace = @namespace;
        ArgumentFormatted = argumentFormatted;
        ParameterFormatted = parameterFormatted;
    }
}
