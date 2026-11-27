// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_freezing_jagged_member_views : given.jagged_array_members
{
    ArtifactMemberTypeUseFact _input = null!;
    ArtifactMemberTypeUseFact _admitted = null!;
    ArtifactMemberTypeUseFact _recorded = null!;
    ArtifactMemberTypeUseFact _roundTripped = null!;
    List<TypeUseCollectionOriginKind> _admissionOrigins = null!;
    List<TypeUseCollectionOriginKind> _snapshotOrigins = null!;
    string _defaultJson = null!;

    void Establish()
    {
        _admissionOrigins = [.. ArrayOfArrays];
        _snapshotOrigins = [.. ArrayOfArrays];
        _input = MemberFacts(true, _admissionOrigins).OfType<ArtifactMemberTypeUseFact>().Single();
    }

    void Because()
    {
        var snapshot = Completed(Adapter, []);
        var descriptor = snapshot.Descriptor with { EmittedFactCapabilities = [GenerationFactCapability.ArtifactMemberTypeUse] };
        var admission = AdapterContributionAdmission.Admit(descriptor, Contribution(_input));
        _admitted = admission.Snapshot!.Facts.OfType<ArtifactMemberTypeUseFact>().Single();
        _admissionOrigins.Clear();

        var raw = _input with { Definition = _input.Definition with { CollectionOrigins = _snapshotOrigins } };
        var generated = Generator.Generate(Snapshot(Completed(Adapter, [raw])), new ScreenplayGenerationOptions { Domain = "Ordering" });
        _recorded = (ArtifactMemberTypeUseFact)generated.AdapterRun!.Facts.Single().Fact;
        _snapshotOrigins.Clear();

        _roundTripped = JsonSerializer.Deserialize<ArtifactMemberTypeUseFact>(JsonSerializer.Serialize(_recorded))!;
        _defaultJson = JsonSerializer.Serialize(_input with { Definition = _input.Definition with { UseLegacyJaggedArrayReference = false, CollectionOrigins = null } });
    }

    [Fact] void should_keep_the_admitted_request() => _admitted.Definition.UseLegacyJaggedArrayReference.ShouldBeTrue();
    [Fact] void should_freeze_the_admitted_origins() => _admitted.Definition.CollectionOrigins.ShouldEqual<TypeUseCollectionOriginKind>(ArrayOfArrays);
    [Fact] void should_keep_the_request_in_the_run_record() => _recorded.Definition.UseLegacyJaggedArrayReference.ShouldBeTrue();
    [Fact] void should_freeze_origins_in_the_run_record() => _recorded.Definition.CollectionOrigins.ShouldEqual<TypeUseCollectionOriginKind>(ArrayOfArrays);
    [Fact] void should_round_trip_the_complete_run_fact() => JsonSerializer.Serialize(_roundTripped).ShouldEqual(JsonSerializer.Serialize(_recorded));
    [Fact] void should_preserve_the_fact_identity() => _roundTripped.Id.ShouldEqual(_input.Id);
    [Fact] void should_not_serialize_the_default_view_request() => _defaultJson.ShouldNotContain(nameof(ArtifactMemberTypeUseDefinition.UseLegacyJaggedArrayReference));
    [Fact] void should_not_serialize_the_default_origins() => _defaultJson.ShouldNotContain(nameof(ArtifactMemberTypeUseDefinition.CollectionOrigins));
}
