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

    [Create]
    private void ACreate() {
    }

    [Delete]
    private void ADelete() {
    }
}
""";

        StageCachingTester.VerfiyStageCaching(cslaSource);
    }
}
