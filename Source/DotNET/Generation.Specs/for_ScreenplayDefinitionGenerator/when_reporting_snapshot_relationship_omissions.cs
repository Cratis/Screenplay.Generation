// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_reporting_snapshot_relationship_omissions : given.snapshot_omissions
{
    GeneratedScreenplayDefinition[] _results = [];
    GeneratedScreenplayDefinition _distinct = null!;
    GeneratedScreenplayDefinition _separators = null!;
    GeneratedScreenplayDefinition _foreign = null!;

    void Because()
    {
        var target = Document.Subject;
        GenerationFact[][] shapes = [LegacyDocument("artifact", Document), DeclaredDocument("artifact", Document)];
        _results =
        [
            .. shapes.SelectMany(shape => new[]
            {
                GenerateSnapshot([.. shape, Deletes("delete-1", target, 20)]),
                GenerateSnapshot([.. shape, Deletes("delete-1", target, 20), Deletes("delete-2", target, 21)]),
                GenerateSnapshot([Deletes("delete-2", target, 21), Deletes("delete-1", target, 20), .. shape.Reverse()])
            })
        ];
        _distinct = GenerateSnapshot(
        [
            Deletes("by-target", SubjectOf("Documents.Course"), 20),
            Deletes("by-kind", target, 21, kind: RelationshipKind.Updates),
            Deletes("by-source", target, 22, SubjectOf("Commands.Other"))
        ]);
        _separators = GenerateSnapshot(
        [
            Deletes("a", SubjectOf("Documents.B|C"), 20, SubjectOf("Commands.A")),
            Deletes("b", SubjectOf("Documents.C"), 21, SubjectOf("Commands.A|Documents.B"))
        ]);
        var foreign = new GenerationDiagnostic
        {
            Code = GenerationDiagnosticCodes.UnsupportedRelationship,
            Severity = GenerationDiagnosticSeverity.Warning,
            Message = Of(_results[0].Diagnostics, GenerationDiagnosticCodes.UnsupportedRelationship).Single().Message,
            Facts = [new FactId { Value = "delete-1" }]
        };
        _foreign = Generator.Generate(
            Snapshot(Completed(Adapter, [.. LegacyDocument("artifact", Document), Deletes("delete-1", target, 20)], [foreign])),
            new ScreenplayGenerationOptions { Domain = "Banking" });
    }

    [Fact] void should_report_one_omission_in_every_outward_list() => _results.All(result => Of(result.Diagnostics, GenerationDiagnosticCodes.UnsupportedRelationship).Length == 1).ShouldBeTrue();
    [Fact] void should_report_one_omission_in_every_run_list() => _results.All(result => Of(result.AdapterRun.Diagnostics, GenerationDiagnosticCodes.UnsupportedRelationship).Length == 1).ShouldBeTrue();
    [Fact] void should_report_equal_outward_payloads() => _results.Select(result => Projection(result.Diagnostics)).Distinct().Count().ShouldEqual(1);
    [Fact] void should_report_equal_run_payloads() => _results.Select(result => Projection(result.AdapterRun.Diagnostics)).Distinct().Count().ShouldEqual(1);
    [Fact] void should_keep_the_generated_screenplay_stable() => _results.Select(result => result.Source).Distinct().Count().ShouldEqual(1);
    [Fact] void should_keep_both_fact_records() => new[] { "delete-1", "delete-2" }.All(id => RecordOf(_results[1], id).Disposition == GenerationFactDisposition.OmittedWithDiagnostic).ShouldBeTrue();
    [Fact] void should_keep_each_invocation_site() => RecordOf(_results[1], "delete-2").Fact.Evidence.Source!.StartLine.ShouldEqual(21);
    [Fact] void should_link_each_record_diagnostic_to_its_fact() => new[] { "delete-1", "delete-2" }.All(id => RecordOf(_results[1], id).Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.UnsupportedRelationship && diagnostic.Facts.Any(link => link.Value == id))).ShouldBeTrue();
    [Fact] void should_report_distinct_source_kind_and_target_separately() => Of(_distinct.Diagnostics, GenerationDiagnosticCodes.UnsupportedRelationship).Length.ShouldEqual(3);
    [Fact] void should_report_distinct_tuples_with_separators_separately() => Of(_separators.Diagnostics, GenerationDiagnosticCodes.UnsupportedRelationship).Length.ShouldEqual(2);
    [Fact] void should_keep_a_foreign_diagnostic_with_the_same_prose() => Of(_foreign.Diagnostics, GenerationDiagnosticCodes.UnsupportedRelationship).Length.ShouldEqual(2);
}
