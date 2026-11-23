// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_admitting_duplicate_type_use_target_omissions : given.a_contribution
{
    AdapterContributionAdmissionResult _result = null!;
    AdapterContributionAdmissionResult _permuted = null!;

    void Because()
    {
        var typeUse = EveryFact().OfType<ArtifactMemberTypeUseFact>().Single();
        var first = new TypeUseTargetOmittedFact
        {
            Id = Id("omission-a"), Subject = ArtifactSubject, Evidence = Evidence(),
            Definition = new TypeUseTargetOmittedDefinition
            {
                Member = typeUse.Definition.Member,
                Diagnostic = new GenerationDiagnostic
                {
                    Code = "atomic.TargetOmitted", Severity = GenerationDiagnosticSeverity.Warning,
                    Message = "No target is contributed", Source = Source(), Facts = [typeUse.Id], Subject = ArtifactSubject
                }
            }
        };
        var second = first with { Id = Id("omission-b") };
        _result = Admit(contribution: Contribution([typeUse, first, second]));
        _permuted = Admit(contribution: Contribution([second, typeUse, first]));
    }

    [Fact] void should_reject_the_contribution_atomically() => _result.IsAdmitted.ShouldBeFalse();
    [Fact] void should_not_return_a_snapshot_that_could_report_the_warning_twice() => _result.Snapshot.ShouldBeNull();
    [Fact] void should_report_duplicate_member_ownership() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == AdapterContributionAdmissionDiagnosticCode.OwnershipMismatch && diagnostic.Message.Contains("occurs more than once", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_mistake_distinct_fact_ids_for_duplicate_identities() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == AdapterContributionAdmissionDiagnosticCode.DuplicateFactId).ShouldBeFalse();
    [Fact] void should_reject_the_permuted_contribution() => _permuted.IsAdmitted.ShouldBeFalse();
    [Fact] void should_report_the_same_diagnostics_after_permutation() => _permuted.Diagnostics.ShouldContainOnly(_result.Diagnostics);
}
