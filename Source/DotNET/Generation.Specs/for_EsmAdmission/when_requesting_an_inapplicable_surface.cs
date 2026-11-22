// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Generation.for_EsmAdmission;

public class when_requesting_an_inapplicable_surface : given.v7_corpora
{
    EsmCanonicalReadResult[] _results = [];
    EsmAdmissionDecision[] _decisions = [];

    void Because()
    {
        EsmSurface[] surfaces = [EsmSurface.ReferenceExecution, EsmSurface.Rendering];
        var inputs = Corpora.Select(corpus => corpus.EsmBytes).Prepend(EsmGoldenVectors.Get(6).Bytes);
        _results = [.. inputs.SelectMany(bytes => surfaces.Select(surface => EsmCanonicalReader.Read(bytes.AsSpan(), surface)))];
        _decisions = [.. new[] { (LanguageVersion.V6, SemanticVersion.V6), (LanguageVersion.V7, SemanticVersion.V7) }
            .SelectMany(pair => surfaces.Select(surface => EsmAdmission.Evaluate(pair.Item1, pair.Item2, surface)))];
    }

    [Fact] void should_check_both_absent_surfaces_for_legacy_and_both_v7_corpora() => _results.Length.ShouldEqual(6);
    [Fact] void should_mark_both_surfaces_inapplicable() => _results.All(result => result.Decision.Disposition == EsmDisposition.Inapplicable).ShouldBeTrue();
    [Fact] void should_identify_the_inapplicable_surface_reason() => _results.All(result => result.Decision.Reason == EsmAdmissionReason.InapplicableSurface).ShouldBeTrue();
    [Fact] void should_not_release_a_model() => _results.All(result => result.Model is null).ShouldBeTrue();
    [Fact] void should_not_admit_or_report_a_successful_run() => _results.All(result => !result.IsAdmitted && !result.IsSuccess).ShouldBeTrue();
    [Fact] void should_report_a_blocking_surface_diagnostic() => _results.All(result => result.Diagnostics.Single().Code == "GEN0065" && result.Diagnostics.Single().Severity == GenerationDiagnosticSeverity.Error && result.Diagnostics.Single().Outcome == GenerationDiagnosticOutcome.Unsupported).ShouldBeTrue();
    [Fact] void should_use_the_same_disposition_in_the_shared_policy() => _decisions.All(decision => decision.Disposition == EsmDisposition.Inapplicable && decision.Reason == EsmAdmissionReason.InapplicableSurface).ShouldBeTrue();
}
