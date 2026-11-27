// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_a_jagged_array_member_requests_the_legacy_view : given.jagged_array_members
{
    GeneratedScreenplayDefinition[] _results = null!;

    void Because() => _results = GenerateBoth(true, ArrayOfArrays);

    [Fact] void should_not_report_an_unsupported_shape() => _results.Any(result => HasCode(result, GenerationDiagnosticCodes.UnsupportedTypeUseShape)).ShouldBeFalse();
    [Fact] void should_not_report_an_invalid_view() => _results.Any(result => HasCode(result, GenerationDiagnosticCodes.InvalidTypeUseCompatibilityProjection)).ShouldBeFalse();
    [Fact] void should_keep_the_event_in_the_graph() => _results.All(result => result.Graph.Artifacts.Any(artifact => artifact.Key.Kind == ArtifactKind.Event)).ShouldBeTrue();
    [Fact] void should_resolve_the_unnamed_collection_reference() => _results.All(result => Type(result).IsCollection).ShouldBeTrue();
    [Fact] void should_not_invent_a_target() => _results.All(result => Type(result).Subject is null).ShouldBeTrue();
    [Fact] void should_keep_the_unnamed_element_name_empty() => _results.All(result => Type(result).Name.Length == 0).ShouldBeTrue();

    static TypeReferenceDefinition Type(GeneratedScreenplayDefinition result) => result.Graph.Artifacts.Single(artifact => artifact.Key.Kind == ArtifactKind.Event).Variants.Single().Definition.Properties.Single().Type;
}
