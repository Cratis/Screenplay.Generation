// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class TypeUseCompatibilityProjections
{
    public static TypeUseDefinition? Observation(GenerationFact fact) => fact switch
    {
        ArtifactMemberTypeUseFact typeUse => typeUse.Definition.Type,
        DeclarationInputMemberTypeUseFact typeUse => typeUse.Definition.Type,
        _ => null
    };

    public static TypeUseDefinition Select(GenerationFact observation, IReadOnlyList<GenerationFact> facts)
    {
        var original = Observation(observation)!;
        var projections = ForObservation(observation, facts).ToArray();
        return projections.Length == 1 && TypeUseCompatibilityProjectionValidation.Valid(projections[0].Definition, original)
            ? projections[0].Definition.Type
            : original;
    }

    public static ArtifactMemberTypeUseFact Select(ArtifactMemberTypeUseFact observation, IReadOnlyList<GenerationFact> facts) =>
        observation with { Definition = observation.Definition with { Type = Select((GenerationFact)observation, facts) } };

    public static GenerationFact[] Supports(IEnumerable<GenerationFact> observations, IReadOnlyList<GenerationFact> facts) =>
        [.. observations.SelectMany(observation => ForObservation(observation, facts))];

    public static string ExactKey(TypeUseDefinition type) => Structural.TypeUse(type with { Nodes = null });

    public static bool Agree(IEnumerable<TypeUseDefinition> observations)
    {
        var types = observations.ToArray();
        return types.Select(ExactKey).Distinct(StringComparer.Ordinal).Count() <= 1 &&
            types.Where(type => type.Nodes is not null).Select(Structural.TypeUse).Distinct(StringComparer.Ordinal).Count() <= 1;
    }

    public static void Validate(GenerationFact fact, IReadOnlyList<GenerationFact> facts, List<GenerationDiagnostic> diagnostics)
    {
        ValidateNodes(fact, diagnostics);
        if (fact is TypeUseCompatibilityProjectionFact projection)
        {
            ValidateProjection(projection, facts, diagnostics);
        }
    }

    static void ValidateNodes(GenerationFact fact, List<GenerationDiagnostic> diagnostics)
    {
        if (Observation(fact) is { Nodes: not null } type && !TypeUseCompatibilityProjectionValidation.ValidObservation(type))
        {
            diagnostics.Add(new()
            {
                Code = GenerationDiagnosticCodes.InvalidTypeUseCompatibilityProjection,
                Severity = GenerationDiagnosticSeverity.Error,
                Outcome = GenerationDiagnosticOutcome.Unsupported,
                Message = "Supplemental type-use nodes must be complete and agree with the exact terminal",
                Facts = [fact.Id], Subject = fact.Subject, Source = fact.Evidence.Source
            });
        }
    }

    static void ValidateProjection(TypeUseCompatibilityProjectionFact projection, IReadOnlyList<GenerationFact> facts, List<GenerationDiagnostic> diagnostics)
    {
        var observations = facts.Where(candidate => candidate.Id == projection.Definition?.TypeUse && Matches(projection, candidate)).ToArray();
        var siblings = facts.OfType<TypeUseCompatibilityProjectionFact>().Count(candidate =>
            candidate.Definition?.TypeUse == projection.Definition?.TypeUse && Matches(projection, candidate));
        if (!ValidLocalView(projection, observations, siblings))
        {
            diagnostics.Add(Invalid(projection, observations));
        }
    }

    static bool ValidLocalView(TypeUseCompatibilityProjectionFact projection, GenerationFact[] observations, int count) =>
        count == 1 && observations.Length == 1 && TypeUseCompatibilityProjectionValidation.Valid(projection.Definition, Observation(observations[0]));

    static IEnumerable<TypeUseCompatibilityProjectionFact> ForObservation(GenerationFact observation, IReadOnlyList<GenerationFact> facts) =>
        facts.OfType<TypeUseCompatibilityProjectionFact>().Where(projection => projection.Definition?.TypeUse == observation.Id && Matches(projection, observation));

    static bool Matches(GenerationFact projection, GenerationFact observation) =>
        projection.Subject == observation.Subject && projection.Evidence.Adapter == observation.Evidence.Adapter &&
        projection.Evidence.Source == observation.Evidence.Source && projection.Evidence.Strength == EvidenceStrength.Exact && observation.Evidence.Strength == EvidenceStrength.Exact;

    static GenerationDiagnostic Invalid(TypeUseCompatibilityProjectionFact projection, IEnumerable<GenerationFact> observations) => new()
    {
        Code = GenerationDiagnosticCodes.InvalidTypeUseCompatibilityProjection,
        Severity = GenerationDiagnosticSeverity.Error,
        Outcome = GenerationDiagnosticOutcome.Unsupported,
        Message = $"Compatibility projection '{projection.Id.Value}' requires one exact same-producer observation and a valid supported source-node view",
        Subject = projection.Subject,
        Source = projection.Evidence.Source,
        Facts = [projection.Id, .. observations.Select(fact => fact.Id)]
    };
}
