// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_freezing_declaration_input_type_shapes : given.declaration_input_members
{
    AdapterContributionAdmissionResult _result;
    List<TypeUseShapeKind> _shape;

    void Establish() => _shape = [TypeUseShapeKind.Collection, TypeUseShapeKind.Optional, TypeUseShapeKind.Named];

    void Because()
    {
        _result = AdmitEligibility(Surface(), SurfaceType(_shape), Transfer());
        _shape.Clear();
    }

    [Fact] void should_admit_independent_declaration_surface_capabilities() => _result.IsAdmitted.ShouldBeTrue();
    [Fact] void should_freeze_full_wrapper_order() => _result.Snapshot.Facts.OfType<DeclarationInputMemberTypeUseFact>().Single().Definition.Type.Shape.ShouldEqual(new[] { TypeUseShapeKind.Collection, TypeUseShapeKind.Optional, TypeUseShapeKind.Named });
}
