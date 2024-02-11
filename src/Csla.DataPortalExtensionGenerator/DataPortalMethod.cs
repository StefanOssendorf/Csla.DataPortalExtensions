using NetEscapades.EnumGenerators;

namespace Ossendorf.Csla.DataPortalExtensionGenerator;

[EnumExtensions]
internal enum DataPortalMethod {
    Fetch,
    FetchChild,
    Delete,
    Create,
    CreateChild,
    Execute
}
