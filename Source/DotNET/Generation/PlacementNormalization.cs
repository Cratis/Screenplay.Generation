// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Generation;

static class PlacementNormalization
{
    static readonly GenerationDerivationRuleIdentity _rule = new() { Id = "cratis.screenplay.mixed-slice-placement", Version = "1.0.0" };

    public static (ResolvedApplicationGraph Graph, GenerationDerivationSnapshot Derivation) Complete(ResolvedApplicationGraph graph, GenerationDerivationSnapshot derivation, IReadOnlyList<GenerationFact> baseFacts)
    {
        if (graph.PlacementNormalizations.Count == 0) return (graph, derivation);
        var originals = baseFacts.ToDictionary(fact => fact.Id);
        var derived = derivation.Facts.Where(record => record.Lineage is not null).ToDictionary(record => record.Fact.Id);
        var records = graph.PlacementNormalizations.Select(record => Flatten(record, originals, derived)).ToArray();
        var effective = records.Select(EffectiveRecord).ToArray();
        var warnings = graph.Diagnostics.Select(diagnostic => (diagnostic.Code == GenerationDiagnosticCodes.MixedSliceKindsSplit || diagnostic.Code == GenerationDiagnosticCodes.ConflictingMixedSliceSplit) ? diagnostic with { Facts = [.. Expand(diagnostic.Facts, derived)] } : diagnostic).ToArray();
        return (
            graph with { PlacementNormalizations = records, Diagnostics = warnings },
            derivation with
            {
                Facts = [.. derivation.Facts, .. effective],
                Rules = [.. derivation.Rules, new GenerationDerivationRuleRecord { Rule = _rule, Inputs = [.. records.SelectMany(record => record.Inputs).Distinct().OrderBy(id => id.Value, StringComparer.Ordinal)], Outputs = [.. effective.Select(record => record.Fact.Id)], Diagnostics = [.. warnings.Where(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.MixedSliceKindsSplit)] }]
            });
    }

    public static bool Superseded(ArtifactPlacementFact placement, ResolvedApplicationGraph graph) => graph.PlacementNormalizations.Any(record => record.Artifact == placement.Artifact && Structural.Placement(record.Original) == Structural.Placement(placement.Placement));

    public static bool IsRepairWarning(GenerationDiagnostic diagnostic, GenerationFact fact, ResolvedApplicationGraph graph) =>
        diagnostic.Code == GenerationDiagnosticCodes.MixedSliceKindsSplit && diagnostic.Severity == GenerationDiagnosticSeverity.Warning && diagnostic.Outcome == GenerationDiagnosticOutcome.Conflict && graph.PlacementNormalizations.Any(record => record.Inputs.Contains(fact.Id) && record.Inputs.Any(diagnostic.Facts.Contains));

    static PlacementNormalizationRecord Flatten(PlacementNormalizationRecord record, Dictionary<FactId, GenerationFact> originals, Dictionary<FactId, GenerationFactRecord> derived)
    {
        var inputs = Expand(record.Inputs, derived);
        return record with
        {
            Inputs = inputs,
            Evidence = [.. record.Evidence.Concat(record.Inputs.Where(derived.ContainsKey).SelectMany(id => derived[id].Lineage!.Evidence)).Concat(inputs.Where(originals.ContainsKey).Select(id => originals[id].Evidence)).DistinctBy(Structural.Evidence).OrderBy(Structural.Evidence, StringComparer.Ordinal)]
        };
    }

    static FactId[] Expand(IEnumerable<FactId> ids, Dictionary<FactId, GenerationFactRecord> derived) => [.. ids.SelectMany(id => derived.TryGetValue(id, out var record) ? record.Lineage!.Inputs : [id]).Distinct().OrderBy(id => id.Value, StringComparer.Ordinal)];

    static GenerationFactRecord EffectiveRecord(PlacementNormalizationRecord record) => new()
    {
        Fact = new ArtifactPlacementFact
        {
            Id = new() { Value = $"generation:normalize:mixed-slice:1:{Convert.ToHexString(Encoding.UTF8.GetBytes(Structural.SemanticKey(Structural.ArtifactKey(record.Artifact), Structural.Placement(record.Original), Structural.Placement(record.Final))))}" },
            Subject = record.Artifact.Subject,
            Artifact = record.Artifact,
            Placement = record.Final,
            Evidence = record.Evidence.OrderBy(evidence => evidence.Strength).ThenBy(Structural.Evidence, StringComparer.Ordinal).First(),
            MixedSliceFallback = MixedSliceFallbackStructure.Copy(record.Fallback)
        },
        Lineage = new() { Producer = _rule, Inputs = [.. record.Inputs], Evidence = [.. record.Evidence] }
    };
}
