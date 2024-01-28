using NetEscapades.EnumGenerators;

namespace Ossendorf.Csla.DataPortalExtensionsGenerator;

[EnumExtensions]
internal enum DataPortalMethod {
    Fetch,
    FetchChild,
    Delete,
    Create,
    CreateChild,
    Execute
}
