// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_admitting_mixed_slice_fallbacks_on_completed_placements : given.a_contribution
{
    AdapterContributionAdmissionResult _valid = null!;
    AdapterContributionAdmissionResult _undeclared = null!;
    AdapterContributionAdmissionResult _unsupportedVersion = null!;
    AdapterContributionAdmissionResult _pathShapedName = null!;
    ArtifactPlacementFact _input = null!;
    MixedSlicePlacementFallbackDefinition _fallback = null!;

    void Establish()
    {
        _fallback = new() { Slice = "Register", Evidence = Evidence() };
        _input = EveryFact().OfType<ArtifactPlacementFact>().Single() with { MixedSliceFallback = _fallback };
    }

    void Because()
    {
        _valid = Admit(contribution: Contribution([_input]));
        _undeclared = Admit(descriptor: Descriptor([.. AllCapabilities().Where(capability => capability != GenerationFactCapability.MixedSlicePlacementFallback)]), contribution: Contribution([_input]));
        _unsupportedVersion = Admit(contribution: Contribution([_input with { MixedSliceFallback = _fallback with { Version = 2 } }]));
        _pathShapedName = Admit(contribution: Contribution([_input with { MixedSliceFallback = _fallback with { Slice = "Module/Feature/Slice" } }]));
    }

    [Fact] void should_admit_the_explicit_placement_fallback() => _valid.IsAdmitted.ShouldBeTrue();
    [Fact] void should_reject_an_undeclared_fallback_capability() => _undeclared.IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_an_unsupported_policy_version() => _unsupportedVersion.IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_a_path_instead_of_a_slice_name() => _pathShapedName.IsAdmitted.ShouldBeFalse();
    [Fact] void should_preserve_the_admitted_fallback() => FrozenFallback().ShouldEqual(_fallback);
    [Fact] void should_copy_the_fallback_evidence() => ReferenceEquals(FrozenFallback().Evidence, _fallback.Evidence).ShouldBeFalse();

    MixedSlicePlacementFallbackDefinition FrozenFallback() => _valid.Snapshot.Facts.OfType<ArtifactPlacementFact>().Single().MixedSliceFallback ?? throw new InvalidOperationException("Expected admitted fallback metadata");
}
