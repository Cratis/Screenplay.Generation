// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Generation.for_EsmAdmission;

public class when_receiving_v7 : given.v7_corpora
{
    (EsmSurface Surface, EsmCanonicalReadResult Read, EsmAdmissionDecision Policy)[] _results = [];

    void Because() => _results = [.. Corpora.SelectMany(corpus => new[] { EsmSurface.Reading, EsmSurface.ReverseRecovery }
        .Select(surface => (
            surface,
            EsmCanonicalReader.Read(corpus.EsmBytes.AsSpan(), surface),
            EsmAdmission.Evaluate(LanguageVersion.V7, SemanticVersion.V7, surface))))];

    [Fact] void should_exercise_both_corpora_on_both_surfaces() => _results.Length.ShouldEqual(4);
    [Fact] void should_validate_both_corpora_as_v7() => ValidatedModels.All(model => model.LanguageVersion == LanguageVersion.V7 && model.SemanticVersion == SemanticVersion.V7).ShouldBeTrue();
    [Fact] void should_refuse_every_result() => _results.All(result => result.Read.Decision.Disposition == EsmDisposition.Refused).ShouldBeTrue();
    [Fact] void should_not_admit_a_model() => _results.All(result => !result.Read.IsAdmitted && result.Read.Model is null).ShouldBeTrue();
    [Fact] void should_not_report_success() => _results.Any(result => result.Read.IsSuccess).ShouldBeFalse();
    [Fact] void should_identify_the_unsupported_version() => _results.All(result => result.Read.Decision.Reason == EsmAdmissionReason.UnsupportedVersion).ShouldBeTrue();
    [Fact] void should_report_exactly_one_diagnostic_per_result() => _results.All(result => result.Read.Diagnostics.Length == 1).ShouldBeTrue();
    [Fact] void should_use_the_stable_refusal_code() => _results.All(result => result.Read.Diagnostics.Single().Code == "GEN0065").ShouldBeTrue();
    [Fact] void should_block_publication() => _results.All(result => result.Read.Diagnostics.Single().Severity == GenerationDiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_report_unsupported_semantics() => _results.All(result => result.Read.Diagnostics.Single().Outcome == GenerationDiagnosticOutcome.Unsupported).ShouldBeTrue();
    [Fact] void should_identify_the_surface() => _results.All(result => result.Read.Diagnostics.Single().Message.Contains(result.Surface.ToString(), StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_identify_both_versions() => _results.All(result => result.Read.Diagnostics.Single().Message.Contains("language 7.0 / semantic 7.0", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_link_the_admission_issue() => _results.All(result => result.Read.Diagnostics.Single().Message.Contains("https://github.com/Cratis/Screenplay.Generation/issues/71", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_invent_a_source_location() => _results.All(result => result.Read.Diagnostics.Single().Source is null).ShouldBeTrue();
    [Fact] void should_agree_with_the_shared_policy() => _results.All(result => result.Read.Decision.Disposition == result.Policy.Disposition && result.Read.Decision.Reason == result.Policy.Reason && result.Read.Diagnostics.Single() == result.Policy.Diagnostics.Single()).ShouldBeTrue();
    [Fact] void should_preserve_the_original_bytes() => Corpora.Select((corpus, index) => corpus.EsmBytes.AsSpan().SequenceEqual(OriginalBytes[index])).All(equal => equal).ShouldBeTrue();
    [Fact] void should_preserve_the_original_revisions() => ValidatedModels.Select(model => model.Revision).SequenceEqual(OriginalRevisions).ShouldBeTrue();
}
