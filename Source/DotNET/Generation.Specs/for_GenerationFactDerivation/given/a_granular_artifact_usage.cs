// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation.given;

public class a_granular_artifact_usage : an_artifact_usage
{
    protected static GenerationFact[] EndpointFacts(ArtifactKey key, string name, bool conflict = false)
    {
        var member = new ArtifactMemberKey { Artifact = key, Name = "id" };
        return
        [
            Legacy("empty", key, name, []),
            Legacy("populated", key, name, conflict ? [Property("id"), Property("legacy")] : [Property("id")]),
            new ArtifactMemberDeclarationFact
            {
                Id = new FactId { Value = $"application:{name}-member" }, Subject = key.Subject, Evidence = UsageEvidence(),
                Definition = new ArtifactMemberDeclarationDefinition { Member = member, DeclarationOrder = 0 }
            },
            new ArtifactMemberTypeUseFact
            {
                Id = new FactId { Value = $"application:{name}-type" }, Subject = key.Subject, Evidence = UsageEvidence(),
                Definition = new ArtifactMemberTypeUseDefinition
                {
                    Member = member,
                    Type = new TypeUseDefinition { Name = "String", Shape = [TypeUseShapeKind.Named] }
                }
            }
        ];
    }

    static PropertyDefinition Property(string name) => new() { Name = name, Type = new TypeReferenceDefinition { Name = "String" } };

    static ArtifactFact Legacy(string id, ArtifactKey key, string name, PropertyDefinition[] properties) => new()
    {
        Id = new FactId { Value = $"application:{name}-{id}" }, Subject = key.Subject, Evidence = UsageEvidence(),
        Definition = new ArtifactDefinition { Key = key, Name = name, Properties = properties }
    };
}
