using Csla;

namespace Ossendorf.Csla.DataPortalExtensionsGenerator.Tests.CsprojConfig;

public class UnitTest1 {
    [Fact]
    public async Task Test1() {
        IDataPortal<TestBO> tmp = null;

        if (tmp != null) {
            await tmp.MyFetchAB(1);
        }
    }
}

[Serializable]
public class TestBO : BusinessBase<TestBO> {
    [Fetch]
    private Task FetchAB(int a) {
        return Task.CompletedTask;
    }
}


[Ossendorf.Csla.DataPortalExtensionGenerator.DataPortalExtensions]
public static partial class TestExtensions {

}