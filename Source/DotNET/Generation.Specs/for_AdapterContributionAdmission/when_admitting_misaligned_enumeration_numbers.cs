// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_admitting_misaligned_enumeration_numbers : given.a_numbered_enumeration
{
    AdapterContributionAdmissionResult _result = null!;

    void Because() => _result = Admit(contribution: Contribution([Representation([7])]));

    [Fact] void should_reject_the_contribution() => _result.Snapshot.ShouldBeNull();
    [Fact] void should_identify_the_numeric_operand() => _result.Diagnostics.Single().Path.EndsWith(".Definition.EnumerationNumbers", StringComparison.Ordinal).ShouldBeTrue();
}
