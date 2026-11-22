// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_declaration_member_name_is_not_normalized : given.declaration_input_members
{
    AdapterContributionAdmissionResult _result;

    void Because() => _result = AdmitEligibility(Surface() with { Definition = Surface().Definition with { Member = Surface().Definition.Member with { Name = "not a member" } } });

    [Fact] void should_not_reinterpret_a_display_name_as_a_member_identity() => _result.IsAdmitted.ShouldBeFalse();
}
