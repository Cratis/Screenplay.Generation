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
        var relationship = (RelationshipFact)facts[2];
        facts[2] = relationship with
        {
            Definition = relationship.Definition with
            {
                Key = relationship.Definition.Key with { Source = ExternalSubject }
            }
        };
        var concept = (ConceptRepresentationFact)facts[3];
        facts[3] = concept with
        {
            Definition = concept.Definition with { Concept = ExternalSubject }
        };
        var scenario = (SpecificationScenarioFact)facts[6];
        facts[6] = scenario with
        {
            Definition = scenario.Definition with
            {
                Key = new SpecificationScenarioKey { Scenario = ExternalSubject }
            }
        };
        var step = (SpecificationStepFact)facts[7];
        facts[7] = step with
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
        var value = (SpecificationValueFact)facts[8];
        facts[8] = value with
        {
            Definition = value.Definition with
            {
                Children = [ValueKey(["unrelated"])]
            }
        };
        _result = Admit(contribution: Contribution(facts));
    }

    [Fact] void should_reject_the_whole_contribution() => _result.Snapshot.ShouldBeNull();
    [Fact] void should_report_every_inconsistent_ownership_chain() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == AdapterContributionAdmissionDiagnosticCode.OwnershipMismatch).ShouldBeGreaterThan(6);
}
