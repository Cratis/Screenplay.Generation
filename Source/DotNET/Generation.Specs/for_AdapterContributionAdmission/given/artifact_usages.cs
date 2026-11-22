// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission.given;

public class artifact_usages : a_contribution
{
    protected static readonly ArtifactKey CommandArtifact = new() { Subject = ArtifactSubject, Kind = ArtifactKind.Command };

    protected static ArtifactUsageDeclarationFact Declaration() => new()
    {
        Id = Id("usage-declaration"),
        Subject = ArtifactSubject,
        Evidence = Evidence(),
        Definition = new ArtifactUsageDeclarationDefinition { Declaration = ArtifactSubject, Artifact = CommandArtifact }
    };

    protected static EntryPointFact Entry() => new()
    {
        Id = Id("entry"),
        Subject = ArtifactSubject,
        Evidence = Evidence(),
        Definition = new EntryPointDefinition { Method = ArtifactSubject, Artifact = CommandArtifact }
    };

    protected static MethodArtifactUsageFact Usage() => new()
    {
        Id = Id("usage"),
        Subject = ArtifactSubject,
        Evidence = Evidence(),
        Definition = new MethodArtifactUsageDefinition { Method = ArtifactSubject, Declaration = ArtifactSubject, Kind = RelationshipKind.Reads }
    };
}
