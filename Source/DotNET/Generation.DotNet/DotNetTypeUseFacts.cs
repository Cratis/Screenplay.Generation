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
        Func<IPropertySymbol, ArtifactMemberRoleKind?>? roleFor = null) =>
        EmitProperties(DotNetTypeShapes.PublicReadablePropertiesOf(type), artifact, context, evidence, roleFor);

    /// <summary>
    /// Emits granular facts from an explicitly selected property surface without changing legacy overload binding.
    /// </summary>
    /// <param name="type">The nominated type whose artifact owns every emitted member.</param>
    /// <param name="artifact">The artifact role that owns the members.</param>
    /// <param name="context">The analyzed projects establishing authored origin and exact type subjects.</param>
    /// <param name="evidence">The caller's evidence retained on all emitted facts.</param>
    /// <param name="scope">The declared or authority-filtered inherited surface.</param>
    /// <param name="roleFor">An optional semantic role rule applied only to retained properties.</param>
    /// <returns>The member facts in contiguous derived-first declaration order.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The scope is undefined.</exception>
    public static IReadOnlyList<GenerationFact> EmitWithSurface(
        INamedTypeSymbol type,
        ArtifactKey artifact,
        DotNetAnalysisContext context,
        Evidence evidence,
        DotNetMemberSurfaceScope scope,
        Func<IPropertySymbol, ArtifactMemberRoleKind?>? roleFor = null) =>
        EmitProperties(DotNetEffectiveProperties.Of(type, context, scope), artifact, context, evidence, roleFor);

    static List<GenerationFact> EmitProperties(
        IReadOnlyList<IPropertySymbol> properties,
        ArtifactKey artifact,
        DotNetAnalysisContext context,
        Evidence evidence,
        Func<IPropertySymbol, ArtifactMemberRoleKind?>? roleFor)
    {
        List<GenerationFact> facts = [];
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
        string sourceMemberName)
    {
        var legacy = string.Join(
            ':',
            adapter,
            family,
            Encode(member.Artifact.Subject.Value),
            ((int)member.Artifact.Kind).ToString(System.Globalization.CultureInfo.InvariantCulture),
            Encode(sourceMemberName),
            Encode(member.Name));
        var scope = member.Artifact.Scope;
        return new FactId
        {
            Value = scope is null ? legacy : $"{legacy}:scope:{Encode(scope.Store.Value)}:{(scope.EventModelName is { } name ? $"1:{Encode(name)}" : "0")}"
        };
    }

    static string Encode(string value) => string.Concat(
        value.Select(character => ((int)character).ToString("X4", System.Globalization.CultureInfo.InvariantCulture)));
}
