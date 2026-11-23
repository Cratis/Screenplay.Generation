// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation;

static class OmissionDiagnosticCoalescer
{
    static readonly GenerationDerivationRuleIdentity _rule = new() { Id = "cratis.screenplay.omission-diagnostic-group", Version = "1.0.0" };

    public static ImmutableArray<GenerationDiagnostic> Coalesce(ImmutableArray<GenerationFactRecord> records, ImmutableArray<GenerationDiagnostic> diagnostics, IEnumerable<GenerationDiagnostic>? blockers = null)
    {
        var precedence = blockers is null ? diagnostics : [.. diagnostics.Concat(blockers)];
        var replacements = records.Where(record => record.Disposition == GenerationFactDisposition.ProvenanceOnly)
            .Select(record => record.Fact).OfType<OmittedFactDiagnosticGroupFact>()
            .Where(group => Replaceable(group, records, precedence)).ToArray();
        var covered = replacements.SelectMany(group => group.Definition.CoveredFacts).ToHashSet();
        return [.. diagnostics.Where(diagnostic => !CoveredFallback(diagnostic, covered)).Concat(replacements.Select(group => Project(group, records)))
            .GroupBy(Canonical.Diagnostic, StringComparer.Ordinal).OrderBy(group => group.Key, StringComparer.Ordinal).Select(group => group.First())];
    }

    static bool Replaceable(OmittedFactDiagnosticGroupFact group, ImmutableArray<GenerationFactRecord> records, ImmutableArray<GenerationDiagnostic> diagnostics) =>
        group.Definition.CoveredFacts.All(id => GenericOmission(id, records) &&
            !diagnostics.Any(diagnostic => diagnostic.Code != GenerationDiagnosticCodes.OmittedGenerationFact && References(diagnostic, id)));

    static bool GenericOmission(FactId id, ImmutableArray<GenerationFactRecord> records)
    {
        var matches = records.Where(record => record.Fact.Id == id).ToArray();
        return matches.Length == 1 && matches[0].Disposition == GenerationFactDisposition.OmittedWithDiagnostic &&
            !matches[0].Diagnostics.IsEmpty && matches[0].Diagnostics.All(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.OmittedGenerationFact);
    }

    static bool References(GenerationDiagnostic diagnostic, FactId id) => diagnostic.Facts.Contains(id) || (diagnostic.Lineage?.Inputs.Contains(id) ?? false);

    static bool CoveredFallback(GenerationDiagnostic diagnostic, HashSet<FactId> covered) =>
        diagnostic.Code == GenerationDiagnosticCodes.OmittedGenerationFact && !diagnostic.Facts.IsEmpty && diagnostic.Facts.All(covered.Contains);

    static GenerationDiagnostic Project(OmittedFactDiagnosticGroupFact group, ImmutableArray<GenerationFactRecord> records)
    {
        var ids = group.Definition.CoveredFacts.Concat(group.Definition.Diagnostic.Facts).Append(group.Id).ToHashSet();
        var inputs = records.Where(record => ids.Contains(record.Fact.Id)).Select(record => record.Fact).OrderBy(fact => fact.Id.Value, StringComparer.Ordinal).ToArray();
        var lineage = new GenerationFactLineage { Producer = _rule, Inputs = [.. inputs.Select(fact => fact.Id)], Evidence = [.. inputs.Select(fact => fact.Evidence)] };
        return group.Definition.Diagnostic with { Facts = lineage.Inputs, Lineage = lineage };
    }
}
