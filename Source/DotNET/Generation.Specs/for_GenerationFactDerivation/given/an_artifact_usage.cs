// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation.given;

public class an_artifact_usage : a_derivation
{
    protected static readonly SubjectId Method = new() { Value = "dotnet://Application/Api.Find``1(System.String)" };
    protected static readonly SubjectId Declaration = new() { Value = "dotnet://Library/Plans.FindCustomer``1" };
    protected static readonly ArtifactKey Query = new() { Subject = new SubjectId { Value = "dotnet://Application/Api.Find" }, Kind = ArtifactKind.Command };
    protected static readonly ArtifactKey Document = new() { Subject = new SubjectId { Value = "dotnet://Library/Models.Customer" }, Kind = ArtifactKind.ReadModel };

    protected static GenerationFact[] CompleteFacts() => [Artifact(Query, "query", "FindCustomer"), Artifact(Document, "document", "Customer"), Entry(), UsageDeclaration(), Usage()];

    protected static ArtifactDeclarationFact Artifact(ArtifactKey key, string id, string name) => new()
    {
        Id = new FactId { Value = $"application:{id}" },
        Subject = key.Subject,
        Evidence = UsageEvidence(),
        Definition = new ArtifactDeclarationDefinition { Artifact = key, Name = name }
    };

    protected static EntryPointFact Entry(string id = "entry", SubjectId? method = null, ArtifactKey? artifact = null) => new()
    {
        Id = new FactId { Value = $"application:{id}" },
        Subject = method ?? Method,
        Evidence = UsageEvidence(),
        Definition = new EntryPointDefinition { Method = method ?? Method, Artifact = artifact ?? Query }
    };

    protected static ArtifactUsageDeclarationFact UsageDeclaration(string id = "declaration", ArtifactKey? target = null) => new()
    {
        Id = new FactId { Value = $"library:{id}" },
        Subject = Declaration,
        Evidence = UsageEvidence(ConceptAdapter),
        Definition = new ArtifactUsageDeclarationDefinition { Declaration = Declaration, Artifact = target ?? Document }
    };

    protected static MethodArtifactUsageFact Usage(string id = "usage", RelationshipKind kind = RelationshipKind.Reads, SubjectId? method = null) => new()
    {
        Id = new FactId { Value = $"library:{id}" },
        Subject = method ?? Method,
        Evidence = UsageEvidence(ConceptAdapter),
        Definition = new MethodArtifactUsageDefinition { Method = method ?? Method, Declaration = Declaration, Kind = kind }
    };

    protected static Evidence UsageEvidence(AdapterIdentity? adapter = null) => new()
    {
        Adapter = adapter ?? ApplicationAdapter,
        Strength = EvidenceStrength.Exact,
        Source = new SourceRange { Path = "Queries/FindCustomer.cs", StartLine = 2, StartColumn = 1, EndLine = 2, EndColumn = 20 }
    };
}
