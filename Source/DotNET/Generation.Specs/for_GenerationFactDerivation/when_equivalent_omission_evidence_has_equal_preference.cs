// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_equivalent_omission_evidence_has_equal_preference : given.occurrence_omissions
{
    GenerationDiagnostic _first;
    GenerationDiagnostic _reversed;
    SourceRange _expected;
    void Because()
    {
        var earlierProducer = Candidate("a-producer");
        var laterProducer = Candidate("z-producer");
        earlierProducer = Located(earlierProducer, Invocation with { Path = "a-display.cs", FileIdentity = new() { Project = "project://z", Path = "a.cs" } });
        laterProducer = Located(laterProducer, Invocation with { Path = "z-display.cs", FileIdentity = new() { Project = "project://a", Path = "z.cs" } });
        _expected = laterProducer.Evidence.Source;
        var facts = CandidateFacts(earlierProducer, laterProducer);
        _first = Omissions(Derive(facts)).Single();
        _reversed = Omissions(Derive([.. facts.Reverse()])).Single();
    }
    [Fact] void should_use_stable_project_identity_before_display_path_or_producer() => _first.Source.ShouldEqual(_expected);
    [Fact] void should_keep_the_same_complete_payload_and_lineage_when_reversed() => JsonSerializer.Serialize(_reversed).ShouldEqual(JsonSerializer.Serialize(_first));
    [Fact] void should_use_the_full_source_range_to_break_a_tie()
    {
        var late = Located(Candidate("a-late"), Invocation with { EndColumn = 25 });
        var early = Located(Candidate("z-early"), Invocation with { EndColumn = 23 });
        Omissions(Derive(CandidateFacts(late, early))).Single().Source.ShouldEqual(early.Evidence.Source);
    }
    [Fact] void should_use_ordinal_fallback_paths_when_stable_identity_is_absent()
    {
        var lower = Located(Candidate("a-lower"), Invocation with { Path = "z.cs" });
        var upper = Located(Candidate("z-upper"), Invocation with { Path = "A.cs" });
        Omissions(Derive(CandidateFacts(lower, upper))).Single().Source.ShouldEqual(upper.Evidence.Source);
    }
    [Fact] void should_use_structural_identity_only_after_identical_source_coordinates()
    {
        var first = Located(Candidate("a-producer"), Invocation);
        var second = Located(Candidate("z-producer"), Invocation);
        Omissions(Derive(CandidateFacts(second, first))).Single().Facts.Single().Value.ShouldEqual("a-producer:observation");
    }
    [Fact] void should_keep_a_single_candidates_original_payload_and_location()
    {
        var candidate = Candidate("singleton");
        var result = Omissions(Derive(CandidateFacts(candidate))).Single();
        JsonSerializer.Serialize(result with { Lineage = null }).ShouldEqual(JsonSerializer.Serialize(candidate.Definition.Diagnostic));
    }

    static OccurrenceOmissionDiagnosticFact Located(OccurrenceOmissionDiagnosticFact candidate, SourceRange source) => candidate with
    {
        Evidence = candidate.Evidence with { Source = source }, Definition = candidate.Definition with { Diagnostic = candidate.Definition.Diagnostic with { Source = source } }
    };
}
