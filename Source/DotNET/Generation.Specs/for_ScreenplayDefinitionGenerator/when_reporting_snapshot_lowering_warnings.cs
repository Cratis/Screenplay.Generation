// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_reporting_snapshot_lowering_warnings : given.snapshot_omissions
{
    static readonly string[] _codes = [GenerationDiagnosticCodes.MissingConceptRepresentation, GenerationDiagnosticCodes.MissingConceptReference];

    GenerationFact[] _single = [];
    GenerationFact[] _double = [];
    GeneratedScreenplayDefinition _singleDirect = null!;
    GeneratedScreenplayDefinition _singleSnapshot = null!;
    GeneratedScreenplayDefinition _doubleDirect = null!;
    GeneratedScreenplayDefinition _doubleSnapshot = null!;
    GeneratedScreenplayDefinition _guardDirect = null!;
    GeneratedScreenplayDefinition _guardSnapshot = null!;
    GenerationDiagnostic _guardOriginal = null!;
    GenerationDiagnostic _lookalike = null!;

    void Because()
    {
        var subject = new SubjectId { Value = "dotnet://Banking/Concepts.ExternalCode" };
        var concept = new ArtifactFact
        {
            Id = new FactId { Value = "concept:ExternalCode" },
            Subject = subject,
            Definition = new ArtifactDefinition
            {
                Key = new ArtifactKey { Subject = subject, Kind = ArtifactKind.Concept },
                Name = "ExternalCode",
                File = "Concepts/ExternalCode.cs"
            },
            Evidence = new Evidence { Adapter = Adapter, Strength = EvidenceStrength.Exact, Source = new SourceRange { Path = "Concepts/ExternalCode.cs", StartLine = 1, StartColumn = 1, EndLine = 1, EndColumn = 1 } }
        };
        var placed = Event("CodeAssigned", "Assign", Property("code", "ExternalCode", subject));
        var second0 = (ArtifactFact)placed[0];
        var second = second0 with
        {
            Id = new FactId { Value = "event:CodeAssigned:second" },
            Evidence = second0.Evidence with { Source = new SourceRange { Path = "Accounts/Assign/Second.cs", StartLine = 9, StartColumn = 1, EndLine = 9, EndColumn = 1 } }
        };
        _single = [concept, .. placed];
        _double = [concept, .. placed, second];
        _singleDirect = Direct(_single);
        _singleSnapshot = GenerateSnapshot(_single);
        _doubleDirect = Direct(_double);
        _doubleSnapshot = GenerateSnapshot(_double);

        _guardOriginal = Lowering(_doubleDirect.Diagnostics, GenerationDiagnosticCodes.MissingConceptReference).Single();
        _lookalike = _guardOriginal with { Facts = [second.Id] };
        _guardDirect = Generator.Generate([new AdapterContribution { Adapter = Adapter, Facts = _double, Diagnostics = [_lookalike] }], new ScreenplayGenerationOptions { Domain = "Banking" });
        _guardSnapshot = Generator.Generate(Snapshot(Completed(Adapter, _double, [_lookalike])), new ScreenplayGenerationOptions { Domain = "Banking" });
    }

    [Fact] void should_publish_each_lowering_warning_once_in_the_definition() => new[] { _singleSnapshot, _doubleSnapshot }.All(result => _codes.All(code => Of(result.Diagnostics, code).Length == 1)).ShouldBeTrue();
    [Fact] void should_publish_each_lowering_warning_once_in_the_run() => new[] { _singleSnapshot, _doubleSnapshot }.All(result => _codes.All(code => Of(result.AdapterRun.Diagnostics, code).Length == 1)).ShouldBeTrue();
    [Fact] void should_match_the_direct_payloads_for_one_contributor() => Projection(Lowering(_singleSnapshot.Diagnostics)).ShouldEqual(Projection(Lowering(_singleDirect.Diagnostics)));
    [Fact] void should_match_the_direct_payloads_for_two_contributors() => Projection(Lowering(_doubleSnapshot.Diagnostics)).ShouldEqual(Projection(Lowering(_doubleDirect.Diagnostics)));
    [Fact] void should_match_the_direct_source() => _doubleSnapshot.Source.ShouldEqual(_doubleDirect.Source);
    [Fact] void should_link_the_concept_record_to_its_own_warning() => RecordOf(_singleSnapshot, "concept:ExternalCode").Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.MissingConceptRepresentation && diagnostic.Facts.Any(link => link.Value == "concept:ExternalCode")).ShouldBeTrue();
    [Fact] void should_link_each_event_record_to_its_own_warning() => new[] { "event:CodeAssigned", "event:CodeAssigned:second" }.All(id => RecordOf(_doubleSnapshot, id).Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.MissingConceptReference && diagnostic.Facts.Any(link => link.Value == id))).ShouldBeTrue();
    [Fact] void should_keep_a_genuinely_linked_lookalike_in_the_definition() => Of(_guardSnapshot.Diagnostics, GenerationDiagnosticCodes.MissingConceptReference).Length.ShouldEqual(Of(_guardDirect.Diagnostics, GenerationDiagnosticCodes.MissingConceptReference).Length);
    [Fact] void should_publish_both_originals_in_the_definition() => Of(_guardSnapshot.Diagnostics, GenerationDiagnosticCodes.MissingConceptReference).Length.ShouldEqual(2);
    [Fact] void should_publish_both_originals_in_the_run() => Of(_guardSnapshot.AdapterRun.Diagnostics, GenerationDiagnosticCodes.MissingConceptReference).Length.ShouldEqual(2);

    static GenerationDiagnostic[] Lowering(IEnumerable<GenerationDiagnostic> diagnostics, params string[] codes) =>
        [.. diagnostics.Where(diagnostic => (codes.Length == 0 ? _codes : codes).Contains(diagnostic.Code))];

    GeneratedScreenplayDefinition Direct(GenerationFact[] facts) =>
        Generator.Generate([Contribution(facts)], new ScreenplayGenerationOptions { Domain = "Banking" });
}
