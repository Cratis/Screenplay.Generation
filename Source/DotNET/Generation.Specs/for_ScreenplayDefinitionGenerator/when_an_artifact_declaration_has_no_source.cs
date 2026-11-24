// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_an_artifact_declaration_has_no_source : given.artifact_diagnostic_sources
{
    GeneratedScreenplayDefinition _result;
    GeneratedScreenplayDefinition _reversed;

    void Because()
    {
        var declaration = Declaration("document", Document);
        declaration = declaration with { Evidence = declaration.Evidence with { Source = null } };
        GenerationFact[] facts = [declaration, .. Members("document", Document)];
        _result = Generate(Contribution(facts));
        _reversed = Generate(Contribution([.. facts.Reverse()]));
    }

    [Fact] void should_fall_back_to_the_first_member_source() => Omissions(_result).Single().Source.ShouldEqual(At(2, 30, 36).Source);
    [Fact] void should_keep_fallback_order_independent() => Omissions(_reversed).Single().Source.ShouldEqual(Omissions(_result).Single().Source);
    [Fact] void should_still_omit_the_unsupported_artifact() => Omissions(_result).Single().Code.ShouldEqual(GenerationDiagnosticCodes.UnsupportedArtifact);
}
