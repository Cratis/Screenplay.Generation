// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_admitting_jagged_array_views : given.a_contribution
{
    const TypeUseCollectionOriginKind Array = TypeUseCollectionOriginKind.Array;
    const TypeUseCollectionOriginKind Enumerable = TypeUseCollectionOriginKind.Enumerable;

    static readonly TypeUseShapeKind[] _jagged = [TypeUseShapeKind.Collection, TypeUseShapeKind.Collection, TypeUseShapeKind.Named];

    static bool Admitted(bool requested, IReadOnlyList<TypeUseCollectionOriginKind>? origins, TypeUseShapeKind[]? shape = null, bool projection = false, string name = "Int")
    {
        var facts = EveryFact();
        var index = facts.FindIndex(fact => fact is ArtifactMemberTypeUseFact);
        var original = (ArtifactMemberTypeUseFact)facts[index];
        facts[index] = original with
        {
            Definition = original.Definition with
            {
                Type = new() { Name = name, Shape = shape ?? _jagged },
                UseLegacyJaggedArrayReference = requested,
                CollectionOrigins = origins
            }
        };
        if (projection)
        {
            facts.Add(new TypeUseCompatibilityProjectionFact
            {
                Id = Id("jagged-projection"),
                Subject = ArtifactSubject,
                Evidence = Evidence(),
                Definition = new()
                {
                    TypeUse = original.Id,
                    Profile = TypeUseCompatibilityProfile.OneLayerCollection,
                    Version = 1,
                    SourceNodeIndex = 1,
                    Type = new() { Name = "Int", Shape = [TypeUseShapeKind.Collection, TypeUseShapeKind.Named] }
                }
            });
        }

        return Admit(descriptor: Descriptor(), contribution: Contribution(facts)).IsAdmitted;
    }

    [Fact] void should_admit_an_array_of_arrays_view() => Admitted(true, [Array, Array]).ShouldBeTrue();
    [Fact] void should_admit_origins_without_a_request() => Admitted(false, [Array, Array]).ShouldBeTrue();
    [Fact] void should_reject_a_request_without_origins() => Admitted(true, null).ShouldBeFalse();
    [Fact] void should_reject_a_marked_list_of_lists() => Admitted(true, [Enumerable, Enumerable]).ShouldBeFalse();
    [Fact] void should_reject_a_marked_list_of_arrays() => Admitted(true, [Enumerable, Array]).ShouldBeFalse();
    [Fact] void should_reject_a_marked_array_of_lists() => Admitted(true, [Array, Enumerable]).ShouldBeFalse();
    [Fact] void should_reject_too_few_origins() => Admitted(true, [Array]).ShouldBeFalse();
    [Fact] void should_reject_too_many_origins() => Admitted(true, [Array, Array, Array]).ShouldBeFalse();
    [Fact] void should_reject_unknown_origins() => Admitted(true, [Array, TypeUseCollectionOriginKind.Unknown]).ShouldBeFalse();
    [Fact] void should_reject_undefined_origins() => Admitted(true, [Array, (TypeUseCollectionOriginKind)9]).ShouldBeFalse();
    [Fact] void should_reject_origins_misaligned_with_the_shape() => Admitted(false, [Array, Array], [TypeUseShapeKind.Collection, TypeUseShapeKind.Named]).ShouldBeFalse();
    [Fact] void should_reject_a_scalar_request() => Admitted(true, [], [TypeUseShapeKind.Named]).ShouldBeFalse();
    [Fact] void should_reject_an_empty_terminal_name() => Admitted(true, [Array, Array], name: string.Empty).ShouldBeFalse();
    [Fact] void should_reject_a_view_with_a_compatibility_projection() => Admitted(true, [Array, Array], projection: true).ShouldBeFalse();
}
