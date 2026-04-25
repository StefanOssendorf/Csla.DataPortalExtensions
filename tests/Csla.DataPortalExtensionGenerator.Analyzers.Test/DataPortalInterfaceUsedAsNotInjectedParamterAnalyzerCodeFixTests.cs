using VerifyCS = Ossendorf.Csla.DataPortalExtensionGenerator.Analyzers.Test.CSharpCodeFixVerifier<
    Ossendorf.Csla.DataPortalExtensionGenerator.Analyzers.DataPortalInterfaceUsedAsNotInjectedParamterAnalyzer,
    Ossendorf.Csla.DataPortalExtensionGenerator.Analyzers.DataPortalInterfaceUsedAsNotInjectedParamterAnalyzerCodeFixProvider>;

namespace Ossendorf.Csla.DataPortalExtensionGenerator.Analyzers.Test;

public class DataPortalInterfaceUsedAsNotInjectedParamterAnalyzerCodeFixTests {

    [Theory]
    [InlineData("IDataPortal")]
    [InlineData("IChildDataPortal")]
    public async Task AddInjectAttributeToPortalParameter(string portalType) {
        var source = @$"
using Csla;

namespace TestNamespace;

public class Testing : Csla.Core.ICslaObject {{

    [Fetch]
    private void Foo(string a, {{|DPEG1001:{portalType}<Testing> portal|}}){{
    }}
}}";

        var fixedSource = @$"
using Csla;

namespace TestNamespace;

public class Testing : Csla.Core.ICslaObject {{

    [Fetch]
    private void Foo(string a, [Inject] {portalType}<Testing> portal){{
    }}
}}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }

    [Theory]
    [InlineData("IDataPortal")]
    [InlineData("IChildDataPortal")]
    public async Task AddInjectAttributeWhenMultiplePortalParametersAreMissing(string portalType) {
        var source = @$"
using Csla;

namespace TestNamespace;

public class Testing : Csla.Core.ICslaObject {{

    [Fetch]
    private void Foo({{|DPEG1001:{portalType}<Testing> portal1|}}, {{|DPEG1001:{portalType}<Testing> portal2|}}){{
    }}
}}";

        var fixedSource = @$"
using Csla;

namespace TestNamespace;

public class Testing : Csla.Core.ICslaObject {{

    [Fetch]
    private void Foo([Inject] {portalType}<Testing> portal1, [Inject] {portalType}<Testing> portal2){{
    }}
}}";

        await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
    }
}