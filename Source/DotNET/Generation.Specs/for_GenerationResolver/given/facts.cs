// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver.given;

public class facts : Specification
{
    protected static readonly AdapterIdentity FirstAdapter = new() { Id = "first", Version = "1.0.0" };
    protected static readonly AdapterIdentity SecondAdapter = new() { Id = "second", Version = "1.0.0" };
    protected static readonly SubjectId EventSubject = new() { Value = "dotnet://Banking/Events.AccountOpened" };
    protected static readonly SubjectId CommandSubject = new() { Value = "dotnet://Banking/Commands.OpenAccount" };

    protected static ArtifactDefinition EventDefinition(string name = "AccountOpened") => new()
    {
        Key = new ArtifactKey { Subject = EventSubject, Kind = ArtifactKind.Event },
        Name = name,
        File = "Accounts/Open/AccountOpened.cs",
        Properties =
        [
            new PropertyDefinition
            {
                Name = "accountId",
                Type = new TypeReferenceDefinition { Name = "Uuid" }
            }
        ]
    };

    protected static ArtifactFact Fact(
        string id,
        AdapterIdentity adapter,
        ArtifactDefinition? definition = null) => new()
    {
        Id = new FactId { Value = id },
        Subject = EventSubject,
        Definition = definition ?? EventDefinition(),
        Evidence = new Evidence
        {
            Adapter = adapter,
            Strength = EvidenceStrength.Exact,
            Source = new SourceRange
            {
                Path = "Accounts/Open/AccountOpened.cs",
                StartLine = 10,
                StartColumn = 1,
                EndLine = 10,
                EndColumn = 30
            }
        }
    };

    protected static RelationshipFact Relationship(
        string id,
        AdapterIdentity adapter,
        string? sourceMember = null) => new()
    {
        Id = new FactId { Value = id },
        Subject = CommandSubject,
        Definition = new RelationshipDefinition
        {
            Key = new RelationshipKey
            {
                Kind = RelationshipKind.Produces,
                Source = CommandSubject,
                Target = EventSubject
            },
            SourceMember = sourceMember
        },
        Evidence = new Evidence
        {
            Adapter = adapter,
            Strength = EvidenceStrength.Exact,
            Source = new SourceRange
            {
                Path = "Accounts/Open/OpenAccount.cs",
                StartLine = 20,
                StartColumn = 1,
                EndLine = 20,
                EndColumn = 30
            }
        }
    };

    protected static AdapterContribution Contribution(AdapterIdentity adapter, params GenerationFact[] contributedFacts) => new()
    {
        Adapter = adapter,
        Facts = contributedFacts
    };
}
