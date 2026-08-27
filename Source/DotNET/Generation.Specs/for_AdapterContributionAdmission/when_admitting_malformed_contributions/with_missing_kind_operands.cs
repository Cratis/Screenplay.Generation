// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission.when_admitting_malformed_contributions;

public class with_missing_kind_operands : given.a_contribution
{
    AdapterContributionAdmissionResult _result = null!;

    void Because()
    {
        var facts = EveryFact();
        var representation = (ConceptRepresentationFact)facts[8];
        facts[8] = representation with
        {
            Definition = representation.Definition with { Primitive = null }
        };
        var step = (SpecificationStepFact)facts[12];
        facts[12] = step with
        {
            Definition = step.Definition with { Artifact = null }
        };
        var value = (SpecificationValueFact)facts[13];
        facts[13] = value with
        {
            Definition = value.Definition with
            {
                Kind = SpecificationValueKind.Boolean,
                Scalar = "True"
            }
        };
        _result = Admit(contribution: Contribution(facts));
    }

    [Fact] void should_reject_the_whole_contribution() => _result.Snapshot.ShouldBeNull();
    [Fact] void should_report_each_missing_or_invalid_kind_operand() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == AdapterContributionAdmissionDiagnosticCode.InvalidKindOperand).ShouldEqual(3);
}
