// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator.given;

public class an_omitted_type_use : a_generator
{
    protected static readonly SubjectId Target = new() { Value = "dotnet://Ordering/Permissions" };

    protected GenerationFact[] Facts(params TypeUseShapeKind[] shape)
    {
        var owner = new ArtifactKey { Subject = new SubjectId { Value = "dotnet://Ordering/SetPermissions" }, Kind = ArtifactKind.Command };
        var member = new ArtifactMemberKey { Artifact = owner, Name = "permissions" };
        var evidence = new Evidence
        {
            Adapter = Adapter,
            Strength = EvidenceStrength.Exact,
            Source = new SourceRange { Path = "SetPermissions.cs", StartLine = 3, StartColumn = 1, EndLine = 3, EndColumn = 60 }
        };
        return
        [
            new ArtifactDeclarationFact
            {
                Id = Id("command"), Subject = owner.Subject, Evidence = evidence,
                Definition = new ArtifactDeclarationDefinition { Artifact = owner, Name = "SetPermissions" }
            },
            new ArtifactPlacementFact
            {
                Id = Id("placement"), Subject = owner.Subject, Evidence = evidence, Artifact = owner,
                Placement = new ArtifactPlacement { Module = "Ordering", Features = ["Access"], Slice = "Setting", SliceKind = GenerationSliceKind.StateChange }
            },
            new ArtifactMemberDeclarationFact
            {
                Id = Id("member"), Subject = owner.Subject, Evidence = evidence,
                Definition = new ArtifactMemberDeclarationDefinition { Member = member, DeclarationOrder = 0 }
            },
            new ArtifactMemberTypeUseFact
            {
                Id = Id("type-use"), Subject = owner.Subject, Evidence = evidence,
                Definition = new ArtifactMemberTypeUseDefinition
                {
                    Member = member,
                    Type = new TypeUseDefinition { Name = "Int", ObservedTypeSubject = Target, Shape = shape.Length == 0 ? [TypeUseShapeKind.Named] : shape }
                }
            },
            new TypeUseTargetOmittedFact
            {
                Id = Id("omission"), Subject = owner.Subject, Evidence = evidence,
                Definition = new TypeUseTargetOmittedDefinition
                {
                    Member = member,
                    Diagnostic = new GenerationDiagnostic
                    {
                        Code = "example.FlagsEnumOmitted", Severity = GenerationDiagnosticSeverity.Warning,
                        Message = "The observed type has no contributed target", Subject = owner.Subject, Source = evidence.Source,
                        Facts = [Id("type-use")]
                    }
                }
            }
        ];
    }

    protected GeneratedScreenplayDefinition Generate(IEnumerable<GenerationFact> facts) => Generator.Generate(
        Snapshot(Completed(Adapter, [.. facts])), new ScreenplayGenerationOptions { Domain = "Ordering" });

    protected ArtifactFact DeclaredTarget(GenerationFact basis) => new()
    {
        Id = Id("target"), Subject = Target, Evidence = basis.Evidence,
        Definition = new ArtifactDefinition { Key = new ArtifactKey { Subject = Target, Kind = ArtifactKind.Concept }, Name = "Permissions" }
    };

    protected FactId Id(string suffix) => new() { Value = $"{Adapter.Id}:{suffix}" };
}
