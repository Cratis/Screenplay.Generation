// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_rejecting_invalid_compatibility_projections : given.a_compatibility_projection
{
    static bool Rejected(TypeUseCompatibilityProjectionDefinition definition) => !AdmitProjection(Projection() with { Definition = definition }).IsAdmitted;
    static bool RejectedType(TypeUseDefinition type) => Rejected(Projection().Definition with { Type = type });
    static bool RejectedNodes(params TypeUseNodeObservation[] nodes) => !AdmitProjection(observation: Observation(nodes)).IsAdmitted;

    [Fact] void should_reject_unknown_profiles() => Rejected(Projection().Definition with { Profile = TypeUseCompatibilityProfile.Unknown }).ShouldBeTrue();
    [Fact] void should_reject_undefined_profiles() => Rejected(Projection().Definition with { Profile = (TypeUseCompatibilityProfile)99 }).ShouldBeTrue();
    [Fact] void should_reject_future_versions() => Rejected(Projection().Definition with { Version = 2 }).ShouldBeTrue();
    [Fact] void should_reject_default_versions() => Rejected(Projection().Definition with { Version = 0 }).ShouldBeTrue();
    [Fact] void should_reject_missing_observations() => Rejected(Projection().Definition with { TypeUse = Id("missing") }).ShouldBeTrue();
    [Fact] void should_reject_a_hidden_terminal_projection() => Rejected(Projection().Definition with { SourceNodeIndex = 2, Type = Observation().Definition.Type with { Nodes = null, Shape = [TypeUseShapeKind.Collection, TypeUseShapeKind.Named] } }).ShouldBeTrue();
    [Fact] void should_reject_the_wrong_stopping_index() => Rejected(Projection().Definition with { SourceNodeIndex = 0 }).ShouldBeTrue();
    [Fact] void should_reject_a_negative_index() => Rejected(Projection().Definition with { SourceNodeIndex = -1 }).ShouldBeTrue();
    [Fact] void should_reject_the_wrong_name() => RejectedType(Projection().Definition.Type with { Name = "External" }).ShouldBeTrue();
    [Fact] void should_reject_an_empty_name() => RejectedType(Projection().Definition.Type with { Name = "" }).ShouldBeTrue();
    [Fact] void should_reject_the_wrong_subject() => RejectedType(Projection().Definition.Type with { ObservedTypeSubject = ExternalSubject }).ShouldBeTrue();
    [Fact] void should_reject_the_wrong_flags() => RejectedType(Projection().Definition.Type with { Shape = [TypeUseShapeKind.Optional, TypeUseShapeKind.Collection, TypeUseShapeKind.Named] }).ShouldBeTrue();
    [Fact] void should_reject_nonprintable_shapes() => RejectedType(Observation().Definition.Type).ShouldBeTrue();
    [Fact] void should_reject_the_wrong_scope() => RejectedType(Projection().Definition.Type with { ObservedScope = new() { Store = Subject("store") } }).ShouldBeTrue();
    [Fact] void should_require_intermediate_node_evidence() => AdmitProjection(observation: Observation() with { Definition = Observation().Definition with { Type = Observation().Definition.Type with { Nodes = null } } }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_missing_nodes() => RejectedNodes(Nodes()[0]).ShouldBeTrue();
    [Fact] void should_reject_mismatched_terminals() => RejectedNodes(Nodes()[0], Nodes()[1], new() { Name = "Other", Subject = ExternalSubject }).ShouldBeTrue();
    [Fact] void should_reject_invalid_node_names() => RejectedNodes(Nodes()[0], new() { Name = "bad\nname" }, Nodes()[2]).ShouldBeTrue();
    [Fact] void should_reject_node_scope_without_subject() => RejectedNodes(Nodes()[0], new() { Name = "List", Scope = new() { Store = Subject("store") } }, Nodes()[2]).ShouldBeTrue();
    [Fact] void should_reject_nonexact_projections() => AdmitProjection(Projection() with { Evidence = Evidence() with { Strength = EvidenceStrength.Heuristic } }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_nonexact_observations() => AdmitProjection(observation: Observation() with { Evidence = Evidence() with { Strength = EvidenceStrength.Heuristic } }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_foreign_owners() => AdmitProjection(Projection() with { Subject = Subject("foreign") }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_foreign_adapters() => AdmitProjection(Projection() with { Evidence = Evidence() with { Adapter = new() { Id = "foreign", Version = "1" } } }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_foreign_source_locations() => AdmitProjection(Projection() with { Evidence = Evidence() with { Source = Evidence().Source! with { StartLine = 99 } } }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_conflicting_views() => Admit(descriptor: Descriptor([.. AllCapabilities(), GenerationFactCapability.TypeUseCompatibilityProjection]), contribution: Contribution([Observation(), Projection(), Projection() with { Id = Id("other-projection"), Definition = Projection().Definition with { Version = 2 } }])).IsAdmitted.ShouldBeFalse();
}
