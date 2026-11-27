// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator.given;

public class jagged_array_members : a_generator
{
    protected GeneratedScreenplayDefinition[] GenerateBoth(
        bool requested,
        IReadOnlyList<TypeUseCollectionOriginKind>? origins,
        IReadOnlyList<TypeUseShapeKind>? shape = null,
        bool projection = false,
        string name = "Int") => GenerateBoth(MemberFacts(requested, origins, shape, projection, name));

    protected GeneratedScreenplayDefinition[] GenerateBoth(GenerationFact[] facts) =>
    [
        Generator.Generate(Snapshot(Completed(Adapter, facts)), new ScreenplayGenerationOptions { Domain = "Ordering" }),
        GenerateFromContributions(facts)
    ];

    protected GenerationFact[] MemberFacts(
        bool requested,
        IReadOnlyList<TypeUseCollectionOriginKind>? origins,
        IReadOnlyList<TypeUseShapeKind>? shape = null,
        bool projection = false,
        string name = "Int",
        SubjectId? observed = null)
    {
        var subject = new SubjectId { Value = "dotnet://Ordering/Events.GridSaved" };
        var artifact = new ArtifactKey { Subject = subject, Kind = ArtifactKind.Event };
        var member = new ArtifactMemberKey { Artifact = artifact, Name = "jagged" };
        var evidence = new Evidence { Adapter = Adapter, Strength = EvidenceStrength.Exact };
        var type = new TypeUseDefinition { Name = name, ObservedTypeSubject = observed, Shape = shape ?? [TypeUseShapeKind.Collection, TypeUseShapeKind.Collection, TypeUseShapeKind.Named] };
        List<GenerationFact> facts =
        [
            new ArtifactFact
            {
                Id = new FactId { Value = "critter-stack:legacy-artifact" },
                Subject = subject,
                Evidence = evidence,
                Definition = new ArtifactDefinition
                {
                    Key = artifact,
                    Name = "GridSaved",
                    Properties = [new PropertyDefinition { Name = "jagged", Type = new TypeReferenceDefinition { Name = string.Empty, IsCollection = true } }]
                }
            },
            new ArtifactMemberDeclarationFact
            {
                Id = new FactId { Value = "critter-stack:member" },
                Subject = subject,
                Evidence = evidence,
                Definition = new ArtifactMemberDeclarationDefinition { Member = member, DeclarationOrder = 0 }
            },
            new ArtifactMemberTypeUseFact
            {
                Id = new FactId { Value = "critter-stack:type-use" },
                Subject = subject,
                Evidence = evidence,
                Definition = new ArtifactMemberTypeUseDefinition
                {
                    Member = member,
                    Type = type,
                    UseLegacyJaggedArrayReference = requested,
                    CollectionOrigins = origins
                }
            },
            new ArtifactPlacementFact
            {
                Id = new FactId { Value = "critter-stack:placement" },
                Subject = subject,
                Evidence = evidence,
                Artifact = artifact,
                Placement = new ArtifactPlacement { Module = "Customers", Slice = "Register", SliceKind = GenerationSliceKind.StateChange }
            }
        ];
        if (projection)
        {
            facts.Add(Projection(subject, evidence));
        }

        return [.. facts];
    }

    protected GeneratedScreenplayDefinition GenerateFromContributions(params GenerationFact[] facts) =>
        Generator.Generate([Contribution(facts)], new ScreenplayGenerationOptions { Domain = "Ordering" });

    static TypeUseCompatibilityProjectionFact Projection(SubjectId subject, Evidence evidence) => new()
    {
        Id = new FactId { Value = "critter-stack:projection" },
        Subject = subject,
        Evidence = evidence,
        Definition = new TypeUseCompatibilityProjectionDefinition
        {
            TypeUse = new FactId { Value = "critter-stack:type-use" },
            Profile = TypeUseCompatibilityProfile.OneLayerCollection,
            Version = 1,
            SourceNodeIndex = 1,
            Type = new TypeUseDefinition { Name = "Int", Shape = [TypeUseShapeKind.Collection, TypeUseShapeKind.Named] }
        }
    };

    protected static TypeUseCollectionOriginKind[] ArrayOfArrays => [TypeUseCollectionOriginKind.Array, TypeUseCollectionOriginKind.Array];

    protected static bool HasCode(GeneratedScreenplayDefinition result, string code) => result.Diagnostics.Any(diagnostic => diagnostic.Code == code);
}
