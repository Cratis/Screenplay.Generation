// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_projection_support_authorizes_another_method : given.diagnostic_candidates
{
    AdapterContributionAdmissionResult _result;
    void Because()
    {
        var support = Support();
        _result = Admit(Intent(), support with { Definition = support.Definition with { Alternatives = [new() { Method = Method }, new() { Method = ConceptSubject }] } });
    }
    [Fact] void should_reject_cross_method_alternatives() => _result.IsAdmitted.ShouldBeFalse();
}
