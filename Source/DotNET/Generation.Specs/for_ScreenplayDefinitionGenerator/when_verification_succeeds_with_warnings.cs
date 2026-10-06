// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_verification_succeeds_with_warnings : given.a_verifying_generator
{
    void Establish() => CompilerDiagnostics =
    [
        Diagnostic.Warning("PLAY0177", "Unknown read model", new(9, 3, "generated.play")),
        Diagnostic.Warning("PLAY0165", "Unknown type", new(7, 5, "generated.play"))
    ];

    void Because() => GenerateBoth();

    [Fact] void should_keep_contribution_generation_successful() => ContributionsResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_snapshot_generation_successful() => SnapshotResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_preserve_both_warnings_for_contributions() => ContributionsResult.Diagnostics.Count.ShouldEqual(2);
    [Fact] void should_preserve_both_warnings_for_snapshots() => SnapshotResult.Diagnostics.Count.ShouldEqual(2);
    [Fact] void should_preserve_warning_severity() => ContributionsResult.Diagnostics.All(diagnostic => diagnostic.Severity == GenerationDiagnosticSeverity.Warning).ShouldBeTrue();
    [Fact] void should_preserve_warning_message() => ContributionsResult.Diagnostics.Single(diagnostic => diagnostic.Code == "PLAY0165").Message.ShouldEqual("Unknown type");
    [Fact] void should_preserve_warning_location() => ContributionsResult.Diagnostics.Single(diagnostic => diagnostic.Code == "PLAY0165").Source.ShouldEqual(new SourceRange { Path = "generated.play", StartLine = 7, StartColumn = 5, EndLine = 7, EndColumn = 5 });
    [Fact] void should_use_deterministic_order() => ContributionsResult.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual(SnapshotResult.Diagnostics.Select(diagnostic => diagnostic.Code));
    [Fact] void should_preserve_snapshot_warning_details() => AdapterRunProjection(SnapshotResult.Diagnostics).ShouldEqual(AdapterRunProjection(ContributionsResult.Diagnostics));
}
