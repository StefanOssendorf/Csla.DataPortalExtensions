using VerifyCS = Ossendorf.Csla.DataPortalExtensionGenerator.Analyzers.Test.CSharpAnalyzerVerifier<Ossendorf.Csla.DataPortalExtensionGenerator.Analyzers.DataPortalInterfaceUsedAsNotInjectedParamterAnalyzer>;

namespace Ossendorf.Csla.DataPortalExtensionGenerator.Analyzers.Test;

public class DataPortalInterfaceUsedAsNotInjectedParamterAnalyzerTests {

    [Theory]
    [InlineData("IDataPortal")]
    [InlineData("IChildDataPortal")]
    public async Task NotInjectedDataPortalInterfaceMustGetDiagnosticDPEG1001(string portalType) {
        var cslaSource = @$"
using Csla;

namespace TestNamespace;

public class Testing {{
    
    [Fetch]
    private void Foo(string a, {{|DPEG1001:{portalType}<string> portal|}}){{
    }}
}}";
        await VerifyCS.VerifyAnalyzerAsync(cslaSource);
    }

    [Theory]
    [InlineData("IDataPortal")]
    [InlineData("IChildDataPortal")]
    public async Task InjectedDataPortalInterfaceMustNotGetDiagnosticDPEG1001(string portalType) {
        var cslaSource = @$"
using Csla;

namespace TestNamespace;

public class Testing {{
    
    [Fetch]
    private void Foo(string a, [Inject] {portalType}<string> portal){{
    }}
}}";
        await VerifyCS.VerifyAnalyzerAsync(cslaSource);
    }
}
