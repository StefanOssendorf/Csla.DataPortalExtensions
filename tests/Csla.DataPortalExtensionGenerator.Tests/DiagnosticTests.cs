﻿using FluentAssertions;
using Microsoft.CodeAnalysis;
using Ossendorf.Csla.DataPortalExtensionGenerator.Tests.Helper;

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

        DiagnosticsTester.Verify(extensionClass, expectedDiagnosticId: "DPEGEN001");
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

        DiagnosticsTester.Verify(ValidExtensionClass, cslaClass, "DPEGEN002");
    }

    [Fact]
    public void WhenUsingNullableAnnotationsInDisabledContextWithSuppressWarningsCS8632MustNotBeReported() {
        const string cslaClass = $@"
#nullable enable
using Csla;

namespace SomeCslaClass;

public class Foo {{
    [Fetch]
    private void Bar(string? x, string? z = null){{ }}
}}
";
        var globalCompilerOptions = TestAnalyzerConfigOptionsProvider.Create(new[] {
            KeyValuePair.Create("DataPortalExtensionGen_SuppressWarningCS8669", bool.TrueString),
            KeyValuePair.Create("DataPortalExtensionGen_NullableContext", "disable")
        });

        DiagnosticsTester.Verify(ValidExtensionClass, cslaClass, globalCompilerOptions, diagnostics => diagnostics.Should().BeEmpty(), d => d.Should().BeEmpty(), NullableContextOptions.Disable);
    }

    [Fact]
    public void WhenNullableContextConfigValueIsUnknownItMustGetDiagnosticDPEGEN003() {
        const string cslaClass = $@"
using Csla;

namespace SomeCslaClass;

public class Foo {{
    [Fetch]
    private void Bar(int a){{ }}
}}
";
        var globalCompilerOptions = TestAnalyzerConfigOptionsProvider.Create(new[] {
            KeyValuePair.Create("DataPortalExtensionGen_NullableContext", "Unknown")
        });

        DiagnosticsTester.Verify(ValidExtensionClass, cslaClass, "DPEGEN003", globalCompilerOptions);
    }

    [Fact]
    public void WhenSuppressWarningCS8669ConfigValueIsUnknownItMustGetDiagnosticDPEGEN004() {
        const string cslaClass = $@"
using Csla;

namespace SomeCslaClass;

public class Foo {{
    [Fetch]
    private void Bar(int a){{ }}
}}
";
        var globalCompilerOptions = TestAnalyzerConfigOptionsProvider.Create(new[] {
            KeyValuePair.Create("DataPortalExtensionGen_SuppressWarningCS8669", "NotBooleanParseable")
        });

        DiagnosticsTester.Verify(ValidExtensionClass, cslaClass, "DPEGEN004", globalCompilerOptions);
    }
}