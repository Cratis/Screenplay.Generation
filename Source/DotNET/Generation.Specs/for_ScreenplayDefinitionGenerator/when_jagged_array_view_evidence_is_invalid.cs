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
        .. GenerateBoth(true, null),
        .. GenerateBoth(true, [Enumerable, Enumerable]),
        .. GenerateBoth(true, [Enumerable, Array]),
        .. GenerateBoth(true, [Array, Enumerable]),
        .. GenerateBoth(true, [Array]),
        .. GenerateBoth(true, [Array, Array, Array]),
        .. GenerateBoth(true, [Array, TypeUseCollectionOriginKind.Unknown]),
        .. GenerateBoth(true, [Array, (TypeUseCollectionOriginKind)9]),
        .. GenerateBoth(false, [Array, Array], [TypeUseShapeKind.Collection, TypeUseShapeKind.Named]),
        .. GenerateBoth(true, [], [TypeUseShapeKind.Named]),
        .. GenerateBoth(true, ArrayOfArrays, name: string.Empty),
        .. GenerateBoth(true, ArrayOfArrays, projection: true)
    ];

    [Fact] void should_reject_every_inconsistent_view() => _results
        .Select((result, index) => (Result: result, Index: index))
        .Where(item => !HasCode(item.Result, GenerationDiagnosticCodes.InvalidTypeUseCompatibilityProjection))
        .Select(item => $"Case {item.Index / 2}, path {item.Index % 2}: {string.Join(", ", item.Result.Diagnostics.Select(diagnostic => diagnostic.Code))}")
        .ShouldBeEmpty();
    [Fact] void should_not_succeed_for_any_of_them() => _results.Any(result => result.IsSuccess).ShouldBeFalse();
}
