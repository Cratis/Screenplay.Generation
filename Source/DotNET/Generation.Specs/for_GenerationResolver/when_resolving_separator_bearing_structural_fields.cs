// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_resolving_separator_bearing_structural_fields : given.facts
{
    ResolvedApplicationGraph _result = null!;

    void Because()
    {
        var artifact = EventDefinition().Key;
        var firstRelationship = new RelationshipKey
        {
            Kind = RelationshipKind.Reads,
            Source = CommandSubject,
            Target = new SubjectId { Value = $"{EventSubject.Value}\u001fmember" }
        };
        var secondRelationship = new RelationshipKey
        {
            Kind = RelationshipKind.Reads,
            Source = CommandSubject,
            Target = EventSubject,
            Discriminator = "member"
        };
        _result = new GenerationResolver().Resolve(
        [
            Contribution(
                FirstAdapter,
                Placement("placement:first", FirstAdapter, artifact, "Accounts\u001fRegistration", []),
                Relationship("relationship:first", FirstAdapter, firstRelationship)),
            Contribution(
                SecondAdapter,
                Placement("placement:second", SecondAdapter, artifact, "Accounts", ["Registration"]),
                Relationship("relationship:second", SecondAdapter, secondRelationship))
        ]);
    }

    [Fact] void should_retain_separator_bearing_placements_as_distinct_variants() => _result.Placements.Single().Variants.Count.ShouldEqual(2);
    [Fact] void should_conflict_the_structurally_different_placements() => _result.Placements.Single().IsConflicted.ShouldBeTrue();
    [Fact] void should_resolve_separator_bearing_relationship_keys_independently() => _result.Relationships.Count.ShouldEqual(2);
    [Fact] void should_not_conflict_the_independent_relationships() => _result.Relationships.Any(_ => _.IsConflicted).ShouldBeFalse();

    static ArtifactPlacementFact Placement(
        string id,
        AdapterIdentity adapter,
        ArtifactKey artifact,
        string module,
        IReadOnlyList<string> features) => new()
    {
        Id = new FactId { Value = id },
        Subject = artifact.Subject,
        Artifact = artifact,
        Placement = new ArtifactPlacement
        {
            Module = module,
            Features = features,
            Slice = "Open",
            SliceKind = GenerationSliceKind.StateChange
        },
        Evidence = new Evidence { Adapter = adapter, Strength = EvidenceStrength.Exact }
    };

    static RelationshipFact Relationship(
        string id,
        AdapterIdentity adapter,
        RelationshipKey key) => new()
    {
        Id = new FactId { Value = id },
        Subject = key.Source,
        Definition = new RelationshipDefinition { Key = key },
        Evidence = new Evidence { Adapter = adapter, Strength = EvidenceStrength.Exact }
    };
}
