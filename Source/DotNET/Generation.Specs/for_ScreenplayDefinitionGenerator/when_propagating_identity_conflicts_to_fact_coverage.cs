// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_propagating_identity_conflicts_to_fact_coverage : given.a_coverage_fixture
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var opened = Artifact(ArtifactKind.Event, "AccountOpened", "Open", GenerationSliceKind.StateChange, 1);
        var renamed = (ArtifactFact)opened[0];
        var placement = (ArtifactPlacementFact)opened[1];
        var first = (RelationshipFact)Relation(RelationshipKind.Produces, "OpenAccount", "AccountOpened", sourceMember: "accountId");
        var second = first with
        {
            Id = new FactId { Value = "relationship:second" },
            Definition = first.Definition with { SourceMember = "generatedId" }
        };

        _result = Run([
            .. opened,
            renamed with
            {
                Id = new FactId { Value = "artifact:renamed" },
                Definition = renamed.Definition with { Name = "AccountWasOpened" }
            },
            placement with
            {
                Id = new FactId { Value = "placement:second" },
                Placement = placement.Placement with { Slice = "Close" }
            },
            first,
            second,
            Representation("representation:first", GenerationPrimitiveKind.Uuid),
            Representation("representation:second", GenerationPrimitiveKind.Text),
            ConceptAttribute("attribute:first", "First reason"),
            ConceptAttribute("attribute:second", "Second reason"),
            ConceptRule("rule:first", "BeValidAccountNumber"),
            ConceptRule("rule:second", "BeFormattedAccountNumber")]);
    }

    [Fact]
    void should_associate_every_conflicted_family_with_its_diagnostic() => Records(_result).ShouldEqual(
        """
        artifact:AccountOpened|Conflicted|GEN0001|Conflict|Artifact 'dotnet://Banking/AccountOpened' has 2 incompatible Event definitions
        artifact:renamed|Conflicted|GEN0001|Conflict|Artifact 'dotnet://Banking/AccountOpened' has 2 incompatible Event definitions
        attribute:first|Conflicted|GEN0018|Conflict|Concept 'dotnet://Banking/Concepts.AccountId' has 2 incompatible 'pii' attribute definitions
        attribute:second|Conflicted|GEN0018|Conflict|Concept 'dotnet://Banking/Concepts.AccountId' has 2 incompatible 'pii' attribute definitions
        placement:AccountOpened|Conflicted|GEN0008|Conflict|Artifact 'dotnet://Banking/AccountOpened' has 2 equally strong incompatible Event placements
        placement:second|Conflicted|GEN0008|Conflict|Artifact 'dotnet://Banking/AccountOpened' has 2 equally strong incompatible Event placements
        relationship:Produces:OpenAccount:AccountOpened|Conflicted|GEN0006|Conflict|Relationship 'Produces' from 'dotnet://Banking/OpenAccount' to 'dotnet://Banking/AccountOpened' has 2 incompatible definitions
        relationship:second|Conflicted|GEN0006|Conflict|Relationship 'Produces' from 'dotnet://Banking/OpenAccount' to 'dotnet://Banking/AccountOpened' has 2 incompatible definitions
        representation:first|Conflicted|GEN0011|Conflict|Concept 'dotnet://Banking/Concepts.AccountId' has 2 incompatible representations
        representation:second|Conflicted|GEN0011|Conflict|Concept 'dotnet://Banking/Concepts.AccountId' has 2 incompatible representations
        rule:first|Conflicted|GEN0020|Conflict|Concept 'dotnet://Banking/Concepts.AccountId' has 2 incompatible validation definitions for rule identity 'account-number-format'
        rule:second|Conflicted|GEN0020|Conflict|Concept 'dotnet://Banking/Concepts.AccountId' has 2 incompatible validation definitions for rule identity 'account-number-format'
        """);
}
