using VerifyCS = Ossendorf.Csla.DataPortalExtensionGenerator.Analyzers.Test.CSharpAnalyzerVerifier<Ossendorf.Csla.DataPortalExtensionGenerator.Analyzers.SynchronousDataPortalCallAnalyzer>;

namespace Ossendorf.Csla.DataPortalExtensionGenerator.Analyzers.Test;

public class SynchronousDataPortalCallAnalyzerTests {

    public static TheoryData<string> SyncDataPortalMethods {
        get {
            return new([
                "Fetch()",
                "Create()",
                "Delete()",
                "Execute()"
            ]);
        }
    }

    [Theory]
    [MemberData(nameof(SyncDataPortalMethods))]
    public async Task DataPortalSyncMethodMustTriggerDPEG1002(string portalMethod) {
        var methodCodeLine = portalMethod == "Delete()"
            ? $"{{|DPEG1002:fooPortal.{portalMethod}|}};"
            : $"var x = {{|DPEG1002:fooPortal.{portalMethod}|}};";

        var cslaSource = @$"
using Csla;

namespace TestNamespace {{

    public class Testing : Csla.Core.ICslaObject {{

        public void Test1() {{
            IDataPortal<Testing> fooPortal = null!;

            {methodCodeLine}
        }}
    }}
}}";
        await VerifyCS.VerifyAnalyzerAsync(cslaSource);
    }

    public static TheoryData<string> SyncChildDataPortalMethods {
        get {
            return new([
                "FetchChild()",
                "CreateChild()"
            ]);
        }
    }

    [Theory]
    [MemberData(nameof(SyncChildDataPortalMethods))]
    public async Task ChildDataPortalSyncMethodMustTriggerDPEG1002(string portalMethod) {
        var cslaSource = @$"
using Csla;

namespace TestNamespace {{

    public class Testing : Csla.Core.ICslaObject {{

        public void Test1() {{
            IChildDataPortal<Testing> fooPortal = null!;

            var x = {{|DPEG1002:fooPortal.{portalMethod}|}};
        }}
    }}
}}";
        await VerifyCS.VerifyAnalyzerAsync(cslaSource);
    }

    [Fact]
    public async Task IDataPortalAsyncMethodsMustNotTriggerDPEG1002() {
        var cslaSource = @"
using Csla;
using System.Threading.Tasks;

namespace TestNamespace {

    public class Testing : Csla.Core.ICslaObject {

        public async Task Test1() {
            IDataPortal<Testing> fooPortal = null!;

            var x = await fooPortal.FetchAsync();
        }
    }
}";
        await VerifyCS.VerifyAnalyzerAsync(cslaSource);
    }

    [Fact]
    public async Task IChildDataPortalAsyncMethodsMustNotTriggerDPEG1002() {
        var cslaSource = @"
using Csla;
using System.Threading.Tasks;

namespace TestNamespace {

    public class Testing : Csla.Core.ICslaObject {

        public async Task Test1() {
            IChildDataPortal<Testing> fooPortal = null!;

            var x = await fooPortal.FetchChildAsync();
        }
    }
}";
        await VerifyCS.VerifyAnalyzerAsync(cslaSource);
    }

    [Fact]
    public async Task UserTypeInNestedCslaNamespaceMustNotTriggerDPEG1002() {
        var cslaSource = @"
namespace Acme.Csla {

    public interface IDataPortal<T> {
        T Fetch();
    }

    public class Testing {
        public void Test1() {
            IDataPortal<Testing> portal = null!;
            var x = portal.Fetch();
        }
    }
}";
        await VerifyCS.VerifyAnalyzerAsync(cslaSource);
    }

    [Fact]
    public async Task NonCslaFetchMustNotTriggerDPEG1002() {
        var cslaSource = @"
namespace TestNamespace {

    public interface IMyPortal<T> {
        T Fetch();
    }

    public class Testing {
        public void Test1() {
            IMyPortal<Testing> portal = null!;
            var x = portal.Fetch();
        }
    }
}";
        await VerifyCS.VerifyAnalyzerAsync(cslaSource);
    }
}
