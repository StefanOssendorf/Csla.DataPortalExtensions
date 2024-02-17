using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Ossendorf.Csla.DataPortalExtensionGenerator.Tests;
public class DiagnosticTests {

    private const string ValidExtensionClass = @$"
namespace GeneratorTests {{
    [Ossendorf.Csla.DataPortalExtensionGenerator.DataPortalExtensionsAttribute]
    public static partial class DataPortalExtensions {{
    }}
}}";

    [Fact]
    public void NonPartialExtensionClassMustGetDiagnosticDPEGEN001() {
        const string extensionClass = @$"
namespace GeneratorTests {{
    [Ossendorf.Csla.DataPortalExtensionGenerator.DataPortalExtensionsAttribute]
    public static class DataPortalExtensions {{
    }}
}}";

        TestHelper.Diagnostic(extensionClass, expectedDiagnosticId: "DPEGEN001");
    }

    [Fact]
    public void CslaMethodWithPrivateClassMustGetDiagnosticDPEGEN002() {
        const string cslaClass = $@"
using Csla;

namespace InvalidCslaClass;

public class Foo {{
    [Fetch]
    private void Bar(string x, Foo.PrivateClass z){{ }}

    private class PrivateClass {{ }}
}}
";

        TestHelper.Diagnostic(ValidExtensionClass, cslaClass, "DPEGEN002");
    }
}
