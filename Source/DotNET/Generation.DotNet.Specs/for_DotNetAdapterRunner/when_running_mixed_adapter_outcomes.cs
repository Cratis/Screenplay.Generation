// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAdapterRunner;

public class when_running_mixed_adapter_outcomes : given.a_runner_context
{
    ModernAdapter _applicable = null!;
    ModernAdapter _blocked = null!;
    ModernAdapter _notApplicable = null!;
    ModernAdapter _rejected = null!;
    ModernAdapter _throwing = null!;
    AdapterRunSnapshot _snapshot = null!;

    void Establish()
    {
        _applicable = new(Descriptor("applicable"));
        _blocked = new(Descriptor("blocked"))
        {
            ProbeResult = new AdapterProbeBlocked
            {
                Diagnostics =
                [
                    new GenerationDiagnostic
                    {
                        Code = "BLOCKED",
                        Severity = GenerationDiagnosticSeverity.Error,
                        Message = "The applicable adapter cannot execute safely"
                    }
                ]
            }
        };
        _notApplicable = new(Descriptor("not-applicable")) { ProbeResult = new AdapterProbeNotApplicable() };
        _rejected = new(Descriptor("rejected")) { Contribution = EmptyContribution("another-adapter") };
        _throwing = new(Descriptor("throwing"))
        {
            OnAnalyze = (_, _) => throw new InvalidOperationException("private /checkout/path should not escape")
        };
    }

    void Because() => _snapshot = DotNetAdapterRunner.Run(
        [
            DotNetAdapterRegistration.For(_throwing),
            DotNetAdapterRegistration.For(_rejected),
            DotNetAdapterRegistration.For(_notApplicable),
            DotNetAdapterRegistration.For(_blocked),
            DotNetAdapterRegistration.For(_applicable)
        ],
        new DotNetAnalysisContext([]),
        Options);

    [Fact] void should_probe_every_registration_exactly_once() => new[] { _applicable, _blocked, _notApplicable, _rejected, _throwing }.All(adapter => adapter.ProbeCount == 1).ShouldBeTrue();
    [Fact] void should_execute_only_applicable_registrations_exactly_once() => new[] { _applicable, _rejected, _throwing }.All(adapter => adapter.AnalyzeCount == 1).ShouldBeTrue();
    [Fact] void should_not_execute_blocked_or_not_applicable_registrations() => (_blocked.AnalyzeCount + _notApplicable.AnalyzeCount).ShouldEqual(0);
    [Fact] void should_admit_the_unrelated_valid_adapter() => _snapshot.Adapters.Single(record => record.Descriptor.Identity.Id == "applicable").Disposition.ShouldEqual(AdapterRunDisposition.Admitted);
    [Fact] void should_reject_the_invalid_contribution_atomically() => _snapshot.Adapters.Single(record => record.Descriptor.Identity.Id == "rejected").Disposition.ShouldEqual(AdapterRunDisposition.ContributionRejected);
    [Fact] void should_continue_after_an_analysis_exception() => _snapshot.Adapters.Single(record => record.Descriptor.Identity.Id == "throwing").Disposition.ShouldEqual(AdapterRunDisposition.ExecutionFailed);
    [Fact] void should_exclude_exception_messages_and_paths() => string.Join('|', _snapshot.Diagnostics.Select(diagnostic => diagnostic.Message)).ShouldNotContain("checkout");
}
