// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_a_yielding_concept_has_an_unsupported_member_shape : given.yielding_concepts
{
    GeneratedScreenplayDefinition _result = null!;

    void Because() => _result = Generate([.. Command(TypeUseShapeKind.Collection, TypeUseShapeKind.Optional, TypeUseShapeKind.Named), .. Candidate("enum", true), .. Candidate("vogen", false, true)]);

    [Fact] void should_not_hide_the_unsupported_shape() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(GenerationDiagnosticCodes.UnsupportedTypeUseShape);
    [Fact] void should_omit_the_unsupported_command() => _result.Graph.Artifacts.Any(artifact => artifact.Key.Kind == ArtifactKind.Command).ShouldBeFalse();
}
