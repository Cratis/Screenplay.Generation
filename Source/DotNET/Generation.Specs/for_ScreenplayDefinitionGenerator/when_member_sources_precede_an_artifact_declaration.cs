// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_member_sources_precede_an_artifact_declaration : given.artifact_diagnostic_sources
{
    GeneratedScreenplayDefinition[] _results;
    GenerationFact[] _members;
    ArtifactDeclarationFact _declaration;
    GeneratedScreenplayDefinition _snapshot;

    void Establish()
    {
        _declaration = Declaration("document", Document);
        _members = Members("document", Document);
    }

    void Because()
    {
        _results =
        [
            Generate(Contribution(_declaration, Placement(Document)), Contribution(_members)),
            Generate(Contribution([.. _members.Reverse()]), Contribution(Placement(Document), _declaration))
        ];
        _snapshot = GenerateSnapshot([_declaration, Placement(Document), .. _members]);
    }

    [Fact] void should_report_the_occurrence_source_in_every_input_order() => _results.All(result => Omissions(result).Single().Source == _declaration.Evidence.Source).ShouldBeTrue();
    [Fact] void should_keep_one_unsupported_artifact_warning() => _results.All(result => Omissions(result).Single().Code == GenerationDiagnosticCodes.UnsupportedArtifact).ShouldBeTrue();
    [Fact] void should_keep_the_warning_severity_and_outcome() => _results.All(result => Omissions(result).Single() is { Severity: GenerationDiagnosticSeverity.Warning, Outcome: GenerationDiagnosticOutcome.Unsupported }).ShouldBeTrue();
    [Fact] void should_keep_the_warning_subject() => _results.All(result => Omissions(result).Single().Subject == Document.Subject).ShouldBeTrue();
    [Fact] void should_not_emit_the_document() => _results.All(result => !result.Source.Contains("Student", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_keep_every_member_source_in_graph_evidence() => _results.All(result => _members.All(member => result.Graph.Artifacts.Single().Variants.Single().Evidence.Contains(member.Evidence))).ShouldBeTrue();
    [Fact] void should_keep_member_and_type_use_provenance_in_the_run() => _members.All(member => _snapshot.AdapterRun.Facts.Any(record => record.Fact.Id == member.Id && record.Fact.Evidence == member.Evidence)).ShouldBeTrue();
    [Fact] void should_report_associated_run_diagnostics_at_the_occurrence() => Omissions(_snapshot).All(diagnostic => diagnostic.Source == _declaration.Evidence.Source).ShouldBeTrue();
    [Fact] void should_keep_the_run_artifact_omissions() => Omissions(_snapshot).ShouldNotBeEmpty();
    [Fact] void should_keep_the_canonical_output_stable() => _results.Select(result => result.Source).Distinct().Count().ShouldEqual(1);
}
