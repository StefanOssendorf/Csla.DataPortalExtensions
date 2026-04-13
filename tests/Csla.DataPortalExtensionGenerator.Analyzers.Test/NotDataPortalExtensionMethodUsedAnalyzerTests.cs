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

    public class Testing : Csla.Core.ICslaObject {{
    
        public async Task Test1() {{
            IDataPortal<Testing> fooPortal = null!;

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

    public class Testing : Csla.Core.ICslaObject {{
    
        public async Task Test1() {{
            IChildDataPortal<Testing> fooPortal = null!;

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

    public class Testing : Csla.Core.ICslaObject {{
    
        public async Task Test1() {{
            IDataPortal<Testing> fooPortal = null!;
            Testing foo = null!;

            var x = await fooPortal.UpdateAsync(foo);
        }}
    }}
}}";

        await VerifyCS.VerifyAnalyzerAsync(cslaSource);
    }

    [Fact]
    public async Task UserTypeInNestedCslaNamespaceMustNotTriggerDPEG1000() {
        var cslaSource = @"
namespace Acme.Csla {

    public interface IDataPortal<T> {
        System.Threading.Tasks.Task<T> FetchAsync();
    }

    public class Testing {
        public async System.Threading.Tasks.Task Test1() {
            IDataPortal<Testing> fooPortal = null!;
            var x = await fooPortal.FetchAsync();
        }
    }
}";
        await VerifyCS.VerifyAnalyzerAsync(cslaSource);
    }

    [Fact]
    public async Task IChildDataPortalUpdateMustNotGetDiagnosticDPEG1000() {
        var cslaSource = @$"
using Csla;
using System.Threading.Tasks;

namespace TestNamespace {{

    public class Testing : Csla.Core.ICslaObject {{
    
        public async Task Test1() {{
            IChildDataPortal<Testing> fooPortal = null!;
            Testing foo = null!;

            await fooPortal.UpdateChildAsync(foo);
        }}
    }}
}}";

        await VerifyCS.VerifyAnalyzerAsync(cslaSource);
    }
}