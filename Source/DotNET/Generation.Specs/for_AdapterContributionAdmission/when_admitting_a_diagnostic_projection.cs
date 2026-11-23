// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_admitting_a_diagnostic_projection : given.diagnostic_candidates
{
    AdapterContributionAdmissionResult _result;
    void Because() => _result = Admit(Intent());
    [Fact] void should_accept_without_foreign_entry_point_or_classification_facts() => _result.IsAdmitted.ShouldBeTrue();
    [Fact] void should_freeze_the_nested_payload() => _result.Snapshot.Facts.OfType<MethodDiagnosticProjectionFact>().Single().Definition.Diagnostic.Facts.SequenceEqual(Payload().Facts).ShouldBeTrue();
    [Fact] void should_not_promote_the_payload_to_ordinary_diagnostics() => _result.Snapshot.Diagnostics.ShouldBeEmpty();
}
