namespace Ossendorf.Csla.DataPortalExtensionsGenerator;

internal readonly record struct ClassForExtensions {
    public readonly string Name;
    public readonly string Namespace;

    public ClassForExtensions(string name, string ns) {
        Name = name;
        Namespace = ns;
    }
}
