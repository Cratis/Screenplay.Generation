// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator.given;

public class backend_scope_diagnostic_sources : a_lowering_fixture
{
    protected static readonly BackendScopeKey First = new() { Store = SubjectOf("Stores.FirstRegistration"), EventModelName = "Shared" };
    protected static readonly BackendScopeKey Second = new() { Store = SubjectOf("Stores.SecondRegistration"), EventModelName = "Shared" };

    protected Evidence At(int line) => EvidenceAt("Stores/Configuration.cs", line) with
    {
        Source = Range("Stores/Configuration.cs", line) with
        {
            FileIdentity = new SourceFileIdentity { Project = "Banking/Banking", Path = "Stores/Configuration.cs" },
            StartColumn = 9,
            EndColumn = 135
        }
    };

    protected BackendScopeFact Declaration(BackendScopeKey key, string id, int line = 5) => new()
    {
        Id = new FactId { Value = id },
        Subject = key.Store,
        Evidence = At(line),
        Definition = new BackendScopeDefinition { Key = key, Name = "Shared store" }
    };

    protected GenerationFact[] Members(BackendScopeKey scope, string id = "event")
    {
        var artifact = new ArtifactKey { Subject = SubjectOf("Events.Opened"), Kind = ArtifactKind.Event, Scope = scope };
        var member = new ArtifactMemberKey { Artifact = artifact, Name = "Name" };
        return
        [
            new ArtifactDeclarationFact
            {
                Id = new FactId { Value = $"{id}:artifact" },
                Subject = artifact.Subject,
                Evidence = At(3),
                Definition = new ArtifactDeclarationDefinition { Artifact = artifact, Name = "Opened" }
            },
            new ArtifactMemberDeclarationFact
            {
                Id = new FactId { Value = $"{id}:member" },
                Subject = artifact.Subject,
                Evidence = At(2),
                Definition = new ArtifactMemberDeclarationDefinition { Member = member, DeclarationOrder = 0 }
            },
            new ArtifactMemberTypeUseFact
            {
                Id = new FactId { Value = $"{id}:type" },
                Subject = artifact.Subject,
                Evidence = At(2),
                Definition = new ArtifactMemberTypeUseDefinition
                {
                    Member = member,
                    Type = new TypeUseDefinition { Name = "String", Shape = [TypeUseShapeKind.Named] }
                }
            }
        ];
    }

    protected GeneratedScreenplayDefinition[] GenerateOrders(GenerationFact[] declarations, GenerationFact[] references) =>
    [
        Generator.Generate([Contribution(declarations), Contribution(references)], new ScreenplayGenerationOptions { Domain = "Banking" }),
        Generator.Generate([Contribution([.. references.Reverse()]), Contribution([.. declarations.Reverse()])], new ScreenplayGenerationOptions { Domain = "Banking" })
    ];

    protected static GenerationDiagnostic[] ScopeWarnings(GeneratedScreenplayDefinition result) =>
        [.. result.Diagnostics.Where(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.BackendScopeNotSupported)];
}
