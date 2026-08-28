// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_a_malformed_type_use_shape_bypasses_admission : given.a_generator
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var subject = new SubjectId { Value = "dotnet://Ordering/Events.CustomerRegistered" };
        var artifact = new ArtifactKey { Subject = subject, Kind = ArtifactKind.Event };
        var evidence = new Evidence { Adapter = Adapter, Strength = EvidenceStrength.Exact };
        var facts = new GenerationFact[]
        {
            new ArtifactFact
            {
                Id = new FactId { Value = "event:legacy" },
                Subject = subject,
                Evidence = evidence,
                Definition = new ArtifactDefinition
                {
                    Key = artifact,
                    Name = "CustomerRegistered",
                    Properties =
                    [
                        new PropertyDefinition
                        {
                            Name = "customerCode",
                            Type = new TypeReferenceDefinition { Name = "String" }
                        }
                    ]
                }
            },
            new ArtifactMemberTypeUseFact
            {
                Id = new FactId { Value = "event:type-use" },
                Subject = subject,
                Evidence = evidence,
                Definition = new ArtifactMemberTypeUseDefinition
                {
                    Member = new ArtifactMemberKey { Artifact = artifact, Name = "customerCode" },
                    Type = new TypeUseDefinition { Name = "String", Shape = [] }
                }
            },
            new ArtifactPlacementFact
            {
                Id = new FactId { Value = "event:placement" },
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
            [new AdapterContribution { Adapter = Adapter, Facts = facts }],
            new ScreenplayGenerationOptions { Domain = "Ordering" });
    }

    [Fact] void should_return_a_deterministic_error_instead_of_throwing() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(GenerationDiagnosticCodes.UnsupportedTypeUseShape);
    [Fact] void should_type_the_missing_shape_as_unknown() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.UnsupportedTypeUseShape).Outcome.ShouldEqual(GenerationDiagnosticOutcome.Unknown);
    [Fact] void should_omit_the_malformed_type_use() => _result.Graph.Artifacts.Any(artifact => artifact.Key.Kind == ArtifactKind.Event).ShouldBeFalse();
    [Fact] void should_not_emit_partial_source() => _result.Source.ShouldNotContain("event CustomerRegistered");
}
