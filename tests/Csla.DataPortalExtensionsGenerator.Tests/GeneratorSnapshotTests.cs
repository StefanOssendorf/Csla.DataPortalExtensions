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

    [Theory]
    [MemberData(nameof(CSharpBuiltInTypesNullable))]
    public Task NullablePrimitveParameter(string type) {
        var cslaSource = $@"
using Csla;

namespace VerifyTests;

public class DummyBOWithParams {{
    [Fetch]
    public void Bar({type}? krznbf) {{
    }}
}}
";

        return TestHelper.Verify(cslaSource, t => t.UseParameters(type));
    }

    [Fact]
    public Task ParameterWithGenericArity1() {
        var cslaSource = $@"
using Csla;
using System.Collections.Generic;

namespace VerifyTests;

public class DummyBOWithParams {{
    [Fetch]
    public void Bar(IEnumerable<Guid> krznbf) {{
    }}
}}
";

        return TestHelper.Verify(cslaSource);
    }

    [Fact]
    public Task NullableEnumParameter() {
        var cslaSource = $@"
using Csla;
using TestEnum;

namespace VerifyTests;

public class DummyBOWithParams {{
    [Fetch]
    public void Bar(SomeEnum? krznbf) {{
    }}
}}
";

        var someEnumSource = $@"
namespace TestEnum;

public enum SomeEnum {{
    None = 0,
    Some = 1
}}
";

        return TestHelper.Verify(cslaSource, someEnumSource);
    }

    [Fact]
    public Task EnumParameter() {
        var cslaSource = $@"
using Csla;
using TestEnum;

namespace VerifyTests;

public class DummyBOWithParams {{
    [Fetch]
    public void Bar(SomeEnum krznbf) {{
    }}
}}
";

        var someEnumSource = $@"
namespace TestEnum;

public enum SomeEnum {{
    None = 0,
    Some = 1
}}
";

        return TestHelper.Verify(cslaSource, someEnumSource);
    }

    [Theory]
    [MemberData(nameof(CSharpBuiltInTypes))]
    public Task ArrayOfBuiltInTypes(string type) {
        var cslaSource = $@"
using Csla;

namespace VerifyTests;

public class DummyBOWithParams {{
    [Fetch]
    public void Bar({type}[] krznbf) {{
    }}
}}
";

        return TestHelper.Verify(cslaSource, t => t.UseParameters(type));
    }

    public static TheoryData<string> CSharpBuiltInTypes => new(_csharpBuiltInTypes);

    public static TheoryData<string> CSharpBuiltInTypesNullable => new(_csharpBuiltInTypes.Where(t => t is not "string" and not "object"));

    private static readonly string[] _csharpBuiltInTypes = ["string", "bool", "byte", "sbyte", "char", "decimal", "double", "float", "int", "uint", "long", "ulong", "short", "ushort", "object"];
}