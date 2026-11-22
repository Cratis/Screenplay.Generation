// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Generation.for_EsmAdmission;

public class when_receiving_a_later_version : Specification
{
    EsmGoldenVector _vector = null!;
    ExecutableSemanticModel _validated = null!;
    EsmCanonicalReadResult[] _results = [];
    EsmAdmissionDecision[] _decisions = [];

    void Establish()
    {
        _vector = EsmGoldenVectors.Get(8);
        _validated = SemanticModelSerializer.Deserialize(_vector.Bytes.AsSpan());
    }

    void Because()
    {
        EsmSurface[] surfaces = [EsmSurface.Reading, EsmSurface.ReverseRecovery];
        _results = [.. surfaces.Select(surface => EsmCanonicalReader.Read(_vector.Bytes.AsSpan(), surface))];
        _decisions = [.. surfaces.Select(surface => EsmAdmission.Evaluate(_validated.LanguageVersion, _validated.SemanticVersion, surface))];
    }

    [Fact] void should_validate_the_bundled_v8_vector() => _validated.LanguageVersion.ShouldEqual(LanguageVersion.V8);
    [Fact] void should_validate_v8_semantics() => _validated.SemanticVersion.ShouldEqual(SemanticVersion.V8);
    [Fact] void should_check_both_surfaces() => _results.Length.ShouldEqual(2);
    [Fact] void should_refuse_reading_and_recovery() => _results.All(result => result.Decision.Disposition == EsmDisposition.Refused && result.Decision.Reason == EsmAdmissionReason.UnsupportedVersion).ShouldBeTrue();
    [Fact] void should_not_release_a_model_or_report_success() => _results.All(result => result.Model is null && !result.IsAdmitted && !result.IsSuccess).ShouldBeTrue();
    [Fact] void should_report_the_blocking_version_diagnostic() => _results.All(result => result.Diagnostics.Single().Code == "GEN0065" && result.Diagnostics.Single().Severity == GenerationDiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_keep_package_support_separate_from_policy_admission() => _decisions.All(decision => decision.Disposition == EsmDisposition.Refused && decision.Reason == EsmAdmissionReason.UnsupportedVersion).ShouldBeTrue();
}
