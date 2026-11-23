// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission.given;

public class source_owner_candidates : a_contribution
{
    protected static ArtifactSourcePlacementCandidateFact Candidate() => new()
    {
        Id = Id("source-owner"), Subject = ArtifactSubject, Evidence = Evidence(),
        Definition = new()
        {
            Artifact = ArtifactKey(ArtifactSubject, ArtifactKind.Command), Owner = ArtifactKey(ArtifactSubject, ArtifactKind.Command),
            Reason = ArtifactSourcePlacementReason.Self, SliceKind = GenerationSliceKind.StateChange, Policy = new()
        }
    };

    protected static AdapterContributionAdmissionResult AdmitCandidate(ArtifactSourcePlacementCandidateFact candidate) => Admit(descriptor: Descriptor([.. AllCapabilities(), GenerationFactCapability.ArtifactSourcePlacementCandidate]), contribution: Contribution([candidate]));
}
