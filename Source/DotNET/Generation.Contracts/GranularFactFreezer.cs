// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation;

static class GranularFactFreezer
{
    public static ArtifactDeclarationFact Freeze(
        ArtifactDeclarationFact fact,
        FactId id,
        SubjectId subject,
        Evidence evidence,
        string path,
        AdapterContributionAdmissionContext context) => new()
    {
        Id = id,
        Subject = subject,
        Evidence = evidence,
        Definition = FreezeArtifactDeclaration(fact.Definition, $"{path}.Definition", context)
    };

    public static ArtifactMemberDeclarationFact Freeze(
        ArtifactMemberDeclarationFact fact,
        FactId id,
        SubjectId subject,
        Evidence evidence,
        string path,
        AdapterContributionAdmissionContext context) => new()
    {
        Id = id,
        Subject = subject,
        Evidence = evidence,
        Definition = FreezeMemberDeclaration(fact.Definition, $"{path}.Definition", context)
    };

    public static ArtifactMemberTypeUseFact Freeze(
        ArtifactMemberTypeUseFact fact,
        FactId id,
        SubjectId subject,
        Evidence evidence,
        string path,
        AdapterContributionAdmissionContext context) => new()
    {
        Id = id,
        Subject = subject,
        Evidence = evidence,
        Definition = FreezeMemberTypeUse(fact.Definition, $"{path}.Definition", context)
    };

    public static TypeUseBindingFact Freeze(
        TypeUseBindingFact fact,
        FactId id,
        SubjectId subject,
        Evidence evidence,
        string path,
        AdapterContributionAdmissionContext context) => new()
    {
        Id = id,
        Subject = subject,
        Evidence = evidence,
        Definition = FreezeBinding(fact.Definition, $"{path}.Definition", context)
    };

    public static ArtifactMemberRoleFact Freeze(
        ArtifactMemberRoleFact fact,
        FactId id,
        SubjectId subject,
        Evidence evidence,
        string path,
        AdapterContributionAdmissionContext context) => new()
    {
        Id = id,
        Subject = subject,
        Evidence = evidence,
        Definition = FreezeMemberRole(fact.Definition, $"{path}.Definition", context)
    };

    static ArtifactDeclarationDefinition FreezeArtifactDeclaration(
        ArtifactDeclarationDefinition? definition,
        string path,
        AdapterContributionAdmissionContext context)
    {
        if (definition is null)
        {
            context.Missing(path);
            return new ArtifactDeclarationDefinition
            {
                Artifact = FreezeArtifactKey(null, $"{path}.Artifact", context),
                Name = string.Empty
            };
        }

        return new ArtifactDeclarationDefinition
        {
            Artifact = FreezeArtifactKey(definition.Artifact, $"{path}.Artifact", context),
            Name = definition.Name ?? string.Empty,
            Description = definition.Description,
            File = definition.File
        };
    }

    static ArtifactMemberDeclarationDefinition FreezeMemberDeclaration(
        ArtifactMemberDeclarationDefinition? definition,
        string path,
        AdapterContributionAdmissionContext context)
    {
        if (definition is null)
        {
            context.Missing(path);
            return new ArtifactMemberDeclarationDefinition
            {
                Member = FreezeMemberKey(null, $"{path}.Member", context),
                DeclarationOrder = -1
            };
        }

        return new ArtifactMemberDeclarationDefinition
        {
            Member = FreezeMemberKey(definition.Member, $"{path}.Member", context),
            DeclarationOrder = definition.DeclarationOrder
        };
    }

    static ArtifactMemberTypeUseDefinition FreezeMemberTypeUse(
        ArtifactMemberTypeUseDefinition? definition,
        string path,
        AdapterContributionAdmissionContext context)
    {
        if (definition is null)
        {
            context.Missing(path);
            return new ArtifactMemberTypeUseDefinition
            {
                Member = FreezeMemberKey(null, $"{path}.Member", context),
                Type = FreezeTypeUse(null, $"{path}.Type", context)
            };
        }

        return new ArtifactMemberTypeUseDefinition
        {
            Member = FreezeMemberKey(definition.Member, $"{path}.Member", context),
            Type = FreezeTypeUse(definition.Type, $"{path}.Type", context)
        };
    }

    static TypeUseBindingDefinition FreezeBinding(
        TypeUseBindingDefinition? definition,
        string path,
        AdapterContributionAdmissionContext context)
    {
        if (definition is null)
        {
            context.Missing(path);
            return new TypeUseBindingDefinition
            {
                Member = FreezeMemberKey(null, $"{path}.Member", context),
                Target = FreezeArtifactKey(null, $"{path}.Target", context)
            };
        }

        return new TypeUseBindingDefinition
        {
            Member = FreezeMemberKey(definition.Member, $"{path}.Member", context),
            Target = FreezeArtifactKey(definition.Target, $"{path}.Target", context)
        };
    }

    static ArtifactMemberRoleDefinition FreezeMemberRole(
        ArtifactMemberRoleDefinition? definition,
        string path,
        AdapterContributionAdmissionContext context)
    {
        if (definition is null)
        {
            context.Missing(path);
            return new ArtifactMemberRoleDefinition
            {
                Member = FreezeMemberKey(null, $"{path}.Member", context),
                Role = ArtifactMemberRoleKind.Unknown
            };
        }

        return new ArtifactMemberRoleDefinition
        {
            Member = FreezeMemberKey(definition.Member, $"{path}.Member", context),
            Role = definition.Role
        };
    }

    static ArtifactMemberKey FreezeMemberKey(
        ArtifactMemberKey? member,
        string path,
        AdapterContributionAdmissionContext context)
    {
        if (member is null)
        {
            context.Missing(path);
            return new ArtifactMemberKey
            {
                Artifact = FreezeArtifactKey(null, $"{path}.Artifact", context),
                Name = string.Empty
            };
        }

        return new ArtifactMemberKey
        {
            Artifact = FreezeArtifactKey(member.Artifact, $"{path}.Artifact", context),
            Name = member.Name ?? string.Empty
        };
    }

    static TypeUseDefinition FreezeTypeUse(
        TypeUseDefinition? type,
        string path,
        AdapterContributionAdmissionContext context)
    {
        if (type is null)
        {
            context.Missing(path);
            return new TypeUseDefinition { Name = string.Empty, Shape = [] };
        }

        return new TypeUseDefinition
        {
            Name = type.Name ?? string.Empty,
            ObservedTypeSubject = type.ObservedTypeSubject is null
                ? null
                : FreezeSubject(type.ObservedTypeSubject, $"{path}.ObservedTypeSubject", context),
            Shape = FreezeShape(type.Shape, $"{path}.Shape", context)
        };
    }

    static ArtifactKey FreezeArtifactKey(
        ArtifactKey? key,
        string path,
        AdapterContributionAdmissionContext context)
    {
        if (key is null)
        {
            context.Missing(path);
            return new ArtifactKey
            {
                Subject = new SubjectId { Value = string.Empty },
                Kind = ArtifactKind.Unknown
            };
        }

        return new ArtifactKey
        {
            Subject = FreezeSubject(key.Subject, $"{path}.Subject", context),
            Kind = key.Kind
        };
    }

    static SubjectId FreezeSubject(
        SubjectId? subject,
        string path,
        AdapterContributionAdmissionContext context)
    {
        if (subject is null)
        {
            context.Missing(path);
            return new SubjectId { Value = string.Empty };
        }

        return new SubjectId { Value = subject.Value ?? string.Empty };
    }

    static ImmutableArray<TypeUseShapeKind> FreezeShape(
        IReadOnlyList<TypeUseShapeKind>? shape,
        string path,
        AdapterContributionAdmissionContext context)
    {
        if (shape is null)
        {
            context.NullCollection(path);
            return [];
        }

        return [.. shape];
    }
}
