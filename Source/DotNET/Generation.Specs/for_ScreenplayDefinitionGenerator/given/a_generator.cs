// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator.given;

public class a_generator : Specification
{
    protected readonly AdapterIdentity Adapter = new() { Id = "critter-stack", Version = "1.0.0" };
    protected ScreenplayDefinitionGenerator Generator = null!;

    void Establish() => Generator = new();

    protected GenerationFact[] Event(string name, string slice, params PropertyDefinition[] properties)
    {
        var subject = new SubjectId { Value = $"dotnet://Banking/Events.{name}" };
        var artifact = new ArtifactKey { Subject = subject, Kind = ArtifactKind.Event };
        var evidence = new Evidence
        {
            Adapter = Adapter,
            Strength = EvidenceStrength.Exact,
            Source = new SourceRange
            {
                Path = $"Accounts/{slice}/{name}.cs",
                StartLine = 1,
                StartColumn = 1,
                EndLine = 1,
                EndColumn = 1
            }
        };

        return
        [
            new ArtifactFact
            {
                Id = new FactId { Value = $"event:{name}" },
                Subject = subject,
                Definition = new ArtifactDefinition
                {
                    Key = artifact,
                    Name = name,
                    File = $"Accounts/{slice}/{name}.cs",
                    Properties = properties
                },
                Evidence = evidence
            },
            new ArtifactPlacementFact
            {
                Id = new FactId { Value = $"placement:{name}" },
                Subject = subject,
                Artifact = artifact,
                Placement = new ArtifactPlacement
                {
                    Module = "Accounts",
                    Features = ["Accounts"],
                    Slice = slice,
                    SliceKind = GenerationSliceKind.StateChange
                },
                Evidence = evidence
            }
        ];
    }

    protected (SubjectId Subject, GenerationFact[] Facts) Concept(
        string name,
        GenerationPrimitiveKind primitive,
        string? file = null)
    {
        var subject = new SubjectId { Value = $"dotnet://Banking/Concepts.{name}" };
        var evidence = new Evidence
        {
            Adapter = Adapter,
            Strength = EvidenceStrength.Exact,
            Source = new SourceRange
            {
                Path = file ?? $"Concepts/{name}.cs",
                StartLine = 1,
                StartColumn = 1,
                EndLine = 1,
                EndColumn = 1
            }
        };
        return (subject,
        [
            new ArtifactFact
            {
                Id = new FactId { Value = $"concept:{name}" },
                Subject = subject,
                Definition = new ArtifactDefinition
                {
                    Key = new ArtifactKey { Subject = subject, Kind = ArtifactKind.Concept },
                    Name = name,
                    File = file ?? $"Concepts/{name}.cs"
                },
                Evidence = evidence
            },
            new ConceptRepresentationFact
            {
                Id = new FactId { Value = $"concept-representation:{name}" },
                Subject = subject,
                Definition = new ConceptRepresentationDefinition
                {
                    Concept = subject,
                    Kind = ConceptRepresentationKind.Primitive,
                    Primitive = primitive
                },
                Evidence = evidence
            }
        ]);
    }

    protected static PropertyDefinition Property(string name, string type, SubjectId? subject = null) => new()
    {
        Name = name,
        Type = new TypeReferenceDefinition { Name = type, Subject = subject }
    };

    protected AdapterContribution Contribution(params GenerationFact[] facts) => new()
    {
        Adapter = Adapter,
        Facts = facts
    };
}
