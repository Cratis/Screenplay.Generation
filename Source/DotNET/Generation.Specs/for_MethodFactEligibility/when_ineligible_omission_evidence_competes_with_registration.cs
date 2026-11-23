// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_MethodFactEligibility;

public class when_ineligible_omission_evidence_competes_with_registration : for_GenerationFactDerivation.given.occurrence_omissions
{
    OccurrenceOmissionDiagnosticFact _conditional;
    OccurrenceOmissionDiagnosticFact _registration;
    MethodFactSupportFact _support;
    MethodExecutionClassificationFact _failure;
    GenerationDiagnostic _diagnostic;
    void Establish()
    {
        _conditional = Candidate("conditional", OmissionDiagnosticEvidencePreference.IdentityConfiguration);
        _registration = Candidate("registration", OmissionDiagnosticEvidencePreference.ExplicitRegistration);
        var parent = new SubjectId { Value = "symbol://methods/Parent" };
        _support = Conditional(_conditional);
        _support = _support with { Definition = _support.Definition with { Alternatives = [new() { Method = Method, EnclosingExecutables = [parent] }] } };
        _failure = Classification(MethodExecutionKind.FailureOnly);
        _failure = _failure with { Subject = parent, Definition = _failure.Definition with { Method = parent, OmissionReason = Payload() } };
    }
    void Because() => _diagnostic = Omissions(Derive([.. CandidateFacts(_conditional, _registration), _support, _failure])).Single();
    [Fact] void should_keep_the_independent_registration_warning() => _diagnostic.Source.ShouldEqual(_registration.Evidence.Source);
    [Fact] void should_not_use_ineligible_preferred_evidence() => _diagnostic.Lineage.Inputs.Contains(_conditional.Id).ShouldBeFalse();
    [Fact] void should_not_retain_any_ineligible_producer_evidence() => _diagnostic.Lineage.Evidence.Any(evidence => evidence.Adapter == _conditional.Evidence.Adapter).ShouldBeFalse();
    [Fact] void should_emit_no_omission_when_all_candidates_are_ineligible() => Omissions(Derive([.. CandidateFacts(_conditional), _support, _failure])).ShouldBeEmpty();
    [Fact] void should_retain_eligible_method_support_and_classification_evidence()
    {
        var support = Conditional(_conditional);
        var classification = Classification();
        var result = Omissions(Derive([.. CandidateFacts(_conditional), support, classification])).Single();
        result.Lineage.Inputs.Contains(support.Id).ShouldBeTrue();
        result.Lineage.Inputs.Contains(classification.Id).ShouldBeTrue();
    }
    [Fact] void should_not_accept_conflicting_method_classifications()
    {
        var ordinary = Classification();
        var failure = Classification(MethodExecutionKind.FailureOnly);
        failure = failure with { Id = new() { Value = "application:failure" }, Definition = failure.Definition with { OmissionReason = Payload() } };
        var result = Derive([.. CandidateFacts(_conditional, _registration), Conditional(_conditional), ordinary, failure]);
        Omissions(result).Single().Source.ShouldEqual(_registration.Evidence.Source);
    }
}
