// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_an_observed_type_target_is_missing_without_an_omission : given.an_omitted_type_use
{
    GeneratedScreenplayDefinition _result = null!;

    void Because() => _result = Generate(Facts().Where(fact => fact is not TypeUseTargetOmittedFact));

    [Fact] void should_keep_the_missing_target_error() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.MissingTypeUseTarget).ShouldBeTrue();
    [Fact] void should_keep_the_incomplete_member_error() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.IncompleteArtifactMember).ShouldBeTrue();
    [Fact] void should_not_generate_successfully() => _result.IsSuccess.ShouldBeFalse();
}
