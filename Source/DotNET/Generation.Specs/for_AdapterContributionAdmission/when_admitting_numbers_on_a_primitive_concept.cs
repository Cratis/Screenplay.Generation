// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_admitting_numbers_on_a_primitive_concept : given.a_numbered_enumeration
{
    AdapterContributionAdmissionResult _result = null!;

    void Because() => _result = Admit(contribution: Contribution([Representation([], ConceptRepresentationKind.Primitive)]));

    [Fact] void should_reject_even_an_empty_numeric_operand() => _result.Snapshot.ShouldBeNull();
    [Fact] void should_report_an_invalid_operand() => _result.Diagnostics.Single().Code.ShouldEqual(AdapterContributionAdmissionDiagnosticCode.InvalidKindOperand);
}
