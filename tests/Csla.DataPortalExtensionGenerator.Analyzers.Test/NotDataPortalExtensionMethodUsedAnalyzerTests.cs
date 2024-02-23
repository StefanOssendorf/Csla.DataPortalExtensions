using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using VerifyCS = Csla.DataPortalExtensionGenerator.Analyzers.Test.CSharpAnalyzerVerifier<Ossendorf.Csla.DataPortalExtensionGenerator.Analyzers.NotDataPortalExtensionMethodUsedAnalyzer>;

namespace Ossendorf.Csla.DataPortalExtensionGenerator.Analyzers.Test;

[TestClass]
public class NotDataPortalExtensionMethodUsedAnalyzerTests {

    [TestMethod]
    public async Task MyTestMethodAsync() {
        var expectedDiagnostc = VerifyCS.Diagnostic(NotDataPortalExtensionMethodUsedAnalyzer.DiagnosticId)
            //.WithSpan(new FileLinePositionSpan("", new LinePosition(1,1), new LinePosition(1, 1)))
            .WithLocation(0)
            .WithArguments("FetchAsync");

        var cslaSource = @"
using Csla;
using System.Threading.Tasks;

namespace TestNamespace {

    public class Testing {
    
        public async Task Test1() {
            IDataPortal<string> fooPortal = null!;

            var x = await fooPortal.{|#0:FetchAsync|}();
        }
    }
}";
        await VerifyCS.VerifyAnalyzerAsync(cslaSource, expectedDiagnostc);
    }
}
