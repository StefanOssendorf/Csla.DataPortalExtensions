namespace Ossendorf.Csla.DataPortalExtensionGenerator.Analyzers;
internal static class Constants {
    internal static class DiagnosticId {
        public const string NotDataPortalExtensionMethodUsed = "DPEG1000";
        public const string DataPortalInterfaceUsedAsNotInjectedParameter = "DPEG1001";
        public const string SynchronousDataPortalCallUsed = "DPEG1002";
    }

    internal static class Category {
        public const string Usage = "Usage";
    }
}
