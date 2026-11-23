// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_omission_occurrences_have_distinct_scopes_roles_or_families : given.occurrence_omissions
{
    GenerationFact[] _facts;
    GenerationDiagnostic[] _diagnostics;
    void Because()
    {
        var firstScope = new BackendScopeKey { Store = new() { Value = "symbol://stores/Store" }, EventModelName = "First" };
        var secondScope = firstScope with { EventModelName = "Second" };
        var baseCandidate = Candidate("unscoped");
        var first = ForOccurrence(Candidate("first"), baseCandidate.Definition.Occurrence with { Scope = firstScope });
        var second = ForOccurrence(Candidate("second"), baseCandidate.Definition.Occurrence with { Scope = secondScope });
        var role = ForOccurrence(Candidate("role"), baseCandidate.Definition.Occurrence with { Kind = ArtifactKind.ReadModel });
        var subject = ForOccurrence(Candidate("subject"), baseCandidate.Definition.Occurrence with { Subject = new() { Value = "symbol://types/Other" } });
        var family = Candidate("family");
        family = family with { Definition = family.Definition with { Family = new() { Value = "other-family" } } };
        _facts = [.. CandidateFacts(baseCandidate, first, second, role, subject, family), ScopeFact(firstScope), ScopeFact(secondScope)];
        _diagnostics = Omissions(Derive(_facts));
    }
    [Fact] void should_keep_full_occurrences_and_families_separate() => _diagnostics.Length.ShouldEqual(6);
    [Fact] void should_keep_them_separate_in_final_generator_diagnostics() => new ScreenplayDefinitionGenerator().Generate(OmissionSnapshot(_facts), new() { Domain = "Diagnostics" }).Diagnostics.Count(diagnostic => diagnostic.Code == "OMIT0044").ShouldEqual(6);
    [Fact] void should_retain_the_exact_scope_evidence() => _diagnostics.Count(diagnostic => diagnostic.Lineage.Evidence.Any(evidence => evidence.Adapter == ApplicationAdapter)).ShouldEqual(2);

    static OccurrenceOmissionDiagnosticFact ForOccurrence(OccurrenceOmissionDiagnosticFact candidate, ArtifactKey occurrence) => candidate with
    {
        Subject = occurrence.Subject, Definition = candidate.Definition with { Occurrence = occurrence, Diagnostic = candidate.Definition.Diagnostic with { Subject = occurrence.Subject } }
    };
}
