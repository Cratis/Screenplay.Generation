// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator.given;

public class compatibility_type_uses : concept_only_type_uses
{
    protected GenerationFact[] ProjectedCommand(SubjectId? intermediate = null, string intermediateName = "List", bool conceptOnly = false)
    {
        var facts = Command(TypeUseShapeKind.Collection, TypeUseShapeKind.Collection, TypeUseShapeKind.Named);
        var original = facts.OfType<ArtifactMemberTypeUseFact>().Single();
        var observation = original with
        {
            Definition = original.Definition with
            {
                Type = original.Definition.Type with
                {
                    Nodes = [new() { Name = "List" }, new() { Name = intermediateName, Subject = intermediate }, new() { Name = original.Definition.Type.Name, Subject = original.Definition.Type.ObservedTypeSubject }]
                }
            }
        };
        return [.. facts.Where(fact => fact is not ArtifactMemberTypeUseFact), observation, View(observation, intermediate, intermediateName), .. conceptOnly ? new[] { Policy(observation) } : []];
    }

    protected TypeUseCompatibilityProjectionFact View(ArtifactMemberTypeUseFact observation, SubjectId? intermediate = null, string name = "List") => new()
    {
        Id = new() { Value = $"{observation.Id.Value}:view" }, Subject = observation.Subject, Evidence = observation.Evidence,
        Definition = new()
        {
            TypeUse = observation.Id, Profile = TypeUseCompatibilityProfile.OneLayerCollection, Version = 1, SourceNodeIndex = 1,
            Type = new() { Name = name, ObservedTypeSubject = intermediate, Shape = [TypeUseShapeKind.Collection, TypeUseShapeKind.Named] }
        }
    };
}
