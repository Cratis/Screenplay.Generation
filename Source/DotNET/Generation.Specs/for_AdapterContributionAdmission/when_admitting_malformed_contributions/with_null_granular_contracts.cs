// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission.when_admitting_malformed_contributions;

public class with_null_granular_contracts : given.a_contribution
{
    AdapterContributionAdmissionResult _nullDefinitions = null!;
    AdapterContributionAdmissionResult _nullShape = null!;

    void Because()
    {
        var facts = EveryFact();
        facts[2] = ((ArtifactDeclarationFact)facts[2]) with { Definition = null! };
        facts[3] = ((ArtifactMemberDeclarationFact)facts[3]) with { Definition = null! };
        facts[4] = ((ArtifactMemberTypeUseFact)facts[4]) with { Definition = null! };
        facts[5] = ((TypeUseBindingFact)facts[5]) with { Definition = null! };
        facts[6] = ((ArtifactMemberRoleFact)facts[6]) with { Definition = null! };
        _nullDefinitions = Admit(contribution: Contribution(facts));

        var nullShapeFacts = EveryFact();
        var typeUse = (ArtifactMemberTypeUseFact)nullShapeFacts[4];
        nullShapeFacts[4] = typeUse with
        {
            Definition = typeUse.Definition with
            {
                Type = typeUse.Definition.Type with { Shape = null! }
            }
        };
        _nullShape = Admit(contribution: Contribution(nullShapeFacts));
    }

    [Fact] void should_reject_all_null_granular_definitions_atomically() => _nullDefinitions.Snapshot.ShouldBeNull();
    [Fact] void should_report_each_missing_granular_definition() => _nullDefinitions.Diagnostics.Count(diagnostic => diagnostic.Code == AdapterContributionAdmissionDiagnosticCode.MissingRequiredValue).ShouldBeGreaterThan(4);
    [Fact] void should_reject_a_null_type_use_shape_atomically() => _nullShape.Snapshot.ShouldBeNull();
    [Fact] void should_report_the_null_type_use_shape_collection() => _nullShape.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(AdapterContributionAdmissionDiagnosticCode.NullRequiredCollection);
}
