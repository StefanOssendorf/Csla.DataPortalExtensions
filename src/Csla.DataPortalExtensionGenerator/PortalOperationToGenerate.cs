using Ossendorf.Csla.DataPortalExtensionGenerator.Internals;

namespace Ossendorf.Csla.DataPortalExtensionGenerator;

internal readonly record struct PortalOperationToGenerate {

    public readonly string MethodName;
    public readonly EquatableArray<OperationParameter> Parameters;
    public readonly DataPortalMethod PortalMethod;
    public readonly PortalObject Object;

    public PortalOperationToGenerate(PortalObject actualClass, PortalOperationToGenerate item) 
        : this(item.MethodName, item.Parameters, item.PortalMethod, actualClass) {
    }

    public PortalOperationToGenerate(string methodName, EquatableArray<OperationParameter> parameters, DataPortalMethod portalMethod, PortalObject @object) {
        MethodName = methodName;
        Parameters = parameters;
        PortalMethod = portalMethod;
        Object = @object;
    }
}