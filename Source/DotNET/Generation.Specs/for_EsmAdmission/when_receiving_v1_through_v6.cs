// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Generation.for_EsmAdmission;

public class when_receiving_v1_through_v6 : Specification
{
    EsmGoldenVector[] _vectors = [];
    EsmCanonicalReadResult[] _results = [];
    EsmAdmissionDecision[] _recovery = [];
    EsmAdmissionDecision[] _unlisted = [];
    byte[][] _roundTrips = [];

    void Establish() => _vectors =
    [
        EsmGoldenVectors.Get(1), EsmGoldenVectors.Get(2), EsmGoldenVectors.Get(3),
        EsmGoldenVectors.Get(4), EsmGoldenVectors.Get(5), EsmGoldenVectors.Get(6)
    ];

    void Because()
    {
        _results = [.. _vectors.Select(vector => EsmCanonicalReader.Read(vector.Bytes.AsSpan(), EsmSurface.Reading))];
        _roundTrips = [.. _results.Select(result => SemanticModelSerializer.Serialize(result.Model!))];
        _recovery = [.. _results.Select(result => EsmAdmission.Evaluate(result.Model!.LanguageVersion, result.Model.SemanticVersion, EsmSurface.ReverseRecovery))];
        (LanguageVersion Language, SemanticVersion Semantic)[] unlisted =
        [
            (new(1, 1), SemanticVersion.V1), (LanguageVersion.V1, new(1, 1)),
            (LanguageVersion.V1, SemanticVersion.V2), (new(0, 0), new(0, 0)), (new(100, 0), new(100, 0))
        ];
        _unlisted = [.. unlisted.SelectMany(pair => new[] { EsmSurface.Reading, EsmSurface.ReverseRecovery }
            .Select(surface => EsmAdmission.Evaluate(pair.Language, pair.Semantic, surface)))];
    }

    [Fact] void should_exercise_exactly_six_legacy_vectors() => _results.Length.ShouldEqual(6);
    [Fact] void should_admit_reading() => _results.All(result => result.Decision.Disposition == EsmDisposition.Admitted && result.IsAdmitted).ShouldBeTrue();
    [Fact] void should_report_success() => _results.All(result => result.IsSuccess).ShouldBeTrue();
    [Fact] void should_release_every_model() => _results.All(result => result.Model is not null).ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _results.All(result => result.Diagnostics.IsEmpty).ShouldBeTrue();
    [Fact] void should_have_no_refusal_reason() => _results.All(result => result.Decision.Reason == EsmAdmissionReason.None).ShouldBeTrue();
    [Fact] void should_preserve_canonical_bytes() => _vectors.Select((vector, index) => vector.Bytes.AsSpan().SequenceEqual(_roundTrips[index])).All(equal => equal).ShouldBeTrue();
    [Fact] void should_preserve_revisions() => _results.Select(result => result.Model!.Revision).SequenceEqual(_vectors.Select(vector => vector.SemanticRevision)).ShouldBeTrue();
    [Fact] void should_preserve_the_bounded_legacy_recovery_allowance() => _recovery.All(decision => decision.Disposition == EsmDisposition.Admitted && decision.Diagnostics.IsEmpty).ShouldBeTrue();
    [Fact] void should_check_unlisted_pairs_on_both_surfaces() => _unlisted.Length.ShouldEqual(10);
    [Fact] void should_not_admit_unlisted_pairs() => _unlisted.All(decision => decision.Disposition == EsmDisposition.Refused && decision.Reason == EsmAdmissionReason.UnsupportedVersion).ShouldBeTrue();
}
