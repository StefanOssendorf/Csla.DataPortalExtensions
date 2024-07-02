namespace Ossendorf.Csla.DataPortalExtensionGenerator;

internal readonly record struct PortalObject {
    public readonly bool HasPublicModifier;
    public readonly bool IsAbstract;
    public readonly string GloballyQualifiedName;
    public readonly string BaseClass;

    public PortalObject(bool hasPublicModifier, string globallyQualifiedName, string baseClass, bool isAbstract) {
        HasPublicModifier = hasPublicModifier;
        GloballyQualifiedName = globallyQualifiedName;
        BaseClass = baseClass;
        IsAbstract = isAbstract;
    }
}