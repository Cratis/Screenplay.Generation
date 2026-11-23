// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation;

static class TypeUseCompatibilityProjectionAdmission
{
    public static TypeUseCompatibilityProjectionFact Freeze(TypeUseCompatibilityProjectionFact fact, FactEnvelope envelope, AdapterContributionAdmissionContext context)
    {
        var definition = fact.Definition;
        if (definition?.TypeUse is null)
        {
            context.Missing($"{envelope.Path}.Definition.TypeUse");
        }

        return new()
        {
            Id = envelope.Id,
            Subject = envelope.Subject,
            Evidence = envelope.Evidence,
            Definition = new()
            {
                TypeUse = new() { Value = definition?.TypeUse?.Value ?? string.Empty },
                Profile = definition?.Profile ?? TypeUseCompatibilityProfile.Unknown,
                Version = definition?.Version ?? 0,
                SourceNodeIndex = definition?.SourceNodeIndex ?? -1,
                Type = GranularFactFreezer.FreezeTypeUse(definition?.Type, $"{envelope.Path}.Definition.Type", context)
            }
        };
    }

    public static void ValidateShape(TypeUseCompatibilityProjectionFact fact, string path, AdapterContributionAdmissionContext context)
    {
        context.Enum(fact.Definition.Profile, TypeUseCompatibilityProfile.Unknown, $"{path}.Definition.Profile", fact.Id, fact.Subject);
        if (fact.Definition.Version != 1)
        {
            Invalid(fact, "Only compatibility projection version 1 is supported", context);
        }
    }

    public static void Validate(IReadOnlyList<GenerationFact> facts, AdapterContributionAdmissionContext context)
    {
        foreach (var fact in facts)
        {
            ValidateNodes(fact, context);
        }

        foreach (var projection in facts.OfType<TypeUseCompatibilityProjectionFact>())
        {
            ValidateProjection(projection, facts, context);
        }
    }

    public static IReadOnlyList<TypeUseNodeObservation>? FreezeNodes(IReadOnlyList<TypeUseNodeObservation>? nodes, string path, AdapterContributionAdmissionContext context) =>
        nodes?.Select((node, index) => FreezeNode(node, $"{path}[{index}]", context)).ToImmutableArray();

    static void ValidateNodes(GenerationFact fact, AdapterContributionAdmissionContext context)
    {
        if (Observation(fact) is { Nodes: not null } type && !TypeUseCompatibilityProjectionValidation.ValidObservation(type))
        {
            Invalid(fact, "Supplemental type-use nodes must be complete and agree with the exact terminal", context);
        }
    }

    static void ValidateProjection(TypeUseCompatibilityProjectionFact projection, IReadOnlyList<GenerationFact> facts, AdapterContributionAdmissionContext context)
    {
        var observations = facts.Where(fact => fact.Id == projection.Definition.TypeUse && Matches(projection, fact)).ToArray();
        var count = facts.OfType<TypeUseCompatibilityProjectionFact>().Count(candidate => candidate.Definition.TypeUse == projection.Definition.TypeUse);
        if (!ValidLocalView(projection, observations, count))
        {
            Invalid(projection, "A compatibility projection requires one exact same-producer observation and a valid supported source-node view", context);
        }
    }

    static bool ValidLocalView(TypeUseCompatibilityProjectionFact projection, GenerationFact[] observations, int count) =>
        count == 1 && observations.Length == 1 && TypeUseCompatibilityProjectionValidation.Valid(projection.Definition, Observation(observations[0]));

    static TypeUseNodeObservation FreezeNode(TypeUseNodeObservation? node, string path, AdapterContributionAdmissionContext context)
    {
        if (node is null)
        {
            context.Missing(path);
        }

        return new()
        {
            Name = node?.Name ?? string.Empty,
            Subject = node?.Subject is { } subject ? GranularFactFreezer.FreezeSubject(subject, $"{path}.Subject", context) : null,
            Scope = BackendScopeFreezer.Key(node?.Scope, $"{path}.Scope", context)
        };
    }

    static TypeUseDefinition? Observation(GenerationFact fact) => fact switch
    {
        ArtifactMemberTypeUseFact typeUse => typeUse.Definition.Type,
        DeclarationInputMemberTypeUseFact typeUse => typeUse.Definition.Type,
        _ => null
    };

    static bool Matches(GenerationFact projection, GenerationFact observation) =>
        projection.Subject == observation.Subject && projection.Evidence.Adapter == observation.Evidence.Adapter &&
        projection.Evidence.Source == observation.Evidence.Source && projection.Evidence.Strength == EvidenceStrength.Exact && observation.Evidence.Strength == EvidenceStrength.Exact;

    static void Invalid(GenerationFact fact, string message, AdapterContributionAdmissionContext context) =>
        context.Add(AdapterContributionAdmissionDiagnosticCode.InvalidTypeUseShape, $"Contribution.Facts[{fact.Id}].Definition", message, fact.Id, fact.Subject);
}
