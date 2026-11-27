// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_jagged_array_views_are_checked_before_concept_name_yield : given.yielding_concepts
{
    static readonly TypeUseCollectionOriginKind[] _arrays = [TypeUseCollectionOriginKind.Array, TypeUseCollectionOriginKind.Array];
    static readonly TypeUseShapeKind[] _shape = [TypeUseShapeKind.Collection, TypeUseShapeKind.Collection, TypeUseShapeKind.Named];
    static readonly ScreenplayGenerationOptions _options = new() { Domain = "Ordering" };

    GenerationFact[] _baselineFacts = null!;
    GenerationFact[][] _rejectedFacts = null!;
    GenerationFact[] _validFacts = null!;
    GenerationFact[] _ordinaryFacts = null!;
    ArtifactMemberTypeUseFact _observation = null!;
    TypeUseCompatibilityProjectionFact _projection = null!;
    GeneratedScreenplayDefinition _baseline = null!;
    GeneratedScreenplayDefinition[] _rejected = null!;
    GeneratedScreenplayDefinition[] _valid = null!;
    GeneratedScreenplayDefinition[] _ordinary = null!;

    void Establish()
    {
        _baselineFacts = [.. Command(), .. Candidate("enum", true)];
        var member = UnboundMember("Permissions");
        var declaration = member.OfType<ArtifactMemberDeclarationFact>().Single();
        _observation = Marked(member.OfType<ArtifactMemberTypeUseFact>().Single());
        _projection = Projection(_observation);
        var list = _observation with
        {
            Definition = _observation.Definition with { CollectionOrigins = [TypeUseCollectionOriginKind.Enumerable, TypeUseCollectionOriginKind.Enumerable] }
        };
        _rejectedFacts = [[.. _baselineFacts, list], [.. _baselineFacts, _observation, _projection]];
        _validFacts = [.. _baselineFacts, declaration, _observation];
        _ordinaryFacts = [.. _baselineFacts, .. member];
    }

    void Because()
    {
        _baseline = Generator.Generate(Snapshot(Completed(Adapter, _baselineFacts)), _options);
        _rejected = [.. _rejectedFacts.SelectMany(GenerateInBothOrders)];
        _valid = GenerateInBothOrders(_validFacts);
        _ordinary = GenerateInBothOrders(_ordinaryFacts);
    }

    [Fact] void should_keep_the_concept_selected_in_the_absence_of_invalid_reservations() =>
        _rejected.Select(Concepts).Distinct().ShouldEqual([Concepts(_baseline)]);

    [Fact] void should_keep_the_original_concept() => _rejected.All(HasPermissions).ShouldBeTrue();
    [Fact] void should_not_emit_a_yield_warning_for_either_invalid_view() => _rejected.SelectMany(result => result.Diagnostics).Any(IsWarning).ShouldBeFalse();
    [Fact] void should_keep_identical_bindings_and_fallbacks() =>
        _rejected.Where(result => result.AdapterRun is not null).Select(DerivedMembers).Distinct().ShouldEqual([DerivedMembers(_baseline)]);

    [Fact] void should_not_consume_invalid_inputs_in_the_yield_rule() => _rejected.Where(result => result.AdapterRun is not null)
        .SelectMany(result => result.AdapterRun!.Derivation.Rules)
        .Where(rule => rule.Rule.Id == "cratis.screenplay.concept-name-yield")
        .SelectMany(rule => rule.Inputs).Any(id => id == _observation.Id || id == _projection.Id).ShouldBeFalse();

    [Fact] void should_report_exactly_one_located_error_for_each_invalid_view() => _rejected.All(result =>
        result.Diagnostics.Count(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.InvalidTypeUseCompatibilityProjection &&
            diagnostic.Severity == GenerationDiagnosticSeverity.Error && diagnostic.Source == _observation.Evidence.Source &&
            diagnostic.Subject == _observation.Subject && diagnostic.Facts.Contains(_observation.Id)) == 1).ShouldBeTrue();

    [Fact] void should_not_print_either_invalid_member() => _rejected.All(result => !result.Source.Contains("unbound", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_reserve_the_name_when_the_view_is_valid() => _valid.All(HasPermissions).ShouldBeTrue();
    [Fact] void should_yield_when_an_ordinary_type_use_reserves_the_name() => _ordinary.All(result => !HasPermissions(result) && result.Diagnostics.Any(IsWarning)).ShouldBeTrue();

    GeneratedScreenplayDefinition[] GenerateInBothOrders(GenerationFact[] facts)
    {
        var reversed = facts.Reverse().ToArray();
        return
        [
            Generator.Generate(Snapshot(Completed(Adapter, facts)), _options),
            Generator.Generate(Snapshot(Completed(Adapter, reversed)), _options),
            Generator.Generate([Contribution(facts)], _options),
            Generator.Generate([Contribution(reversed)], _options)
        ];
    }

    static ArtifactMemberTypeUseFact Marked(ArtifactMemberTypeUseFact observation) => observation with
    {
        Definition = observation.Definition with
        {
            Type = observation.Definition.Type with { Shape = _shape },
            CollectionOrigins = _arrays,
            UseLegacyJaggedArrayReference = true
        }
    };

    static TypeUseCompatibilityProjectionFact Projection(ArtifactMemberTypeUseFact observation) => new()
    {
        Id = new FactId { Value = "critter-stack:contradictory-view" },
        Subject = observation.Subject,
        Evidence = observation.Evidence,
        Definition = new TypeUseCompatibilityProjectionDefinition
        {
            TypeUse = observation.Id,
            Profile = TypeUseCompatibilityProfile.OneLayerCollection,
            Version = 1,
            SourceNodeIndex = 1,
            Type = new TypeUseDefinition { Name = "Permissions", Shape = [TypeUseShapeKind.Collection, TypeUseShapeKind.Named] }
        }
    };

    static string Concepts(GeneratedScreenplayDefinition result) => AdapterRunProjection(result.Graph.Artifacts.Where(artifact => artifact.Key.Kind == ArtifactKind.Concept));
    static bool HasPermissions(GeneratedScreenplayDefinition result) => result.Graph.Artifacts.Any(artifact => artifact.Key.Kind == ArtifactKind.Concept && artifact.Key.Subject == Target);
    static string DerivedMembers(GeneratedScreenplayDefinition result) => AdapterRunProjection(result.AdapterRun!.Derivation.Facts
        .Where(record => record.Fact is TypeUseBindingFact or TypeUseTargetOmittedFact).Select(record => new { record.Fact, record.Lineage }));
}
