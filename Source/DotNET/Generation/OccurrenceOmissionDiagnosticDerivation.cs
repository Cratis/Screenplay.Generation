// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation;

static class OccurrenceOmissionDiagnosticDerivation
{
    public static GenerationDerivationRuleIdentity Rule { get; } = new() { Id = "cratis.screenplay.occurrence-omission-diagnostic", Version = "1.0.0" };

    public static ImmutableArray<GenerationDerivationRuleRecord> Derive(ImmutableArray<GenerationFactRecord> records, EffectiveGenerationInputs effective)
    {
        var facts = records.Select(record => record.Fact).ToArray();
        var candidates = facts.OfType<OccurrenceOmissionDiagnosticFact>().ToArray();
        if (candidates.Length == 0)
        {
            return [];
        }

        var eligible = candidates.Where(candidate => LocalSupport(candidate, facts, effective)).ToArray();
        var diagnostics = candidates.Except(eligible).Select(OccurrenceOmissionDiagnosticValidation.Invalid)
            .Concat(eligible.GroupBy(candidate => (candidate.Definition.Family.Value, Occurrence: Structural.ArtifactKey(candidate.Definition.Occurrence)))
                .SelectMany(group => Coalesce([.. group], facts, effective))).OrderBy(Canonical.Diagnostic, StringComparer.Ordinal).ToImmutableArray();
        return
        [
            new()
            {
                Rule = Rule, Inputs = [.. diagnostics.SelectMany(diagnostic => diagnostic.Lineage?.Inputs ?? diagnostic.Facts).Distinct().OrderBy(id => id.Value, StringComparer.Ordinal)],
                Outputs = [], Diagnostics = diagnostics
            }
        ];
    }

    static bool LocalSupport(OccurrenceOmissionDiagnosticFact candidate, GenerationFact[] facts, EffectiveGenerationInputs effective) =>
        DiagnosticIntentLineage.LocalSupportsExist(candidate, candidate.Definition.Diagnostic, facts) &&
        OccurrenceOmissionDiagnosticValidation.SupportsMatch(candidate, effective.OriginalFacts);

    static IEnumerable<GenerationDiagnostic> Coalesce(OccurrenceOmissionDiagnosticFact[] candidates, GenerationFact[] facts, EffectiveGenerationInputs effective)
    {
        var partitions = candidates.GroupBy(candidate => PayloadKey(candidate.Definition.Diagnostic), StringComparer.Ordinal).ToArray();
        foreach (var partition in partitions)
        {
            var winner = Ordered(partition).First();
            yield return winner.Definition.Diagnostic with { Lineage = Lineage(partition, facts, effective) };
        }

        if (partitions.Length > 1)
        {
            var winner = Ordered(candidates).First();
            yield return new()
            {
                Code = GenerationDiagnosticCodes.ConflictingOccurrenceOmissionDiagnostic, Severity = GenerationDiagnosticSeverity.Warning,
                Outcome = GenerationDiagnosticOutcome.Conflict, Message = "Incompatible omission diagnostics target one family and exact artifact occurrence",
                Source = winner.Definition.Diagnostic.Source, Subject = winner.Subject, Facts = winner.Definition.Diagnostic.Facts,
                Lineage = Lineage(candidates, facts, effective)
            };
        }
    }

    static string PayloadKey(GenerationDiagnostic diagnostic) => Structural.Diagnostic(diagnostic with { Source = null, Facts = [], Lineage = null });

    static IOrderedEnumerable<OccurrenceOmissionDiagnosticFact> Ordered(IEnumerable<OccurrenceOmissionDiagnosticFact> candidates) =>
        candidates.OrderByDescending(candidate => OccurrenceOmissionDiagnosticValidation.Rank(candidate.Definition.Preference))
            .ThenBy(candidate => candidate.Definition.Diagnostic.Source!.FileIdentity?.Project, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Definition.Diagnostic.Source!.FileIdentity?.Path ?? candidate.Definition.Diagnostic.Source!.Path, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Definition.Diagnostic.Source!.StartLine).ThenBy(candidate => candidate.Definition.Diagnostic.Source!.StartColumn)
            .ThenBy(candidate => candidate.Definition.Diagnostic.Source!.EndLine).ThenBy(candidate => candidate.Definition.Diagnostic.Source!.EndColumn)
            .ThenBy(Structural.Fact, StringComparer.Ordinal);

    static GenerationFactLineage Lineage(IEnumerable<OccurrenceOmissionDiagnosticFact> candidates, GenerationFact[] facts, EffectiveGenerationInputs effective) =>
        DiagnosticIntentLineage.Create(
            Rule,
            candidates.SelectMany(candidate => DiagnosticIntentLineage.LocalInputs(candidate, candidate.Definition.Diagnostic, facts)
                .Concat(facts.OfType<BackendScopeFact>().Where(scope => scope.Definition.Key == candidate.Definition.Occurrence.Scope))),
            effective);
}
