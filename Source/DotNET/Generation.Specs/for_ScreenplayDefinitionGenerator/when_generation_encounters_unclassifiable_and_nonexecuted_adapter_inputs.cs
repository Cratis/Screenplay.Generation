// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_generation_encounters_unclassifiable_and_nonexecuted_adapter_inputs : given.a_generator
{
    GeneratedScreenplayDefinition _unclassifiable = null!;
    GeneratedScreenplayDefinition _nonExecuted = null!;

    void Because()
    {
        var fact = new UnclassifiableFact
        {
            Id = new FactId { Value = "custom:fact" },
            Subject = new SubjectId { Value = "custom://facts/unclassifiable" },
            Evidence = new Evidence { Adapter = Adapter, Strength = EvidenceStrength.Exact }
        };
        _unclassifiable = Generator.Generate(
            Snapshot(Completed(Adapter, [fact])),
            new ScreenplayGenerationOptions { Domain = "Unknowns" });

        var blockedDiagnostic = Diagnostic("RUN-BLOCKED", "Adapter was blocked");
        var rejectedDiagnostic = Diagnostic("RUN-REJECTED", "Adapter contribution was rejected");
        var blocked = Record(
            "adapter:blocked",
            new AdapterProbeBlocked { Diagnostics = [blockedDiagnostic] },
            new AdapterExecutionNotRun { Diagnostics = [blockedDiagnostic] },
            AdapterRunDisposition.Blocked);
        var rejected = Record(
            "adapter:rejected",
            new AdapterProbeApplicable(),
            new AdapterExecutionRejected { Diagnostics = [rejectedDiagnostic] },
            AdapterRunDisposition.ContributionRejected,
            executed: true);
        _nonExecuted = Generator.Generate(
            new AdapterRunSnapshot
            {
                Adapters = [rejected, blocked],
                Facts = [new GenerationFactRecord { Fact = fact }],
                Diagnostics = [rejectedDiagnostic, blockedDiagnostic]
            },
            new ScreenplayGenerationOptions { Domain = "Empty" });
    }

    [Fact] void should_fail_closed_for_the_unclassifiable_fact() => _unclassifiable.AdapterRun!.Facts.Single().Disposition.ShouldEqual(GenerationFactDisposition.Unknown);
    [Fact] void should_report_a_stable_error_for_the_unclassifiable_fact() => _unclassifiable.AdapterRun!.Facts.Single().Diagnostics.Single().Code.ShouldEqual(GenerationDiagnosticCodes.UnclassifiedGenerationFact);
    [Fact] void should_not_report_success_for_the_unclassifiable_fact() => _unclassifiable.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_resolve_facts_from_the_top_level_input_fact_records() => _nonExecuted.AdapterRun!.Facts.ShouldBeEmpty();
    [Fact] void should_preserve_blocked_and_rejected_adapter_records() => _nonExecuted.AdapterRun!.Adapters.Select(record => record.Disposition).ShouldContainOnly(AdapterRunDisposition.Blocked, AdapterRunDisposition.ContributionRejected);
    [Fact] void should_preserve_the_blocked_diagnostic() => _nonExecuted.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("RUN-BLOCKED");
    [Fact] void should_preserve_the_rejected_diagnostic() => _nonExecuted.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("RUN-REJECTED");
    [Fact] void should_keep_runner_diagnostics_canonical_without_duplicates() => _nonExecuted.AdapterRun!.Diagnostics.Length.ShouldEqual(2);

    static AdapterRunRecord Record(
        string id,
        AdapterProbeResult probe,
        AdapterExecutionResult execution,
        AdapterRunDisposition disposition,
        bool executed = false)
    {
        var descriptor = new AdapterDescriptor
        {
            Identity = new AdapterIdentity { Id = id, Version = "1.0.0" },
            SourceLanguage = AdapterSourceLanguage.SourceIndependent,
            Category = AdapterCategory.ApplicationFramework
        };
        return new AdapterRunRecord
        {
            Considered = true,
            Probed = probe is not AdapterProbeNotRun,
            Executed = executed,
            Descriptor = descriptor,
            Probe = probe,
            Execution = execution,
            Disposition = disposition
        };
    }

    static GenerationDiagnostic Diagnostic(string code, string message) => new()
    {
        Code = code,
        Severity = GenerationDiagnosticSeverity.Error,
        Message = message
    };

    sealed record UnclassifiableFact : GenerationFact;
}
