﻿//HintName: GeneratorTests.DataPortalExtensions.g.cs
// <auto-generated />
#nullable enable

namespace GeneratorTests
{
    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Generated by the Ossendorf.Csla.DataPortalExtensionsGenerators source generator.")]
    static partial class DataPortalExtensions
    {
        public static global::System.Threading.Tasks.Task<global::VerifyTests.DummyBOWithParams> Bar(this global::Csla.IDataPortal<global::VerifyTests.DummyBOWithParams> __dpeg_source, string a, int b = 1, int[]? c = null, string? x = null) => __dpeg_source.FetchAsync(a, b, c, x);
    }
}
