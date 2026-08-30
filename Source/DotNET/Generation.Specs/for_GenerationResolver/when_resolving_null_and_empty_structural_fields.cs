// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_resolving_null_and_empty_structural_fields : given.facts
{
    ResolvedApplicationGraph _result = null!;

    void Because()
    {
        var artifactWithNullFile = EventDefinition() with { File = null };
        var artifactWithEmptyFile = EventDefinition() with { File = string.Empty };
        var relationshipWithNullMembers = Relationship("relationship:null", FirstAdapter).Definition;
        var relationshipWithEmptySource = relationshipWithNullMembers with { SourceMember = string.Empty };
        var relationshipWithEmptyTarget = relationshipWithNullMembers with { TargetMember = string.Empty };
        _result = new GenerationResolver().Resolve(
        [
            Contribution(
                FirstAdapter,
                Fact("artifact:null", FirstAdapter, artifactWithNullFile),
                RelationshipFact("relationship:null", FirstAdapter, relationshipWithNullMembers)),
            Contribution(
                SecondAdapter,
                Fact("artifact:empty", SecondAdapter, artifactWithEmptyFile),
                RelationshipFact("relationship:empty-source", SecondAdapter, relationshipWithEmptySource),
                RelationshipFact("relationship:empty-target", SecondAdapter, relationshipWithEmptyTarget))
        ]);
    }

    [Fact] void should_retain_one_semantic_artifact_variant() => _result.Artifacts.Single().Variants.Count.ShouldEqual(1);
    [Fact] void should_retain_both_artifact_file_realizations() => _result.Artifacts.Single().Variants.Single().Files.Count.ShouldEqual(2);
    [Fact] void should_retain_the_null_artifact_file_realization() => _result.Artifacts.Single().Variants.Single().Files[0].ShouldBeNull();
    [Fact] void should_retain_the_empty_artifact_file_realization() => _result.Artifacts.Single().Variants.Single().Files[1].ShouldEqual(string.Empty);
    [Fact] void should_not_conflict_null_and_empty_artifact_files_semantically() => _result.Artifacts.Single().IsConflicted.ShouldBeFalse();
    [Fact] void should_retain_null_and_empty_relationship_members_as_distinct_variants() => _result.Relationships.Single().Definitions.Count.ShouldEqual(3);
    [Fact] void should_conflict_null_and_empty_relationship_members() => _result.Relationships.Single().IsConflicted.ShouldBeTrue();

    static RelationshipFact RelationshipFact(
        string id,
        AdapterIdentity adapter,
        RelationshipDefinition definition) => new()
    {
        Id = new FactId { Value = id },
        Subject = definition.Key.Source,
        Definition = definition,
        Evidence = new Evidence { Adapter = adapter, Strength = EvidenceStrength.Exact }
    };
}
