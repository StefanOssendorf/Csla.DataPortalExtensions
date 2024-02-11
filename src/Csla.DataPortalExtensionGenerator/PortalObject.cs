namespace Ossendorf.Csla.DataPortalExtensionGenerator;

internal readonly record struct PortalObject {
    public readonly bool HasPublicModifier;
    public readonly string GloballyQualifiedName;

    public PortalObject(bool hasPublicModifier, string globallyQualifiedName) {
        HasPublicModifier = hasPublicModifier;
        GloballyQualifiedName = globallyQualifiedName;
    }
}