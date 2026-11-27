// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_a_jagged_array_member_is_blocked_by_invalid_evidence : given.jagged_array_members
{
    const TypeUseCollectionOriginKind Enumerable = TypeUseCollectionOriginKind.Enumerable;

    GeneratedScreenplayDefinition[] _results = null!;
    GeneratedScreenplayDefinition[] _targeted = null!;

    void Because()
    {
        _results =
        [
            .. GenerateBoth(true, [Enumerable, Enumerable]),
            .. GenerateBoth(true, null),
            .. GenerateBoth(true, ArrayOfArrays, projection: true)
        ];
        _targeted = GenerateBoth(Targeted(MemberFacts(true, ArrayOfArrays)));
    }

    [Fact] void should_report_the_invalid_view_on_every_path() => _results
        .All(result => HasCode(result, GenerationDiagnosticCodes.InvalidTypeUseCompatibilityProjection)).ShouldBeTrue();

    [Fact] void should_omit_the_owning_artifact_despite_the_legacy_property() => _results
        .Any(result => result.Graph.Artifacts.Any(artifact => artifact.Key.Kind == ArtifactKind.Event)).ShouldBeFalse();

    [Fact] void should_reject_a_targeted_legacy_property_for_a_view() => _targeted
        .All(result => HasCode(result, GenerationDiagnosticCodes.InvalidTypeUseCompatibilityProjection)).ShouldBeTrue();

    [Fact] void should_omit_the_artifact_of_a_targeted_view() => _targeted
        .Any(result => result.Graph.Artifacts.Any(artifact => artifact.Key.Kind == ArtifactKind.Event)).ShouldBeFalse();

    static GenerationFact[] Targeted(GenerationFact[] facts) =>
    [
        .. facts.Select(fact => fact is ArtifactFact legacy
            ? legacy with
            {
                Definition = legacy.Definition with
                {
                    Properties = [.. legacy.Definition.Properties.Select(property => property with
                    {
                        Type = property.Type with
                        {
                            Subject = new SubjectId { Value = "dotnet://Ordering/Events.Part" },
                            TargetArtifactKind = ArtifactKind.Event
                        }
                    })]
                }
            }
            : fact)
    ];
}
