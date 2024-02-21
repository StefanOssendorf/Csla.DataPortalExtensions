namespace Ossendorf.Csla.DataPortalExtensionGenerator;

internal readonly record struct ClassForExtensions {
    public readonly string Name;
    public readonly string Namespace;
    public readonly bool HasPartialModifier;

    public ClassForExtensions(string name, string @namespace, bool hasPartialModifier) {
        Name = name;
        Namespace = @namespace;
        HasPartialModifier = hasPartialModifier;
    }
}
