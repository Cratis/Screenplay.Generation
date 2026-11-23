// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_validating_projection_evidence : given.a_compatibility_projection
{
    AdapterContributionAdmissionResult _unauthorized = null!;
    AdapterContributionAdmissionResult _frozen = null!;
    AdapterContributionAdmissionResult _scoped = null!;

    void Because()
    {
        var descriptor = Descriptor([.. AllCapabilities(), GenerationFactCapability.TypeUseCompatibilityProjection]);
        _unauthorized = Admit(descriptor: descriptor, contribution: Contribution([Observation(), Projection()]), validator: new UnauthoritativeSource());
        TypeUseShapeKind[] shape = [TypeUseShapeKind.Collection, TypeUseShapeKind.Named];
        TypeUseNodeObservation[] nodes = [Nodes()[0], Nodes()[1]];
        var projection = Projection() with { Definition = Projection().Definition with { Type = Projection().Definition.Type with { Shape = shape, Nodes = nodes } } };
        _frozen = AdmitProjection(projection);
        shape[0] = TypeUseShapeKind.Optional;
        nodes[0] = new() { Name = "Changed" };
        var scope = new BackendScopeKey { Store = Subject("store"), EventModelName = "Orders" };
        var exactNodes = Nodes();
        exactNodes[1] = exactNodes[1] with { Scope = scope };
        _scoped = AdmitProjection(Projection() with { Definition = Projection().Definition with { Type = Projection().Definition.Type with { ObservedScope = scope } } }, Observation(exactNodes));
    }

    [Fact] void should_reject_unauthoritative_projection_evidence() => _unauthorized.IsAdmitted.ShouldBeFalse();
    [Fact] void should_locate_the_authority_failure() => _unauthorized.Diagnostics.Any(diagnostic => diagnostic.Source == Projection().Evidence.Source).ShouldBeTrue();
    [Fact] void should_admit_evidenced_projected_nodes() => _frozen.IsAdmitted.ShouldBeTrue();
    [Fact] void should_freeze_the_printable_shape() => Frozen().Definition.Type.Shape[0].ShouldEqual(TypeUseShapeKind.Collection);
    [Fact] void should_freeze_the_printable_node_metadata() => Frozen().Definition.Type.Nodes![0].Name.ShouldEqual("List");
    [Fact] void should_admit_the_exact_selected_scope() => _scoped.IsAdmitted.ShouldBeTrue();
    [Fact] void should_keep_the_selected_scope() => _scoped.Snapshot!.Facts.OfType<TypeUseCompatibilityProjectionFact>().Single().Definition.Type.ObservedScope!.EventModelName.ShouldEqual("Orders");
    [Fact] void should_reject_invented_projected_node_metadata() => AdmitProjection(Projection() with
    {
        Definition = Projection().Definition with { Type = Projection().Definition.Type with { Nodes = [new() { Name = "Different" }, Nodes()[1]] } }
    }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_missing_projection_payloads() => AdmitProjection(Projection() with { Definition = null! }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_missing_printable_types() => AdmitProjection(Projection() with { Definition = Projection().Definition with { Type = null! } }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_null_node_entries() => AdmitProjection(observation: Observation([Nodes()[0], null!, Nodes()[2]])).IsAdmitted.ShouldBeFalse();

    TypeUseCompatibilityProjectionFact Frozen() => _frozen.Snapshot!.Facts.OfType<TypeUseCompatibilityProjectionFact>().Single();

    sealed class UnauthoritativeSource : ISourceAuthorityValidator
    {
        public bool IsAuthoritative(SourceRange source) => false;
    }
}
