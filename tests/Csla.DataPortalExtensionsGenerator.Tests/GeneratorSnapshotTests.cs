using System;
using System.Threading.Tasks;
using Xunit;

namespace Ossendorf.Csla.DataPortalExtensionsGenerator.Tests;

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
    public Task EmptyParameters(string portalMethod) {
        var cslaSource = @$"
using Csla;

namespace VerifyTests;

public class DummyBO {{
    
    [{portalMethod}]
    private void Foo() {{
    }}
}}
";

        return TestHelper.Verify(cslaSource, cfg => cfg.UseParameters(portalMethod));
    }

    [Fact]
    public Task MultiplePrimitiveParameters() {
        var cslaSource = $@"
using Csla;
using System;

namespace VerifyTests;

public class DummyBOWithParams {{
    [Fetch]
    private void Bar(int? id, string krznbf, Guid reference) {{
    }}
}}
";

        return TestHelper.Verify(cslaSource);
    }

    [Fact]
    public Task InjectedParametersIgnored() {
        var cslaSource = $@"
using Csla;

namespace VerifyTests;

public class DummyBOWithParams {{
    [Fetch]
    private void Bar(int? id, string krznbf, decimal reference, [Inject] IDataPortalFactory dpf) {{
    }}
}}
";

        return TestHelper.Verify(cslaSource);
    }

    [Fact]
    public Task DefaultValueParameter() {
        var cslaSource = $@"
using Csla;

namespace VerifyTests;

public class DummyBOWithParams {{
    [Fetch]
    private void Bar(int? id, string krznbf, decimal reference, [Inject] IDataPortalFactory dpf, string abcdefg = """") {{
    }}
}}
";

        return TestHelper.Verify(cslaSource);
    }

    [Fact]
    public Task OnlyInjectedParameters() {
        var cslaSource = $@"
using Csla;

namespace VerifyTests;

public class DummyBOWithParams {{
    [Fetch]
    private void Bar([Inject] IDataPortalFactory dpf, [Inject] IChildDataPortalFactory cdpf) {{
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
    private void Bar(int? id, Foo.Bar krznbf, decimal reference, [Inject] IDataPortalFactory dpf) {{
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
        private void Bar() {{
        }}
    }}
}}
";

        return TestHelper.Verify(cslaSource);
    }

    [Theory]
    [MemberData(nameof(CSharpBuiltInTypesNullable))]
    public Task NullablePrimitves(string type) {
        var cslaSource = $@"
using Csla;

namespace VerifyTests;

public class DummyBOWithParams {{
    [Fetch]
    private void Bar({type}? krznbf) {{
    }}
}}
";

        return TestHelper.Verify(cslaSource, t => t.UseParameters(type));
    }

    [Fact]
    public Task GenericArity1Parameter() {
        var cslaSource = $@"
using Csla;
using System;
using System.Collections.Generic;

namespace VerifyTests;

public class DummyBOWithParams {{
    [Fetch]
    private void Bar(IEnumerable<Guid> krznbf) {{
    }}
}}
";

        return TestHelper.Verify(cslaSource);
    }

    [Fact]
    public Task GenericArity3Parameter() {
        var cslaSource = $@"
using Csla;
using GenericTests;

namespace VerifyTests;

public class DummyBOWithParams {{
    [Fetch]
    private void Bar(TestGenerica<RandomClass, RandomClass.RandomInner, RandomClass.RandomInner.ImportantEnum> krznbf) {{
    }}
}}
";

        var additionalTypes = $@"
namespace GenericTests;

public class TestGenerica<T1,T2,T3> {{
    public T1 Type1 {{ get; set; }}
    public T2 Type2 {{ get; set; }}
    public T3 Type3 {{ get; set; }}
}}

public class RandomClass {{
    public class RandomInner {{
        public enum ImportantEnum {{
            None = 0
        }}
    }}
}}
";

        return TestHelper.Verify(cslaSource, additionalTypes);
    }

    [Fact]
    public Task NullableEnum() {
        var cslaSource = $@"
using Csla;
using TestEnum;

namespace VerifyTests;

public class DummyBOWithParams {{
    [Fetch]
    private void Bar(SomeEnum? krznbf) {{
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
    private void Bar(SomeEnum krznbf) {{
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
    public Task InternalParameterMustMakeTheExtensionInternal() {
        var cslaSource = $@"
using Csla;
using TestInternal;

namespace VerifyTests;

public class DummyBOWithParams {{
    [Fetch]
    private void Bar(string a, SomeInternalType b) {{
    }}
}}
";

        var someInternalType = $@"
using System;

namespace TestInternal;

internal record SomeInternalType(string Name, Guid Id);
";

        return TestHelper.Verify(cslaSource, someInternalType);
    }

    [Fact]
    public Task GenericParameterDefaultValue() {
        var cslaSource = $@"
using Csla;
using System.Collections.Generic;

namespace VerifyTests;

public class DummyBOWithParams {{
    [Fetch]
    private void Bar(string a, int b = 1, List<string> list = null, string x = null, string z = """") {{
    }}
}}
";

        return TestHelper.Verify(cslaSource);
    }

    [Fact]
    public Task BuiltInTypeArrayDefaultValue() {
        var cslaSource = $@"
using Csla;

namespace VerifyTests;

public class DummyBOWithParams {{
    [Fetch]
    private void Bar(string a, int b = 1, int[] c = null, string x = null) {{
    }}
}}
";

        return TestHelper.Verify(cslaSource);
    }

    [Fact]
    public Task EnumParameterDefaultValue() {
        var cslaSource = $@"
using Csla;
using TestEnum;

namespace VerifyTests;

public class DummyBOWithParams {{
    [Fetch]
    private void Bar(SomeEnum krznbf = SomeEnum.Some) {{
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
    public Task BuiltInTypeArray(string type) {
        var cslaSource = $@"
using Csla;

namespace VerifyTests;

public class DummyBOWithParams {{
    [Fetch]
    private void Bar({type}[] krznbf) {{
    }}
}}
";

        return TestHelper.Verify(cslaSource, t => t.UseParameters(type));
    }

    public static TheoryData<string> CSharpBuiltInTypes => new(_csharpBuiltInTypes);

    public static TheoryData<string> CSharpBuiltInTypesNullable => new(_csharpBuiltInTypes.Where(t => t is not "string" and not "object"));

    private static readonly string[] _csharpBuiltInTypes = ["string", "bool", "byte", "sbyte", "char", "decimal", "double", "float", "int", "uint", "long", "ulong", "short", "ushort", "object"];
}