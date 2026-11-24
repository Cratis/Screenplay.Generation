// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator.given;

public class artifact_diagnostic_sources : a_lowering_fixture
{
    protected static readonly ArtifactKey Document = new()
    {
        Subject = new SubjectId { Value = "dotnet://Students/Students/Students.Student" },
        Kind = ArtifactKind.Document
    };

    protected Evidence At(int line, int column, int endColumn) => EvidenceAt("Students/Student.cs", line) with
    {
        Source = Range("Students/Student.cs", line) with
        {
            FileIdentity = new SourceFileIdentity { Project = "Students/Students", Path = "Student.cs" },
            StartColumn = column,
            EndColumn = endColumn
        }
    };

    protected ArtifactDeclarationFact Declaration(string id, ArtifactKey key, int line = 5) => new()
    {
        Id = new FactId { Value = id },
        Subject = key.Subject,
        Evidence = At(line, 83, 105),
        Definition = new ArtifactDeclarationDefinition { Artifact = key, Name = "Student" }
    };

    protected GenerationFact[] Members(string id, ArtifactKey key) =>
    [
        .. Member(id, key, ("Id", 0, "Int32"), At(2, 30, 36)),
        .. Member(id, key, ("Name", 1, "String"), At(2, 38, 49))
    ];

    static GenerationFact[] Member(string id, ArtifactKey key, (string Name, int Order, string Type) property, Evidence evidence)
    {
        var member = new ArtifactMemberKey { Artifact = key, Name = property.Name };
        return
        [
            new ArtifactMemberDeclarationFact
            {
                Id = new FactId { Value = $"{id}:{property.Name}:member" },
                Subject = key.Subject,
                Evidence = evidence,
                Definition = new ArtifactMemberDeclarationDefinition { Member = member, DeclarationOrder = property.Order }
            },
            new ArtifactMemberTypeUseFact
            {
                Id = new FactId { Value = $"{id}:{property.Name}:type" },
                Subject = key.Subject,
                Evidence = evidence,
                Definition = new ArtifactMemberTypeUseDefinition
                {
                    Member = member,
                    Type = new TypeUseDefinition { Name = property.Type, Shape = [TypeUseShapeKind.Named] }
                }
            }
        ];
    }

    protected ArtifactPlacementFact Placement(ArtifactKey key) => new()
    {
        Id = new FactId { Value = "placement" },
        Subject = key.Subject,
        Evidence = At(5, 83, 105),
        Artifact = key,
        Placement = new ArtifactPlacement { Module = "Students", Slice = "Store", SliceKind = GenerationSliceKind.StateChange }
    };

    protected GeneratedScreenplayDefinition Generate(params AdapterContribution[] contributions) =>
        Generator.Generate(contributions, new ScreenplayGenerationOptions { Domain = "Banking" });

    protected GeneratedScreenplayDefinition GenerateSnapshot(params GenerationFact[] facts) =>
        Generator.Generate(Snapshot(Completed(Adapter, facts)), new ScreenplayGenerationOptions { Domain = "Banking" });

    protected static GenerationDiagnostic[] Omissions(GeneratedScreenplayDefinition result) =>
        [.. result.Diagnostics.Where(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.UnsupportedArtifact || diagnostic.Code == GenerationDiagnosticCodes.IncompleteArtifact)];
}
