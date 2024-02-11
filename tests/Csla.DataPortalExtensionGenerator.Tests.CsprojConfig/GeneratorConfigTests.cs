using Csla;

namespace Ossendorf.Csla.DataPortalExtensionsGenerator.Tests.CsprojConfig;

public class GeneratorConfigTests {
    [Fact]
    public async Task Test1() {
        IDataPortal<TestBO> tmp = null;

        if (tmp != null) {
            await tmp.PrefixFetchABCSuffix(1);
        }

        //Compilable test method is the test since I can't test the generated method in code :)
    }
}

[Serializable]
public class TestBO : BusinessBase<TestBO> {
    [Fetch]
    private Task FetchABC(int a) {
        _ = a;
        return Task.CompletedTask;
    }
}

[Ossendorf.Csla.DataPortalExtensionGenerator.DataPortalExtensions]
public static partial class TestExtensions {

}