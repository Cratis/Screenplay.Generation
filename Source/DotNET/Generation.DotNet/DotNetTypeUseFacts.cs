// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Screenplay.Generation.DotNet;

/// <summary>
/// Emits source-neutral member declarations, exact type uses, and optional roles from Roslyn properties.
/// </summary>
public static class DotNetTypeUseFacts
{
    /// <summary>
    /// Emits granular facts for every public readable instance property declared by a source type.
    /// </summary>
    /// <param name="type">The exact source type declaring the properties.</param>
    /// <param name="artifact">The artifact role that owns the members.</param>
    /// <param name="context">The fixed analysis context used only to establish exact source type subjects.</param>
    /// <param name="evidence">The evidence establishing the declared member surface.</param>
    /// <param name="roleFor">An optional source-framework rule that establishes a semantic role for a property.</param>
    /// <returns>Member declaration and type-use facts, followed by an optional role fact for each property.</returns>
    public static IReadOnlyList<GenerationFact> Emit(
        INamedTypeSymbol type,
        ArtifactKey artifact,
        DotNetAnalysisContext context,
        Evidence evidence,
        Func<IPropertySymbol, ArtifactMemberRoleKind?>? roleFor = null)
    {
        var facts = new List<GenerationFact>();
        var properties = DotNetTypeShapes.PublicReadablePropertiesOf(type);
        for (var order = 0; order < properties.Count; order++)
        {
            var property = properties[order];
            var name = DotNetTypeShapes.PropertyName(property.Name);
            var member = new ArtifactMemberKey
            {
                Artifact = artifact,
                Name = name
            };
            facts.Add(new ArtifactMemberDeclarationFact
            {
                Id = FactIdFor(evidence.Adapter.Id, "artifact-member-declaration", member, property.Name),
                Subject = artifact.Subject,
                Evidence = evidence,
                Definition = new ArtifactMemberDeclarationDefinition
                {
                    Member = member,
                    DeclarationOrder = order
                }
            });
            facts.Add(new ArtifactMemberTypeUseFact
            {
                Id = FactIdFor(evidence.Adapter.Id, "artifact-member-type-use", member, property.Name),
                Subject = artifact.Subject,
                Evidence = evidence,
                Definition = new ArtifactMemberTypeUseDefinition
                {
                    Member = member,
                    Type = DotNetTypeShapes.TypeUseFor(property.Type, context)
                }
            });

            var role = roleFor?.Invoke(property);
            if (role is not null)
            {
                facts.Add(new ArtifactMemberRoleFact
                {
                    Id = FactIdFor(evidence.Adapter.Id, $"artifact-member-role-{(int)role.Value}", member, property.Name),
                    Subject = artifact.Subject,
                    Evidence = evidence,
                    Definition = new ArtifactMemberRoleDefinition
                    {
                        Member = member,
                        Role = role.Value
                    }
                });
            }
        }

        return facts;
    }

    static FactId FactIdFor(
        string adapter,
        string family,
        ArtifactMemberKey member,
        string sourceMemberName) => new()
    {
        Value = string.Join(
            ':',
            adapter,
            family,
            Encode(member.Artifact.Subject.Value),
            ((int)member.Artifact.Kind).ToString(System.Globalization.CultureInfo.InvariantCulture),
            Encode(sourceMemberName),
            Encode(member.Name))
    };

    static string Encode(string value) => string.Concat(
        value.Select(character => ((int)character).ToString("X4", System.Globalization.CultureInfo.InvariantCulture)));
}
