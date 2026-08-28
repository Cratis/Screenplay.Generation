// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class GranularFactAdmissionValidator
{
    public static void Validate(
        ArtifactDeclarationFact fact,
        string path,
        AdapterContributionAdmissionContext context)
    {
        AdapterContributionAdmissionValidator.ValidateArtifactKey(
            fact.Definition.Artifact,
            $"{path}.Definition.Artifact",
            fact.Id,
            context);
        ValidateOwner(fact, fact.Definition.Artifact.Subject, $"{path}.Definition.Artifact.Subject", context);
        AdapterContributionAdmissionValidator.ValidateRequiredText(
            fact.Definition.Name,
            $"{path}.Definition.Name",
            fact.Id,
            fact.Subject,
            context);
    }

    public static void Validate(
        ArtifactMemberDeclarationFact fact,
        string path,
        AdapterContributionAdmissionContext context)
    {
        ValidateMember(fact, fact.Definition.Member, $"{path}.Definition.Member", context);
        if (fact.Definition.DeclarationOrder < 0)
        {
            context.Add(
                AdapterContributionAdmissionDiagnosticCode.InvalidDeclarationOrder,
                $"{path}.Definition.DeclarationOrder",
                $"Artifact member declaration order '{fact.Definition.DeclarationOrder}' must not be negative",
                fact.Id,
                fact.Subject);
        }
    }

    public static void Validate(
        ArtifactMemberTypeUseFact fact,
        string path,
        AdapterContributionAdmissionContext context)
    {
        ValidateMember(fact, fact.Definition.Member, $"{path}.Definition.Member", context);
        var typePath = $"{path}.Definition.Type";
        AdapterContributionAdmissionValidator.ValidateRequiredText(
            fact.Definition.Type.Name,
            $"{typePath}.Name",
            fact.Id,
            fact.Subject,
            context);
        if (fact.Definition.Type.ObservedTypeSubject is not null)
        {
            AdapterContributionAdmissionValidator.ValidateSubject(
                fact.Definition.Type.ObservedTypeSubject,
                $"{typePath}.ObservedTypeSubject",
                fact.Id,
                context);
        }

        ValidateShape(fact, fact.Definition.Type.Shape, $"{typePath}.Shape", context);
    }

    public static void Validate(
        TypeUseBindingFact fact,
        string path,
        AdapterContributionAdmissionContext context)
    {
        ValidateMember(fact, fact.Definition.Member, $"{path}.Definition.Member", context);
        AdapterContributionAdmissionValidator.ValidateArtifactKey(
            fact.Definition.Target,
            $"{path}.Definition.Target",
            fact.Id,
            context);
    }

    public static void Validate(
        ArtifactMemberRoleFact fact,
        string path,
        AdapterContributionAdmissionContext context)
    {
        ValidateMember(fact, fact.Definition.Member, $"{path}.Definition.Member", context);
        context.Enum(
            fact.Definition.Role,
            ArtifactMemberRoleKind.Unknown,
            $"{path}.Definition.Role",
            fact.Id,
            fact.Subject);
    }

    static void ValidateMember(
        GenerationFact fact,
        ArtifactMemberKey member,
        string path,
        AdapterContributionAdmissionContext context)
    {
        AdapterContributionAdmissionValidator.ValidateArtifactKey(
            member.Artifact,
            $"{path}.Artifact",
            fact.Id,
            context);
        ValidateOwner(fact, member.Artifact.Subject, $"{path}.Artifact.Subject", context);
        AdapterContributionAdmissionValidator.ValidateRequiredText(
            member.Name,
            $"{path}.Name",
            fact.Id,
            fact.Subject,
            context);
    }

    static void ValidateShape(
        GenerationFact fact,
        IReadOnlyList<TypeUseShapeKind> shape,
        string path,
        AdapterContributionAdmissionContext context)
    {
        for (var index = 0; index < shape.Count; index++)
        {
            context.Enum(shape[index], TypeUseShapeKind.Unknown, $"{path}[{index}]", fact.Id, fact.Subject);
        }

        var allNodesAreDefined = shape.All(node => node != TypeUseShapeKind.Unknown && Enum.IsDefined(node));
        var hasExactNamedTerminal = shape.Count > 0 &&
                                    shape[^1] == TypeUseShapeKind.Named &&
                                    shape.Take(shape.Count - 1).All(node => node != TypeUseShapeKind.Named);
        if (allNodesAreDefined && !hasExactNamedTerminal)
        {
            context.Add(
                AdapterContributionAdmissionDiagnosticCode.InvalidTypeUseShape,
                path,
                "A type-use shape must contain wrappers followed by exactly one terminal named type",
                fact.Id,
                fact.Subject);
        }
    }

    static void ValidateOwner(
        GenerationFact fact,
        SubjectId owner,
        string path,
        AdapterContributionAdmissionContext context)
    {
        if (owner != fact.Subject)
        {
            context.Add(
                AdapterContributionAdmissionDiagnosticCode.OwnershipMismatch,
                path,
                $"Nested artifact owner '{owner.Value}' does not equal fact subject '{fact.Subject.Value}'",
                fact.Id,
                fact.Subject);
        }
    }
}
