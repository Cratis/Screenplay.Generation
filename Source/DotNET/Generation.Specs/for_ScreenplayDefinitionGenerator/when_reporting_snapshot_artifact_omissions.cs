// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_reporting_snapshot_artifact_omissions : given.snapshot_omissions
{
    static readonly ArtifactKey _other = new()
    {
        Subject = SubjectOf("Documents.Course"),
        Kind = ArtifactKind.Document
    };

    GeneratedScreenplayDefinition[] _results = [];
    GeneratedScreenplayDefinition _twoArtifacts = null!;

    void Because()
    {
        _results =
        [
            GenerateSnapshot(LegacyDocument("legacy-1", Document)),
            GenerateSnapshot([.. LegacyDocument("legacy-1", Document), .. LegacyDocument("legacy-2", Document, 6).Take(1)]),
            GenerateSnapshot([.. LegacyDocument("legacy-2", Document, 6).Take(1), .. LegacyDocument("legacy-1", Document)]),
            GenerateSnapshot(DeclaredDocument("declared-1", Document)),
            GenerateSnapshot([.. DeclaredDocument("declared-1", Document), Declaration("declared-2", Document, 6)]),
            GenerateSnapshot([Declaration("declared-2", Document, 6), .. DeclaredDocument("declared-1", Document)])
        ];
        _twoArtifacts = GenerateSnapshot([.. LegacyDocument("legacy-1", Document), .. DeclaredDocument("declared-other", _other, 7)]);
    }

    [Fact] void should_report_one_omission_in_every_outward_list() => _results.All(result => Of(result.Diagnostics, GenerationDiagnosticCodes.UnsupportedArtifact).Length == 1).ShouldBeTrue();
    [Fact] void should_report_one_omission_in_every_run_list() => _results.All(result => Of(result.AdapterRun.Diagnostics, GenerationDiagnosticCodes.UnsupportedArtifact).Length == 1).ShouldBeTrue();
    [Fact] void should_report_equal_outward_payloads() => _results.Select(result => Projection(result.Diagnostics)).Distinct().Count().ShouldEqual(1);
    [Fact] void should_report_equal_run_payloads() => _results.Select(result => Projection(result.AdapterRun.Diagnostics)).Distinct().Count().ShouldEqual(1);
    [Fact] void should_report_equal_sources() => _results.Select(result => Of(result.Diagnostics, GenerationDiagnosticCodes.UnsupportedArtifact).Single().Source).Distinct().Count().ShouldEqual(1);
    [Fact] void should_keep_the_generated_screenplay_stable() => _results.Select(result => result.Source).Distinct().Count().ShouldEqual(1);
    [Fact] void should_keep_every_fact_record() => _results[1].AdapterRun.Facts.Select(record => record.Fact.Id.Value).Order().ShouldContainOnly("legacy-1", "legacy-2", "placement");
    [Fact] void should_keep_each_legacy_contributor_link() => new[] { "legacy-1", "legacy-2" }.All(id => RecordOf(_results[1], id).Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.UnsupportedArtifact && diagnostic.Facts.Any(link => link.Value == id))).ShouldBeTrue();
    [Fact] void should_keep_each_legacy_contributor_evidence() => RecordOf(_results[1], "legacy-2").Fact.Evidence.Source!.StartLine.ShouldEqual(6);
    [Fact] void should_link_each_declaration_to_the_omission() => new[] { "declared-1", "declared-2" }.All(id => RecordOf(_results[4], id).Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.UnsupportedArtifact && diagnostic.Facts.Any(link => link.Value == id))).ShouldBeTrue();
    [Fact] void should_keep_declarations_provenance_only() => new[] { "declared-1", "declared-2" }.All(id => RecordOf(_results[4], id).Disposition == GenerationFactDisposition.ProvenanceOnly).ShouldBeTrue();
    [Fact] void should_keep_each_declaration_evidence() => RecordOf(_results[4], "declared-2").Fact.Evidence.Source!.StartLine.ShouldEqual(6);
    [Fact] void should_report_each_omitted_artifact() => Of(_twoArtifacts.Diagnostics, GenerationDiagnosticCodes.UnsupportedArtifact).Length.ShouldEqual(2);
    [Fact] void should_report_each_omitted_artifact_in_the_run() => Of(_twoArtifacts.AdapterRun.Diagnostics, GenerationDiagnosticCodes.UnsupportedArtifact).Length.ShouldEqual(2);
}
