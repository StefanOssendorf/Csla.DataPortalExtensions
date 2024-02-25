using VerifyCS = Ossendorf.Csla.DataPortalExtensionGenerator.Analyzers.Test.CSharpAnalyzerVerifier<Ossendorf.Csla.DataPortalExtensionGenerator.Analyzers.NotDataPortalExtensionMethodUsedAnalyzer>;

namespace Ossendorf.Csla.DataPortalExtensionGenerator.Analyzers.Test;

public class NotDataPortalExtensionMethodUsedAnalyzerTests {

    public static TheoryData<string> AsyncDataPortalMethods {
        get {
            return new([
                "FetchAsync()",
                "CreateAsync()",
                "DeleteAsync()",
                "ExecuteAsync()"
            ]);
        }
    }

    [Theory]
    [MemberData(nameof(AsyncDataPortalMethods))]
    public async Task DataPortalMethodMustTriggerDPEG1000(string portalMethod) {
        var methodCodeLine = $"var x = await {{|DPEG1000:fooPortal.{portalMethod}|}};";
        if (portalMethod == "DeleteAsync()") {
            methodCodeLine = $"await {{|DPEG1000:fooPortal.{portalMethod}|}};";
        }

        var cslaSource = @$"
using Csla;
using System.Threading.Tasks;

namespace TestNamespace {{

    public class Testing {{
    
        public async Task Test1() {{
            IDataPortal<string> fooPortal = null!;

            {methodCodeLine}
        }}
    }}
}}";
        await VerifyCS.VerifyAnalyzerAsync(cslaSource);
    }

    public static TheoryData<string> AsyncChildDataPortalMethods {
        get {
            return new([
                "FetchChildAsync()",
                "CreateChildAsync()"
            ]);
        }
    }

    [Theory]
    [MemberData(nameof(AsyncChildDataPortalMethods))]
    public async Task ChildDataPortalMethodMustTriggerDPEG1000(string portalMethod) {
        var methodCodeLine = $"var x = await {{|DPEG1000:fooPortal.{portalMethod}|}};";
        if (portalMethod.StartsWith("UpdateChildAsync(")) {
            methodCodeLine = $"await {{|DPEG1000:fooPortal.{portalMethod}|}};";
        }

        var cslaSource = @$"
using Csla;
using System.Threading.Tasks;

namespace TestNamespace {{

    public class Testing {{
    
        public async Task Test1() {{
            IChildDataPortal<string> fooPortal = null!;

            {methodCodeLine}
        }}
    }}
}}";
        await VerifyCS.VerifyAnalyzerAsync(cslaSource);
    }

    [Fact]
    public async Task IDataPortalUpdateMustNotGetDiagnosticDPEG1000() {
        var cslaSource = @$"
using Csla;
using System.Threading.Tasks;

namespace TestNamespace {{

    public class Testing {{
    
        public async Task Test1() {{
            IDataPortal<string> fooPortal = null!;

            var x = await fooPortal.UpdateAsync(""asdad"");
        }}
    }}
}}";

        await VerifyCS.VerifyAnalyzerAsync(cslaSource);
    }

    [Fact]
    public async Task IChildDataPortalUpdateMustNotGetDiagnosticDPEG1000() {
        var cslaSource = @$"
using Csla;
using System.Threading.Tasks;

namespace TestNamespace {{

    public class Testing {{
    
        public async Task Test1() {{
            IChildDataPortal<string> fooPortal = null!;

            await fooPortal.UpdateChildAsync(""asdad"");
        }}
    }}
}}";

        await VerifyCS.VerifyAnalyzerAsync(cslaSource);
    }
}