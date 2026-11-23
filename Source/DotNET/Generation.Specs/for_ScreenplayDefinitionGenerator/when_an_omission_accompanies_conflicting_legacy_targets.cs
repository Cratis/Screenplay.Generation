// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_an_omission_accompanies_conflicting_legacy_targets : given.an_omitted_type_use
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var facts = Facts();
        var declaration = facts.OfType<ArtifactDeclarationFact>().Single();
        var first = LegacyArtifact(declaration, ArtifactKind.Concept, "legacy-concept");
        var second = LegacyArtifact(declaration, ArtifactKind.ReadModel, "legacy-read-model");
        _result = Generate([.. facts, first, second]);
    }

    [Fact] void should_check_legacy_targets_before_waiving_the_missing_target() => _result.AdapterRun.Derivation.Diagnostics.Single(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.ConflictingTypeUseTarget).Message.ShouldContain("incompatible exact legacy target roles");
    [Fact] void should_not_report_the_omission_warning() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == "example.FlagsEnumOmitted").ShouldBeFalse();
    [Fact] void should_not_generate_successfully() => _result.IsSuccess.ShouldBeFalse();

    ArtifactFact LegacyArtifact(ArtifactDeclarationFact declaration, ArtifactKind kind, string id) => new()
    {
        Id = Id(id), Subject = declaration.Subject, Evidence = declaration.Evidence,
        Definition = new ArtifactDefinition
        {
            Key = declaration.Definition.Artifact, Name = declaration.Definition.Name,
            Properties =
            [
                new PropertyDefinition
                {
                    Name = "permissions",
                    Type = new TypeReferenceDefinition { Name = "Int", Subject = Target, TargetArtifactKind = kind }
                }
            ]
        }
    };
}
