// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_source_owner_discriminators_are_undefined : given.source_owner_candidates
{
    ArtifactSourcePlacementCandidateFact _candidate = null!;
    void Establish() => _candidate = Candidate();

    [Fact] void should_reject_undefined_strength() => AdmitCandidate(_candidate with { Evidence = _candidate.Evidence with { Strength = (EvidenceStrength)99 } }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_undefined_mode() => AdmitCandidate(_candidate with { Definition = _candidate.Definition with { Policy = new() { Mode = (ArtifactSourcePlacementSelectionMode)99 } } }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_undefined_reason() => AdmitCandidate(_candidate with { Definition = _candidate.Definition with { Reason = (ArtifactSourcePlacementReason)99 } }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_undefined_kind() => AdmitCandidate(_candidate with { Definition = _candidate.Definition with { SliceKind = (GenerationSliceKind)99 } }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_mismatched_subject() => AdmitCandidate(_candidate with { Subject = ExternalSubject }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_foreign_evidence() => AdmitCandidate(_candidate with { Evidence = _candidate.Evidence with { Adapter = new() { Id = "other", Version = "1" } } }).IsAdmitted.ShouldBeFalse();
}
