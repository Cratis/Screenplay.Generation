// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_verification_reports_errors_and_warnings : given.a_verifying_generator
{
    void Establish() => CompilerDiagnostics =
    [
        Diagnostic.Warning("PLAY0165", "Unknown type", new(7, 5, "generated.play")),
        Diagnostic.Error("PLAY-FAILED", "Verification failed", new(9, 3, "generated.play"))
    ];

    void Because() => GenerateBoth();

    [Fact] void should_keep_contribution_generation_unsuccessful() => ContributionsResult.IsSuccess.ShouldBeFalse();
    [Fact] void should_keep_snapshot_generation_unsuccessful() => SnapshotResult.IsSuccess.ShouldBeFalse();
    [Fact] void should_keep_the_failure_contract() => ContributionsResult.Diagnostics.Single(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.DocumentDidNotCompile).Message.ShouldEqual("The generated Screenplay document did not compile: PLAY-FAILED Verification failed");
    [Fact] void should_keep_the_failure_severity() => ContributionsResult.Diagnostics.Single(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.DocumentDidNotCompile).Severity.ShouldEqual(GenerationDiagnosticSeverity.Error);
    [Fact] void should_also_preserve_the_warning() => ContributionsResult.Diagnostics.Single(diagnostic => diagnostic.Code == "PLAY0165").Severity.ShouldEqual(GenerationDiagnosticSeverity.Warning);
    [Fact] void should_preserve_snapshot_diagnostics() => AdapterRunProjection(SnapshotResult.Diagnostics).ShouldEqual(AdapterRunProjection(ContributionsResult.Diagnostics));
}
