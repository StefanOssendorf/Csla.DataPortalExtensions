namespace Ossendorf.Csla.DataPortalExtensionsGenerator.Tests;

[UsesVerify]
public class GeneratorSnapshotTests {

    public static TheoryData<string> AllSupportedDataPortalMethods {
        get {
            var data = new TheoryData<string>();

            var portalMethods = new string[] { "Fetch", "Create", "Delete", "Execute", "FetchChild", "CreateChild" };
            foreach (var item in portalMethods) {
                data.AddRange(item, $"{item}Attribute");
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(AllSupportedDataPortalMethods))]
    public Task WithEmptyParameterGeneratesCorrectly(string portalMethod) {
        var cslaSource = @$"
using Csla;

namespace VerifyTests;

public class DummyBO {{
    
    [{portalMethod}]
    public void Foo() {{
    }}
}}
";

        return TestHelper.Verify(cslaSource, cfg => cfg.UseParameters(portalMethod));
    }

    [Fact]
    public Task With3PrimitiveParametersShouldGenerateCorrectly() {
        var cslaSource = $@"
using Csla;
using System;

namespace VerifyTests;

public class DummyBOWithParams {{
    [Fetch]
    public void Bar(int? id, string krznbf, Guid reference) {{
    }}
}}
";

        return TestHelper.Verify(cslaSource);
    }

    [Fact]
    public Task InjectedParametersMustBeIgnored() {
        var cslaSource = $@"
using Csla;

namespace VerifyTests;

public class DummyBOWithParams {{
    [Fetch]
    public void Bar(int? id, string krznbf, decimal reference, [Inject] IDataPortalFactory dpf) {{
    }}
}}
";

        return TestHelper.Verify(cslaSource);
    }

    [Fact]
    public Task ParametersWithDefaultValuesShouldBeUsedAsIs() {
        var cslaSource = $@"
using Csla;

namespace VerifyTests;

public class DummyBOWithParams {{
    [Fetch]
    public void Bar(int? id, string krznbf, decimal reference, [Inject] IDataPortalFactory dpf, string abcdefg = """") {{
    }}
}}
";

        return TestHelper.Verify(cslaSource);
    }

    [Fact]
    public Task OperationWithOnlyInjectedParameters() {
        var cslaSource = $@"
using Csla;

namespace VerifyTests;

public class DummyBOWithParams {{
    [Fetch]
    public void Bar([Inject] IDataPortalFactory dpf, [Inject] IChildDataPortalFactory cdpf) {{
    }}
}}
";

        return TestHelper.Verify(cslaSource);
    }

    [Fact]
    public Task NestedTypeAsParameter() {
        var cslaSource = $@"
using Csla;
using MyTest.Greats;

namespace VerifyTests;

public class DummyBOWithParams {{
    [Fetch]
    public void Bar(int? id, Foo.Bar krznbf, decimal reference, [Inject] IDataPortalFactory dpf) {{
    }}
}}
";

        var additionalType = $@"
namespace MyTest.Greats;

public class Foo {{
    public class Bar {{
        public string Name {{ get; set; }}
    }}
}}
";

        return TestHelper.Verify(cslaSource, additionalType);
    }

    [Fact]
    public Task NestedTypeAsBusinessObjectType() {
        var cslaSource = $@"
using Csla;

namespace VerifyTests;

public class DummyBOWithParams {{
    public class InnerDummy {{
        [Fetch]
        public void Bar() {{
        }}
    }}
}}
";

        return TestHelper.Verify(cslaSource);
    }

    [Fact]
    public Task NullablePrimitveParameter() {
        var cslaSource = $@"
using Csla;

namespace VerifyTests;

public class DummyBOWithParams {{
    [Fetch]
    public void Bar(int? krznbf) {{
    }}
}}
";

        return TestHelper.Verify(cslaSource);
    }
}