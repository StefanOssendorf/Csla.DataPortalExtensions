namespace Ossendorf.Csla.DataPortalExtensionGenerator;

internal readonly record struct OperationParameter {
    public readonly string Namespace;
    public readonly string ArgumentFormatted;
    public readonly string ParameterFormatted;
    public readonly bool IsPublic;

    public OperationParameter(string @namespace, string argumentFormatted, string parameterFormatted, bool isPublic) {
        Namespace = @namespace;
        ArgumentFormatted = argumentFormatted;
        ParameterFormatted = parameterFormatted;
        IsPublic = isPublic;
    }
}
