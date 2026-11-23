// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class BackendScopeOwnershipValidator
{
    public static IEnumerable<GenerationDiagnostic> Validate(GenerationFact[] facts, HashSet<FactId> rejected)
    {
        var artifacts = facts.Select(fact => fact switch
        {
            ArtifactFact artifact => new Candidate(artifact.Definition.Key, fact),
            ArtifactDeclarationFact declaration => new Candidate(declaration.Definition.Artifact, fact),
            _ => null
        }).OfType<Candidate>().ToArray();
        foreach (var diagnostic in ValidateEndpoints(facts, rejected, artifacts).Concat(ValidateUsages(facts, rejected)))
        {
            yield return diagnostic;
        }
    }

    static IEnumerable<GenerationDiagnostic> ValidateEndpoints(GenerationFact[] facts, HashSet<FactId> rejected, Candidate[] artifacts)
    {
        foreach (var reference in facts.Where(fact => RequiresTarget(fact, facts)).SelectMany(fact => Endpoints(fact).Select(endpoint => (Fact: fact, Endpoint: endpoint))))
        {
            if (Check(reference.Fact, reference.Endpoint, artifacts) is { } diagnostic)
            {
                rejected.Add(reference.Fact.Id);
                yield return diagnostic;
            }
        }
    }

    static IEnumerable<GenerationDiagnostic> ValidateUsages(GenerationFact[] facts, HashSet<FactId> rejected)
    {
        var entries = facts.OfType<EntryPointFact>().ToLookup(
            entry => entry.Definition.Method,
            entry => new Candidate(entry.Definition.Artifact, entry));
        var declarations = facts.OfType<ArtifactUsageDeclarationFact>().ToLookup(
            declaration => declaration.Definition.Declaration,
            declaration => new Candidate(declaration.Definition.Artifact, declaration));
        foreach (var usage in facts.OfType<MethodArtifactUsageFact>())
        {
            foreach (var diagnostic in ValidateUsage(usage, [.. entries[usage.Definition.Method]], [.. declarations[usage.Definition.Declaration]]))
            {
                rejected.Add(usage.Id);
                yield return diagnostic;
            }
        }
    }

    // An opted-in observation is not a target assertion. Scope declarations and explicit bindings still validate normally.
    static bool RequiresTarget(GenerationFact fact, IReadOnlyList<GenerationFact> facts) =>
        fact is not ArtifactMemberTypeUseFact || !TypeUseBindingPolicies.OptedIn(fact, facts);

    static IEnumerable<Endpoint> Endpoints(GenerationFact fact) => fact switch
    {
        ArtifactFact artifact => ArtifactEndpoints(artifact),
        ArtifactMemberTypeUseFact use when use.Definition.Type.ObservedTypeSubject is { } subject => [new Endpoint(subject, use.Definition.Type.ObservedScope)],
        TypeUseBindingFact binding => [new Endpoint(binding.Definition.Target.Subject, binding.Definition.Target.Scope, binding.Definition.Target.Kind)],
        RelationshipFact relationship => [new Endpoint(relationship.Definition.Key.Source, relationship.Definition.Key.SourceScope), new Endpoint(relationship.Definition.Key.Target, relationship.Definition.Key.TargetScope)],
        _ => FacetEndpoints(fact)
    };

    static IEnumerable<Endpoint> ArtifactEndpoints(ArtifactFact artifact) => artifact.Definition.Properties.Where(property => property.Type.Subject is not null)
        .Select(property => new Endpoint(property.Type.Subject!, property.Type.Scope, property.Type.TargetArtifactKind));

    static IEnumerable<Endpoint> FacetEndpoints(GenerationFact fact) => fact switch
    {
        ArtifactUsageDeclarationFact declaration => [new Endpoint(declaration.Definition.Artifact.Subject, declaration.Definition.Artifact.Scope, declaration.Definition.Artifact.Kind)],
        EntryPointFact entry => [new Endpoint(entry.Definition.Artifact.Subject, entry.Definition.Artifact.Scope, entry.Definition.Artifact.Kind)],
        SpecificationScenarioFact scenario => [new Endpoint(scenario.Definition.TargetArtifact.Subject, scenario.Definition.TargetArtifact.Scope, scenario.Definition.TargetArtifact.Kind)],
        SpecificationStepFact step when step.Definition.Artifact is { } artifact => [new Endpoint(artifact.Subject, artifact.Scope, artifact.Kind)],
        SpecificationValueFact value when value.Definition.Type?.Subject is { } subject => [new Endpoint(subject, value.Definition.Type.Scope, value.Definition.Type.TargetArtifactKind)],
        _ => []
    };

    static IEnumerable<GenerationDiagnostic> ValidateUsage(MethodArtifactUsageFact usage, Candidate[] sources, Candidate[] targets)
    {
        var definition = usage.Definition;
        if (sources.Length == 0)
        {
            yield break;
        }

        if (CheckSelection(usage, definition.SourceScope, sources) is { } sourceFailure)
        {
            yield return sourceFailure;
        }

        if (CheckSelection(usage, definition.TargetScope, targets) is { } targetFailure)
        {
            yield return targetFailure;
        }
    }

    static GenerationDiagnostic? Check(GenerationFact fact, Endpoint endpoint, Candidate[] artifacts)
    {
        var candidates = artifacts.Where(candidate => candidate.Key.Subject == endpoint.Subject &&
                                                       (endpoint.Kind is null || candidate.Key.Kind == endpoint.Kind)).ToArray();
        return CheckSelection(fact, endpoint.Scope, candidates);
    }

    static GenerationDiagnostic? CheckSelection(GenerationFact fact, BackendScopeKey? selector, Candidate[] candidates)
    {
        if (HasSelection(selector, candidates))
        {
            return null;
        }

        var ambiguous = selector is null && candidates.Select(candidate => candidate.Key.Scope).Distinct().Count() > 1;
        var inputs = candidates.Select(candidate => candidate.Fact).Append(fact).DistinctBy(input => input.Id)
            .OrderBy(input => input.Id.Value, StringComparer.Ordinal).ToArray();
        return new GenerationDiagnostic
        {
            Code = ambiguous ? GenerationDiagnosticCodes.AmbiguousBackendScopeOwnership : GenerationDiagnosticCodes.UnknownBackendScope,
            Severity = GenerationDiagnosticSeverity.Error,
            Outcome = ambiguous ? GenerationDiagnosticOutcome.Conflict : GenerationDiagnosticOutcome.Unknown,
            Message = ambiguous ? "An unqualified reference has several exact backend occurrences; no scope was selected" : "The exact referenced backend occurrence is not declared; null selects only an unscoped occurrence",
            Subject = fact.Subject,
            Source = LoweringSupport.FirstSource(inputs.Select(input => input.Evidence)),
            Facts = [.. inputs.Select(input => input.Id)]
        };
    }

    static bool HasSelection(BackendScopeKey? selector, Candidate[] candidates) =>
        candidates.Any(candidate => candidate.Key.Scope == selector) ||
        (selector is null && !candidates.Any(candidate => candidate.Key.Scope is not null));

    sealed record Endpoint(SubjectId Subject, BackendScopeKey? Scope, ArtifactKind? Kind = null);
    sealed record Candidate(ArtifactKey Key, GenerationFact Fact);
}
