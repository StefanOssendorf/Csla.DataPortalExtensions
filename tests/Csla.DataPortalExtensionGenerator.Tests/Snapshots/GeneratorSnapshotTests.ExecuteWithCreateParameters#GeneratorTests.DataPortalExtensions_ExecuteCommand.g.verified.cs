﻿//HintName: GeneratorTests.DataPortalExtensions_ExecuteCommand.g.cs
// <auto-generated />
#nullable enable

namespace GeneratorTests
{
    static partial class DataPortalExtensions
    {
        public static global::System.Threading.Tasks.Task<global::ExecuteTests.DummyCmd> ExecuteWithoutParameters(this global::Csla.IDataPortal<global::ExecuteTests.DummyCmd> __dpeg_source, global::ExecuteTests.DummyCmd __dpeg_command) => __dpeg_source.ExecuteAsync(__dpeg_command);
        public static async global::System.Threading.Tasks.Task<global::ExecuteTests.DummyCmd> CreateAndExecuteCommand(this global::Csla.IDataPortal<global::ExecuteTests.DummyCmd> __dpeg_source, int a, string x) {
            var __dpeg_tmp_cmd = await __dpeg_source.CreateTest(a, x);
            return await __dpeg_source.ExecuteWithoutParameters(__dpeg_tmp_cmd);
        }
    }
}
