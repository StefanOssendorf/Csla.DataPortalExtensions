using Ossendorf.Csla.DataPortalExtensionGenerator.Tests.Helper;

namespace Ossendorf.Csla.DataPortalExtensionGenerator.Tests;

public class SourceGeneratorStageCachingTests {
    [Fact]
    public void EachCslaPortalOperationStageIsCachedCorrectly() {
        var cslaSource = """
using Csla;
using System;

namespace VerifyTests;

public class DummyBOWithParams {
    [Fetch]
    private void AFetch() {
    }

    [FetchChild]
    private void AFetchChild() {
    }

    [Create]
    private void ACreate() {
    }

    [CreateChild]
    private void ACreateChild() {
    }

    [Delete]
    private void ADelete() {
    }
}
""";

        StageCachingTester.VerfiyStageCaching(cslaSource);
    }
}
