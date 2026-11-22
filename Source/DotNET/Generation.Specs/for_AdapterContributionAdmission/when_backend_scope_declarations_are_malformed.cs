// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_backend_scope_declarations_are_malformed : for_GenerationResolver.given.backend_scopes
{
    AdapterContributionAdmissionResult _result;

    void Because() => _result = Admit(
        Scope(First with { EventModelName = " " }, "empty"),
        Scope(First, "owner") with { Subject = EventSubject },
        Scope(First, "source") with { Evidence = Evidence() with { Source = null } },
        Scope(First, "missing") with { Definition = null! });

    [Fact] void should_reject_without_throwing() => _result.IsAdmitted.ShouldBeFalse();
    [Fact] void should_locate_the_invalid_qualifier() => _result.Diagnostics.Any(diagnostic => diagnostic.Path.EndsWith(".EventModelName", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_report_mismatched_ownership() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == AdapterContributionAdmissionDiagnosticCode.OwnershipMismatch).ShouldBeTrue();
    [Fact] void should_require_real_source_evidence() => _result.Diagnostics.Any(diagnostic => diagnostic.Path.EndsWith(".Evidence.Source", StringComparison.Ordinal)).ShouldBeTrue();
}
