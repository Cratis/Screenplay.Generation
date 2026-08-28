// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_an_exact_type_use_shape_cannot_be_lowered : given.a_generator
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var subject = new SubjectId { Value = "dotnet://Ordering/Events.CustomerRegistered" };
        var artifact = new ArtifactKey { Subject = subject, Kind = ArtifactKind.Event };
        var evidence = new Evidence { Adapter = Adapter, Strength = EvidenceStrength.Exact };
        var facts = new GenerationFact[]
        {
            new ArtifactDeclarationFact
            {
                Id = new FactId { Value = "critter-stack:declaration" },
                Subject = subject,
                Evidence = evidence,
                Definition = new ArtifactDeclarationDefinition
                {
                    Artifact = artifact,
                    Name = "CustomerRegistered"
                }
            },
            new ArtifactMemberDeclarationFact
            {
                Id = new FactId { Value = "critter-stack:member" },
                Subject = subject,
                Evidence = evidence,
                Definition = new ArtifactMemberDeclarationDefinition
                {
                    Member = new ArtifactMemberKey { Artifact = artifact, Name = "codes" },
                    DeclarationOrder = 0
                }
            },
            new ArtifactMemberTypeUseFact
            {
                Id = new FactId { Value = "critter-stack:type-use" },
                Subject = subject,
                Evidence = evidence,
                Definition = new ArtifactMemberTypeUseDefinition
                {
                    Member = new ArtifactMemberKey { Artifact = artifact, Name = "codes" },
                    Type = new TypeUseDefinition
                    {
                        Name = "String",
                        Shape =
                        [
                            TypeUseShapeKind.Collection,
                            TypeUseShapeKind.Optional,
                            TypeUseShapeKind.Named
                        ]
                    }
                }
            },
            new ArtifactPlacementFact
            {
                Id = new FactId { Value = "critter-stack:placement" },
                Subject = subject,
                Evidence = evidence,
                Artifact = artifact,
                Placement = new ArtifactPlacement
                {
                    Module = "Customers",
                    Slice = "Register",
                    SliceKind = GenerationSliceKind.StateChange
                }
            }
        };

        _result = Generator.Generate(
            Snapshot(Completed(Adapter, facts)),
            new ScreenplayGenerationOptions { Domain = "Ordering" });
    }

    [Fact] void should_fail_closed() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_exact_shape_as_unsupported() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(GenerationDiagnosticCodes.UnsupportedTypeUseShape);
    [Fact] void should_omit_the_incomplete_granular_only_artifact_atomically() => _result.Graph.Artifacts.Any(artifact => artifact.Key.Kind == ArtifactKind.Event).ShouldBeFalse();
    [Fact] void should_not_emit_the_unsupported_member() => _result.Source.ShouldNotContain("codes String");
    [Fact] void should_omit_the_type_use_with_its_diagnostic() => TypeUseRecord().Disposition.ShouldEqual(GenerationFactDisposition.OmittedWithDiagnostic);
    [Fact] void should_associate_the_shape_diagnostic_with_the_type_use() => TypeUseRecord().Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(GenerationDiagnosticCodes.UnsupportedTypeUseShape);

    GenerationFactRecord TypeUseRecord() => _result.AdapterRun!.Facts.Single(record => record.Fact.Id.Value == "critter-stack:type-use");
}
