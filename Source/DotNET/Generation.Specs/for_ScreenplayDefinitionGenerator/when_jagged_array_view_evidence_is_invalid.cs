// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_jagged_array_view_evidence_is_invalid : given.jagged_array_members
{
    const TypeUseCollectionOriginKind Array = TypeUseCollectionOriginKind.Array;
    const TypeUseCollectionOriginKind Enumerable = TypeUseCollectionOriginKind.Enumerable;

    GeneratedScreenplayDefinition[] _results = null!;

    void Because() => _results =
    [
        Generate(true, null),
        Generate(true, [Enumerable, Enumerable]),
        Generate(true, [Enumerable, Array]),
        Generate(true, [Array, Enumerable]),
        Generate(true, [Array]),
        Generate(true, [Array, Array, Array]),
        Generate(true, [Array, TypeUseCollectionOriginKind.Unknown]),
        Generate(true, [Array, (TypeUseCollectionOriginKind)9]),
        Generate(true, [Array], [TypeUseShapeKind.Collection, TypeUseShapeKind.Named]),
        Generate(true, ArrayOfArrays, projection: true)
    ];

    [Fact] void should_reject_every_inconsistent_view() => _results.All(result => HasCode(result, GenerationDiagnosticCodes.InvalidTypeUseCompatibilityProjection)).ShouldBeTrue();
    [Fact] void should_not_succeed_for_any_of_them() => _results.Any(result => result.IsSuccess).ShouldBeFalse();
}
