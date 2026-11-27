// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_a_jagged_array_member_requests_the_legacy_view : given.jagged_array_members
{
    GeneratedScreenplayDefinition _result = null!;

    void Because() => _result = Generate(true, ArrayOfArrays);

    [Fact] void should_not_report_an_unsupported_shape() => HasCode(_result, GenerationDiagnosticCodes.UnsupportedTypeUseShape).ShouldBeFalse();
    [Fact] void should_not_report_an_invalid_view() => HasCode(_result, GenerationDiagnosticCodes.InvalidTypeUseCompatibilityProjection).ShouldBeFalse();
    [Fact] void should_keep_the_event_in_the_graph() => _result.Graph.Artifacts.Any(artifact => artifact.Key.Kind == ArtifactKind.Event).ShouldBeTrue();
    [Fact] void should_resolve_the_unnamed_collection_reference() => Type().IsCollection.ShouldBeTrue();
    [Fact] void should_not_invent_a_target() => Type().Subject.ShouldBeNull();
    [Fact] void should_keep_the_unnamed_element_name_empty() => Type().Name.ShouldEqual(string.Empty);

    TypeReferenceDefinition Type() => _result.Graph.Artifacts.Single(artifact => artifact.Key.Kind == ArtifactKind.Event).Variants.Single().Definition.Properties.Single().Type;
}
