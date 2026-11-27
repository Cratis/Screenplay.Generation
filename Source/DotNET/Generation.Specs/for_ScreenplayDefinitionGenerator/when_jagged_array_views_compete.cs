// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_jagged_array_views_compete : given.jagged_array_members
{
    static readonly AdapterIdentity _other = new() { Id = "other-producer", Version = "1.0.0" };
    static readonly ScreenplayGenerationOptions _options = new() { Domain = "Ordering" };

    GenerationFact[] _facts = null!;
    ArtifactMemberTypeUseFact[] _competitors = null!;
    GenerationFact[] _originFacts = null!;
    ArtifactMemberTypeUseFact _originCompetitor = null!;
    GeneratedScreenplayDefinition[][] _results = null!;
    GeneratedScreenplayDefinition[] _originResults = null!;

    void Establish()
    {
        _facts = MemberFacts(true, ArrayOfArrays);
        var view = _facts.OfType<ArtifactMemberTypeUseFact>().Single();
        var rival = view with { Id = new FactId { Value = "other-producer:type-use" }, Evidence = view.Evidence with { Adapter = _other } };
        _competitors =
        [
            rival with { Definition = rival.Definition with { UseLegacyJaggedArrayReference = false } },
            rival with { Definition = rival.Definition with { Type = rival.Definition.Type with { Name = "Part" } } }
        ];
        TypeUseShapeKind[] tripleShape = [TypeUseShapeKind.Collection, TypeUseShapeKind.Collection, TypeUseShapeKind.Collection, TypeUseShapeKind.Named];
        _originFacts = MemberFacts(true, [.. ArrayOfArrays, TypeUseCollectionOriginKind.Array], tripleShape);
        _originCompetitor = rival with
        {
            Definition = rival.Definition with
            {
                Type = rival.Definition.Type with { Shape = tripleShape },
                CollectionOrigins = [.. ArrayOfArrays, TypeUseCollectionOriginKind.Enumerable]
            }
        };
    }

    void Because()
    {
        _results = [.. _competitors.Select(rival => GenerateInBothOrders(_facts, rival))];
        _originResults = GenerateInBothOrders(_originFacts, _originCompetitor);
    }

    [Fact] void should_report_conflicting_exact_observations() => _results.SelectMany(results => results)
        .All(result => HasCode(result, GenerationDiagnosticCodes.ConflictingArtifactMember)).ShouldBeTrue();

    [Fact] void should_report_disagreeing_origins_even_when_the_printable_views_match() => _originResults
        .All(result => HasCode(result, GenerationDiagnosticCodes.ConflictingArtifactMember)).ShouldBeTrue();

    [Fact] void should_not_resolve_a_winning_event() => _results.SelectMany(results => results)
        .Any(result => result.Graph.Artifacts.Any(artifact => artifact.Key.Kind == ArtifactKind.Event)).ShouldBeFalse();

    [Fact] void should_preserve_all_diagnostics_when_reversing_snapshot_producers() => _results
        .All(results => AdapterRunProjection(results[0].Diagnostics) == AdapterRunProjection(results[1].Diagnostics)).ShouldBeTrue();

    [Fact] void should_preserve_all_diagnostics_when_reversing_raw_contributions() => _results
        .All(results => AdapterRunProjection(results[2].Diagnostics) == AdapterRunProjection(results[3].Diagnostics)).ShouldBeTrue();

    [Fact] void should_report_both_conflict_codes_on_each_path() => _results.SelectMany(results => results)
        .All(result => Conflicts(result).Select(diagnostic => diagnostic.Code).Order(StringComparer.Ordinal).SequenceEqual([GenerationDiagnosticCodes.ConflictingMemberTypeUse, GenerationDiagnosticCodes.ConflictingArtifactMember])).ShouldBeTrue();

    [Fact] void should_preserve_the_conflict_payloads_across_ingestion_paths() => _results
        .All(results => AdapterRunProjection(Conflicts(results[0])) == AdapterRunProjection(Conflicts(results[2]))).ShouldBeTrue();

    [Fact] void should_report_only_snapshot_dispositions_in_addition_to_conflicts() => _results
        .All(results => results[2].Diagnostics.Count == 2 && results[0].Diagnostics.Count == 5 &&
            results[0].Diagnostics.Where(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.OmittedGenerationFact)
                .Select(diagnostic => diagnostic.Facts.Single().Value).Order(StringComparer.Ordinal)
                .SequenceEqual(["critter-stack:legacy-artifact", "critter-stack:member", "critter-stack:placement"])).ShouldBeTrue();

    static GenerationDiagnostic[] Conflicts(GeneratedScreenplayDefinition result) =>
    [
        .. result.Diagnostics.Where(diagnostic =>
            string.Equals(diagnostic.Code, GenerationDiagnosticCodes.ConflictingArtifactMember, StringComparison.Ordinal) ||
            string.Equals(diagnostic.Code, GenerationDiagnosticCodes.ConflictingMemberTypeUse, StringComparison.Ordinal))
    ];

    GeneratedScreenplayDefinition[] GenerateInBothOrders(GenerationFact[] facts, ArtifactMemberTypeUseFact rival)
    {
        var first = Completed(Adapter, facts);
        var second = Completed(_other, [rival]);
        var original = Contribution(facts);
        var other = new AdapterContribution { Adapter = _other, Facts = [rival] };
        return
        [
            Generator.Generate(Snapshot(first, second), _options),
            Generator.Generate(Snapshot(second, first), _options),
            Generator.Generate([original, other], _options),
            Generator.Generate([other, original], _options)
        ];
    }
}
