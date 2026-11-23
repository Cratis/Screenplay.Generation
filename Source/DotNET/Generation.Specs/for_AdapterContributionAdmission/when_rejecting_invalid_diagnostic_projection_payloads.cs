// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_rejecting_invalid_diagnostic_projection_payloads : given.diagnostic_candidates
{
    AdapterContributionAdmissionResult[] _results;
    void Because()
    {
        var intent = Intent();
        GenerationDiagnostic[] payloads =
        [
            Payload() with { Facts = [] }, Payload() with { Facts = [intent.Id] },
            Payload() with { Facts = [new() { Value = "foreign:observation" }] },
            Payload() with { Facts = [new() { Value = "observations:missing" }] },
            Payload() with { Source = null }, Payload() with { Source = Invocation with { StartLine = 2 } },
            Payload() with { Subject = ConceptSubject }, Payload() with { Code = "" },
            Payload() with { Severity = GenerationDiagnosticSeverity.Unknown },
            Payload() with { Lineage = new() { Producer = new() { Id = "forged", Version = "1.0.0" } } }
        ];
        _results = [.. payloads.Select(payload => Admit(intent with { Definition = intent.Definition with { Diagnostic = payload } }))];
    }
    [Fact] void should_reject_every_malformed_payload_atomically() => _results.All(result => !result.IsAdmitted && result.Snapshot is null).ShouldBeTrue();
}
