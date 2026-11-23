// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_admitting_explicit_mixed_slice_fallbacks : given.source_owner_candidates
{
    ArtifactSourcePlacementCandidateFact _input = null!;
    AdapterContributionAdmissionResult _result = null!;
    MixedSlicePlacementFallbackDefinition _fallback = null!;
    void Because()
    {
        _fallback = new() { Slice = "Register", Evidence = Evidence() };
        _input = Candidate() with { Definition = Candidate().Definition with { MixedSliceFallback = _fallback } };
        _result = Admit(descriptor: Descriptor([.. AllCapabilities(), GenerationFactCapability.MixedSlicePlacementFallback]), contribution: Contribution([_input]));
    }

    [Fact] void should_reject_an_undeclared_fallback_capability() => Admit(descriptor: Descriptor([.. AllCapabilities().Where(capability => capability != GenerationFactCapability.MixedSlicePlacementFallback)]), contribution: Contribution([_input])).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_an_unsupported_policy() => Admit(descriptor: Descriptor([.. AllCapabilities(), GenerationFactCapability.MixedSlicePlacementFallback]), contribution: Contribution([_input with { Definition = _input.Definition with { MixedSliceFallback = _fallback with { Version = 2 } } }])).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_a_path_instead_of_a_slice_name() => Admit(descriptor: Descriptor([.. AllCapabilities(), GenerationFactCapability.MixedSlicePlacementFallback]), contribution: Contribution([_input with { Definition = _input.Definition with { MixedSliceFallback = _fallback with { Slice = "Module/Feature/Slice" } } }])).IsAdmitted.ShouldBeFalse();
    [Fact] void should_accept_an_opt_in_without_an_artifact_name() => Admit(descriptor: Descriptor([.. AllCapabilities(), GenerationFactCapability.MixedSlicePlacementFallback]), contribution: Contribution([_input with { Definition = _input.Definition with { MixedSliceFallback = _fallback with { Slice = null } } }])).IsAdmitted.ShouldBeTrue();
    [Fact] void should_admit_the_inline_metadata() => _result.IsAdmitted.ShouldBeTrue();
    [Fact] void should_preserve_the_admitted_fallback() => FrozenFallback().ShouldEqual(_fallback);
    [Fact] void should_copy_nested_fallback_evidence() => ReferenceEquals(_fallback.Evidence, FrozenFallback().Evidence).ShouldBeFalse();
    [Fact] void should_serialize_and_preserve_the_fallback() => (JsonSerializer.Deserialize<ArtifactSourcePlacementCandidateFact>(JsonSerializer.Serialize(_input))?.Definition.MixedSliceFallback).ShouldEqual(_fallback);
    [Fact] void should_omit_null_metadata_from_legacy_json() => JsonSerializer.Serialize(Candidate()).Contains("MixedSliceFallback", StringComparison.Ordinal).ShouldBeFalse();

    MixedSlicePlacementFallbackDefinition FrozenFallback() => _result.Snapshot.Facts.OfType<ArtifactSourcePlacementCandidateFact>().Single().Definition.MixedSliceFallback ?? throw new InvalidOperationException("Expected admitted fallback metadata");
}
