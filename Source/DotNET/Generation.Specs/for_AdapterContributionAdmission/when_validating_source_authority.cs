// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_validating_source_authority : given.a_contribution
{
    AdapterContributionAdmissionResult _accepted = null!;
    AdapterContributionAdmissionResult _rejected = null!;
    AdapterContributionAdmissionResult _rejectedReversed = null!;
    AdapterContributionAdmissionResult _withoutValidator = null!;
    SourceAuthorityValidator _acceptingValidator = null!;
    SourceAuthorityValidator _rejectingValidator = null!;

    void Because()
    {
        var contribution = Contribution(
            diagnostics:
            [
                new GenerationDiagnostic
                {
                    Code = "ATOMIC0001",
                    Severity = GenerationDiagnosticSeverity.Warning,
                    Outcome = GenerationDiagnosticOutcome.Unsupported,
                    Message = "A source behavior cannot be represented",
                    Source = Source(20),
                    Subject = ArtifactSubject
                }
            ]);
        _acceptingValidator = new(true);
        _rejectingValidator = new(false);
        _accepted = Admit(contribution: contribution, validator: _acceptingValidator);
        _rejected = Admit(contribution: contribution, validator: _rejectingValidator);
        _rejectedReversed = Admit(
            contribution: contribution with { Facts = [.. contribution.Facts.AsEnumerable().Reverse()] },
            validator: new SourceAuthorityValidator(false));
        _withoutValidator = AdapterContributionAdmission.Admit(Descriptor(), contribution);
    }

    [Fact] void should_admit_authoritative_fact_and_diagnostic_ranges() => _accepted.IsAdmitted.ShouldBeTrue();
    [Fact] void should_validate_every_fact_and_contribution_diagnostic_range() => _acceptingValidator.Validated.Count.ShouldEqual(15);
    [Fact] void should_reject_nonauthoritative_fact_and_diagnostic_ranges_atomically() => _rejected.Snapshot.ShouldBeNull();
    [Fact] void should_report_every_nonauthoritative_range() => _rejected.Diagnostics.Count(diagnostic => diagnostic.Code == AdapterContributionAdmissionDiagnosticCode.SourceNotAuthoritative).ShouldEqual(15);
    [Fact] void should_reject_source_evidence_when_no_authority_validator_is_supplied() => _withoutValidator.Snapshot.ShouldBeNull();
    [Fact] void should_require_authority_for_every_unvalidated_range() => _withoutValidator.Diagnostics.Count(diagnostic => diagnostic.Code == AdapterContributionAdmissionDiagnosticCode.SourceAuthorityRequired).ShouldEqual(15);
    [Fact] void should_order_rejected_source_diagnostics_independently_of_fact_order() => Projection(_rejectedReversed).ShouldEqual(Projection(_rejected));

    static string[] Projection(AdapterContributionAdmissionResult result) =>
    [
        .. result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}|{diagnostic.Path}|{diagnostic.Fact?.Value}|{diagnostic.Source?.FileIdentity?.Project}|{diagnostic.Source?.FileIdentity?.Path}|{diagnostic.Source?.Path}|{diagnostic.Source?.StartLine}:{diagnostic.Source?.StartColumn}-{diagnostic.Source?.EndLine}:{diagnostic.Source?.EndColumn}")
    ];

    sealed class SourceAuthorityValidator(bool isAuthoritative) : ISourceAuthorityValidator
    {
        public List<SourceRange> Validated { get; } = [];

        public bool IsAuthoritative(SourceRange source)
        {
            Validated.Add(source);
            return isAuthoritative;
        }
    }
}
