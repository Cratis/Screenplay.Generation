// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_a_yield_marker_identifies_another_declaration : given.yielding_concepts
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var candidate = Candidate("enum", true);
        var declaration = candidate.OfType<ArtifactDeclarationFact>().Single();
        var marker = declaration.Definition.NameConflictYield!;
        var foreign = declaration with
        {
            Definition = declaration.Definition with
            {
                NameConflictYield = marker with { Diagnostic = marker.Diagnostic with { Facts = [Id("concept-vogen")] } }
            }
        };
        GenerationFact[] facts = [.. Command(), .. candidate.Where(fact => fact != declaration), foreign, .. Candidate("vogen", false, true)];
        _result = Generator.Generate([Contribution(facts)], new ScreenplayGenerationOptions { Domain = "Ordering" });
    }

    [Fact] void should_keep_the_concept() => _result.Graph.Artifacts.Any(artifact => artifact.Key.Subject == Target).ShouldBeTrue();
    [Fact] void should_retain_the_name_conflict() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(GenerationDiagnosticCodes.ConflictingConceptName);
    [Fact] void should_not_publish_the_foreign_yield_diagnostic() => _result.Diagnostics.Any(IsWarning).ShouldBeFalse();
    [Fact] void should_not_unbind_the_command() => Member(_result).Type.Subject.ShouldEqual(Target);
}
