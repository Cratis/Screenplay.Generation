// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Generation.for_EsmAdmission;

public class when_requesting_an_unknown_surface : Specification
{
    const EsmSurface UnknownSurface = (EsmSurface)1234;
    EsmCanonicalReadResult _result = null!;
    EsmAdmissionDecision _decision = null!;

    void Because()
    {
        _result = EsmCanonicalReader.Read(EsmGoldenVectors.Get(6).Bytes.AsSpan(), UnknownSurface);
        _decision = EsmAdmission.Evaluate(LanguageVersion.V6, SemanticVersion.V6, UnknownSurface);
    }

    [Fact] void should_refuse_the_unknown_surface() => _result.Decision.Disposition.ShouldEqual(EsmDisposition.Refused);
    [Fact] void should_identify_the_unknown_surface_reason() => _result.Decision.Reason.ShouldEqual(EsmAdmissionReason.UnknownSurface);
    [Fact] void should_not_return_a_model() => _result.Model.ShouldBeNull();
    [Fact] void should_not_admit_the_result() => _result.IsAdmitted.ShouldBeFalse();
    [Fact] void should_not_report_success() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_stable_refusal_code() => _result.Diagnostics.Single().Code.ShouldEqual("GEN0065");
    [Fact] void should_report_a_blocking_diagnostic() => _result.Diagnostics.Single().Severity.ShouldEqual(GenerationDiagnosticSeverity.Error);
    [Fact] void should_report_unsupported_processing() => _result.Diagnostics.Single().Outcome.ShouldEqual(GenerationDiagnosticOutcome.Unsupported);
    [Fact] void should_refuse_in_the_policy_too() => _decision.Disposition.ShouldEqual(EsmDisposition.Refused);
    [Fact] void should_not_treat_the_surface_as_inapplicable() => _decision.Reason.ShouldEqual(EsmAdmissionReason.UnknownSurface);
}
