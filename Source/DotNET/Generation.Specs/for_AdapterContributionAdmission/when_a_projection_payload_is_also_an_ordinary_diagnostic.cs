// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_a_projection_payload_is_also_an_ordinary_diagnostic : given.diagnostic_candidates
{
    AdapterContributionAdmissionResult _contribution;
    AdapterContributionAdmissionResult _alternative;
    void Because()
    {
        _contribution = Admit(Intent(), diagnostics: [Payload()]);
        var support = Support();
        _alternative = Admit(Intent(), support with { Definition = support.Definition with { Alternatives = [new() { Method = Method, Diagnostics = [Payload()] }] } });
    }
    [Fact] void should_prevent_early_contribution_leakage() => _contribution.IsAdmitted.ShouldBeFalse();
    [Fact] void should_prevent_early_alternative_leakage() => _alternative.IsAdmitted.ShouldBeFalse();
}
