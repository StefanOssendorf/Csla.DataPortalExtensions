namespace Ossendorf.Csla.DataPortalExtensionGenerator;
internal static class GeneratorHelper {

    public const string FullyQalifiedNameOfMarkerAttribute = "Ossendorf.Csla.DataPortalExtensionGenerator.DataPortalExtensionsAttribute";

    public const string MarkerAttributeNameWithSuffix = "DataPortalExtensionsAttribute";
    public const string MarkerAttributeNameWithoutSuffix = "DataPortalExtensions";

    public static string VersionString { get; } = typeof(DataPortalExtensionGenerator)
                                                    .Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), true)
                                                    .Cast<System.Reflection.AssemblyInformationalVersionAttribute>().SingleOrDefault()?.InformationalVersion ??
                                                    typeof(DataPortalExtensionGenerator).Assembly.GetName().Version.ToString();

    private static readonly Dictionary<string, DataPortalMethod> _methodTranslations = [];
    static GeneratorHelper() {
        foreach (var portalMethod in Enum.GetValues(typeof(DataPortalMethod)).Cast<DataPortalMethod>()) {
            _methodTranslations.Add(portalMethod.ToStringFast(), portalMethod);
            _methodTranslations.Add($"{portalMethod.ToStringFast()}Attribute", portalMethod);
        }
    }

    public static IReadOnlyDictionary<string, DataPortalMethod> RecognizedCslaDataPortalAttributes = _methodTranslations;
}