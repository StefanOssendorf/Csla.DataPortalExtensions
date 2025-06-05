using Microsoft.CodeAnalysis;
using Ossendorf.Csla.DataPortalExtensionGenerator.Diagnostics;
using Ossendorf.Csla.DataPortalExtensionGenerator.Internals;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Ossendorf.Csla.DataPortalExtensionGenerator.Configuration;
internal static class OptionsGeneratorPart {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IncrementalValueProvider<GeneratorOptions> GetGeneratorOptions(IncrementalGeneratorInitializationContext context) {

        var optionsAndDiagnostics = context.AnalyzerConfigOptionsProvider.Select((options, _) => {

            if (!TryGetGlobalOption(ConfigConstants.MethodPrefix, out var methodPrefix) || methodPrefix is null) {
                methodPrefix = "";
            }

            if (!TryGetGlobalOption(ConfigConstants.MethodSuffix, out var methodSuffix) || methodSuffix is null) {
                methodSuffix = "";
            }

            var errors = new List<DiagnosticInfo>();
            var nullableContextOptions = NullableContextOptions.Enable;
            if (TryGetGlobalOption(ConfigConstants.NullableContext, out var nullabilityContext)) {
                if (nullabilityContext.Equals("Disable", StringComparison.OrdinalIgnoreCase)) {
                    nullableContextOptions = NullableContextOptions.Disable;
                } else if (!nullabilityContext.Equals("Enable", StringComparison.OrdinalIgnoreCase)) {
                    errors.Add(NullableContextValueDiagnostic.Create(nullabilityContext));
                }
            }

            var suppressWarningCS8669 = false;
            if (TryGetGlobalOption(ConfigConstants.SuppressWarningCS8669, out var suppressWarningString)) {
                if (!bool.TryParse(suppressWarningString, out suppressWarningCS8669)) {
                    suppressWarningCS8669 = false;
                    errors.Add(SuppressWarningCS8669ValueDiagnostic.Create(suppressWarningString));
                }
            }

            var tfms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (TryGetGlobalOption(ConfigConstants.TargetFramework, out var targetFramework) && !string.IsNullOrWhiteSpace(targetFramework)) {
                tfms.Add(targetFramework);
            }

            if (TryGetGlobalOption(ConfigConstants.TargetFrameworks, out var targetFrameworks) && !string.IsNullOrWhiteSpace(targetFrameworks)) {
                var splitTfms = targetFrameworks.Split(';', StringSplitOptions.RemoveEmptyEntries);
                for (var i = 0; i < splitTfms.Length; i++) {
                    tfms.Add(splitTfms[i]);
                }
            }

            return new Result<GeneratorOptions>(new GeneratorOptions(methodPrefix, methodSuffix, nullableContextOptions, suppressWarningCS8669, new EquatableArray<string>([.. tfms])), new EquatableArray<DiagnosticInfo>([.. errors]));

            bool TryGetGlobalOption(string key, [NotNullWhen(true)] out string? value) => options.GlobalOptions.TryGetValue($"build_property.{key}", out value) && !string.IsNullOrWhiteSpace(value);
        }).WithTrackingName(TrackingNames.ExtractOptions);

        context.RegisterSourceOutput(
            optionsAndDiagnostics.SelectMany((r, _) => r.Errors),
            static (ctx, info) => ctx.ReportDiagnostic(info)
        );

        return optionsAndDiagnostics.Select((r, _) => r.Value).WithTrackingName(TrackingNames.SelectOptions);
    }
}
