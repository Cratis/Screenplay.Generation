// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission.when_admitting_malformed_contributions;

public class with_inconsistent_ownership : given.a_contribution
{
    AdapterContributionAdmissionResult _result = null!;

    void Because()
    {
        var facts = EveryFact();
        var artifact = (ArtifactFact)facts[0];
        facts[0] = artifact with
        {
            Definition = artifact.Definition with
            {
                Key = artifact.Definition.Key with { Subject = ExternalSubject }
            }
        };
        var placement = (ArtifactPlacementFact)facts[1];
        facts[1] = placement with { Artifact = placement.Artifact with { Subject = ExternalSubject } };
        var declaration = (ArtifactDeclarationFact)facts[2];
        facts[2] = declaration with
        {
            Definition = declaration.Definition with
            {
                Artifact = declaration.Definition.Artifact with { Subject = ExternalSubject }
            }
        };
        var member = (ArtifactMemberDeclarationFact)facts[3];
        facts[3] = member with
        {
            Definition = member.Definition with
            {
                Member = member.Definition.Member with
                {
                    Artifact = member.Definition.Member.Artifact with { Subject = ExternalSubject }
                }
            }
        };
        var typeUse = (ArtifactMemberTypeUseFact)facts[4];
        facts[4] = typeUse with
        {
            Definition = typeUse.Definition with
            {
                Member = typeUse.Definition.Member with
                {
                    Artifact = typeUse.Definition.Member.Artifact with { Subject = ExternalSubject }
                }
            }
        };
        var binding = (TypeUseBindingFact)facts[5];
        facts[5] = binding with
        {
            Definition = binding.Definition with
            {
                Member = binding.Definition.Member with
                {
                    Artifact = binding.Definition.Member.Artifact with { Subject = ExternalSubject }
                }
            }
        };
        var role = (ArtifactMemberRoleFact)facts[6];
        facts[6] = role with
        {
            Definition = role.Definition with
            {
                Member = role.Definition.Member with
                {
                    Artifact = role.Definition.Member.Artifact with { Subject = ExternalSubject }
                }
            }
        };
        var relationship = (RelationshipFact)facts[7];
        facts[7] = relationship with
        {
            Definition = relationship.Definition with
            {
                Key = relationship.Definition.Key with { Source = ExternalSubject }
            }
        };
        var concept = (ConceptRepresentationFact)facts[8];
        facts[8] = concept with
        {
            Definition = concept.Definition with { Concept = ExternalSubject }
        };
        var scenario = (SpecificationScenarioFact)facts[11];
        facts[11] = scenario with
        {
            Definition = scenario.Definition with
            {
                Key = new SpecificationScenarioKey { Scenario = ExternalSubject }
            }
        };
        var step = (SpecificationStepFact)facts[12];
        facts[12] = step with
        {
            Definition = step.Definition with
            {
                Values =
                [
                    new SpecificationValueKey
                    {
                        Step = new SpecificationStepKey { Scenario = ScenarioKey(), Index = 1 },
                        Path = ["arguments", "name"]
                    }
                ]
            }
        };
        var value = (SpecificationValueFact)facts[13];
        facts[13] = value with
        {
            Definition = value.Definition with
            {
                Children = [ValueKey(["unrelated"])]
            }
        };
        _result = Admit(contribution: Contribution(facts));
    }

    [Fact] void should_reject_the_whole_contribution() => _result.Snapshot.ShouldBeNull();
    [Fact] void should_report_every_inconsistent_ownership_chain() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == AdapterContributionAdmissionDiagnosticCode.OwnershipMismatch).ShouldBeGreaterThan(11);
}
