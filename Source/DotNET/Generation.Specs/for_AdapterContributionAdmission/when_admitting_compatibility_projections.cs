// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_admitting_compatibility_projections : given.a_compatibility_projection
{
    AdapterContributionAdmissionResult _result = null!;
    TypeUseNodeObservation[] _nodes = null!;
    TypeUseCompatibilityProjectionFact _roundTrip = null!;

    void Establish() => _nodes = Nodes();
    void Because()
    {
        _result = AdmitProjection(observation: Observation(_nodes));
        _nodes[1] = new() { Name = "Changed" };
        _roundTrip = JsonSerializer.Deserialize<TypeUseCompatibilityProjectionFact>(JsonSerializer.Serialize(Projection()))!;
    }

    [Fact] void should_admit_the_local_view() => _result.IsAdmitted.ShouldBeTrue();
    [Fact] void should_keep_the_full_shape() => FrozenObservation().Definition.Type.Shape.Count.ShouldEqual(3);
    [Fact] void should_freeze_supplemental_evidence() => FrozenObservation().Definition.Type.Nodes![1].Name.ShouldEqual("List");
    [Fact] void should_keep_the_selected_node_identity() => FrozenProjection().Definition.Type.ObservedTypeSubject.ShouldEqual(Subject("container"));
    [Fact] void should_keep_the_original_terminal() => FrozenObservation().Definition.Type.ObservedTypeSubject.ShouldEqual(ExternalSubject);
    [Fact] void should_round_trip_the_local_reference() => _roundTrip.Definition.TypeUse.ShouldEqual(Observation().Id);
    [Fact] void should_round_trip_the_profile() => _roundTrip.Definition.Profile.ShouldEqual(TypeUseCompatibilityProfile.OneLayerCollection);
    [Fact] void should_admit_the_round_tripped_contract() => AdmitProjection(_roundTrip).IsAdmitted.ShouldBeTrue();
    [Fact] void should_round_trip_node_observations() => JsonSerializer.Deserialize<TypeUseDefinition>(JsonSerializer.Serialize(Observation().Definition.Type))!.Nodes![1].Subject.ShouldEqual(Subject("container"));
    [Fact] void should_require_the_declared_capability() => Admit(descriptor: Descriptor([.. AllCapabilities().Where(capability => capability != GenerationFactCapability.TypeUseCompatibilityProjection)]), contribution: Contribution([Observation(), Projection()])).IsAdmitted.ShouldBeFalse();
    [Fact] void should_support_terminal_projections_without_metadata() => AdmitProjection(
        Projection() with
        {
            Definition = Projection().Definition with { SourceNodeIndex = 2, Type = Observation().Definition.Type with { Nodes = null, Shape = [TypeUseShapeKind.Optional, TypeUseShapeKind.Collection, TypeUseShapeKind.Named] } }
        },
        Observation() with { Definition = Observation().Definition with { Type = Observation().Definition.Type with { Nodes = null, Shape = [TypeUseShapeKind.Optional, TypeUseShapeKind.Collection, TypeUseShapeKind.Named] } } }).IsAdmitted.ShouldBeTrue();
    [Fact] void should_support_declaration_input_observations() => AdmitProjection(observation: new DeclarationInputMemberTypeUseFact
    {
        Id = Observation().Id, Subject = ArtifactSubject, Evidence = Evidence(),
        Definition = new() { Member = new() { Declaration = ArtifactSubject, Name = "input" }, Type = Observation().Definition.Type }
    }).IsAdmitted.ShouldBeTrue();

    ArtifactMemberTypeUseFact FrozenObservation() => _result.Snapshot!.Facts.OfType<ArtifactMemberTypeUseFact>().Single();
    TypeUseCompatibilityProjectionFact FrozenProjection() => _result.Snapshot!.Facts.OfType<TypeUseCompatibilityProjectionFact>().Single();
}
