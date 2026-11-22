// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class BackendScopeFactResolver
{
    public static IReadOnlyList<ResolvedBackendScope> Resolve(IEnumerable<GenerationFact> facts) =>
    [
        .. facts.SelectMany(fact => BackendScopeFactReferences.Of(fact).Select(key => (Key: key, Fact: fact)))
            .GroupBy(reference => Structural.BackendScopeKey(reference.Key), StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new ResolvedBackendScope
            {
                Key = group.First().Key,
                Facts = [.. group.Select(reference => reference.Fact).Distinct().OrderBy(Structural.Fact, StringComparer.Ordinal)],
                Definitions = [.. group.Select(reference => reference.Fact).OfType<BackendScopeFact>()
                    .GroupBy(fact => Structural.BackendScope(fact.Definition), StringComparer.Ordinal)
                    .OrderBy(variant => variant.Key, StringComparer.Ordinal).Select(variant => variant.First().Definition)],
                SupportingFacts = [.. group.Select(reference => reference.Fact).OfType<BackendScopeFact>().Select(fact => fact.Id).Distinct().OrderBy(id => id.Value, StringComparer.Ordinal)],
                Evidence = [.. group.Select(reference => reference.Fact).OfType<BackendScopeFact>().Select(fact => fact.Evidence).OrderBy(Canonical.Evidence, StringComparer.Ordinal)]
            })
    ];

    public static IEnumerable<GenerationDiagnostic> Validate(IEnumerable<GenerationFact> facts)
    {
        var inputs = facts.ToArray();
        foreach (var fact in inputs.Where(HasInvalidUsage))
        {
            yield return new GenerationDiagnostic
            {
                Code = GenerationDiagnosticCodes.InvalidBackendScopeUsage,
                Severity = GenerationDiagnosticSeverity.Error,
                Outcome = GenerationDiagnosticOutcome.Unknown,
                Message = "Backend scope ownership is invalid: concepts and queries remain global, and scoped type selectors require an exact observed subject",
                Facts = [fact.Id], Subject = fact.Subject, Source = fact.Evidence.Source
            };
        }

        foreach (var scope in Resolve(inputs))
        {
            if (scope.Definitions.Count == 0)
            {
                yield return Diagnostic(scope, GenerationDiagnosticCodes.UnknownBackendScope, GenerationDiagnosticOutcome.Unknown, "The exact referenced backend scope has no declaration");
            }
            else if (scope.IsConflicted)
            {
                yield return Diagnostic(scope, GenerationDiagnosticCodes.ConflictingBackendScope, GenerationDiagnosticOutcome.Conflict, "The exact backend scope has incompatible metadata declarations");
            }

            foreach (var fact in scope.Facts.OfType<BackendScopeFact>().Where(fact => fact.Subject != fact.Definition.Key.Store))
            {
                yield return Diagnostic(scope, GenerationDiagnosticCodes.InvalidBackendScopeUsage, GenerationDiagnosticOutcome.Unknown, "The backend scope store does not equal its fact subject") with { Facts = [fact.Id] };
            }
        }
    }

    public static GenerationFact[] ValidFacts(GenerationFact[] facts, List<GenerationDiagnostic> diagnostics)
    {
        var scopeFailures = Validate(facts).ToArray();
        diagnostics.AddRange(scopeFailures);
        var invalid = scopeFailures.Where(diagnostic => diagnostic.Outcome is GenerationDiagnosticOutcome.Unknown or GenerationDiagnosticOutcome.Conflict)
            .SelectMany(diagnostic => diagnostic.Facts).ToHashSet();
        diagnostics.AddRange(BackendScopeOwnershipValidator.Validate(facts, invalid));
        return [.. facts.Where(fact => !invalid.Contains(fact.Id))];
    }

    public static GenerationFactRecord AttachScopeLineage(GenerationFactRecord record, GenerationFact[] facts)
    {
        var scopes = BackendScopeFactReferences.Of(record.Fact);
        if (record.Lineage is not { } lineage || scopes.Count == 0)
        {
            return record;
        }

        var inputs = facts.Where(fact => lineage.Inputs.Contains(fact.Id) || (fact is BackendScopeFact scope && scopes.Contains(scope.Definition.Key)))
            .GroupBy(fact => fact.Id).OrderBy(group => group.Key.Value, StringComparer.Ordinal)
            .Select(group => group.First()).ToArray();
        return record with
        {
            Lineage = lineage with { Inputs = [.. inputs.Select(fact => fact.Id)], Evidence = [.. inputs.Select(fact => fact.Evidence)] }
        };
    }

    public static bool HasInvalidUsage(GenerationFact fact)
    {
        var owner = BackendScopeFactReferences.Owner(fact);
        if (owner?.Scope is not null && owner.Kind is ArtifactKind.Concept or ArtifactKind.Query)
        {
            return true;
        }

        return fact switch
        {
            ArtifactFact artifact => artifact.Definition.Properties.Any(property => property.Type.Scope is not null && property.Type.Subject is null),
            ArtifactMemberTypeUseFact typeUse => typeUse.Definition.Type.ObservedScope is not null && typeUse.Definition.Type.ObservedTypeSubject is null,
            SpecificationValueFact value => value.Definition.Type is { Scope: not null, Subject: null },
            BackendScopeFact scope => InvalidDeclaration(scope),
            _ => false
        };
    }

    public static GenerationDiagnostic Diagnostic(ResolvedBackendScope scope, string code, GenerationDiagnosticOutcome outcome, string message) => new()
    {
        Code = code,
        Severity = outcome == GenerationDiagnosticOutcome.Unsupported ? GenerationDiagnosticSeverity.Warning : GenerationDiagnosticSeverity.Error,
        Outcome = outcome,
        Message = message,
        Subject = scope.Key.Store,
        Source = LoweringSupport.FirstSource(scope.Facts.Select(fact => fact.Evidence)),
        Facts = [.. scope.Facts.Select(fact => fact.Id).Distinct().OrderBy(id => id.Value, StringComparer.Ordinal)]
    };

    static bool InvalidDeclaration(BackendScopeFact scope)
    {
        if (scope.Evidence.Source is null || string.IsNullOrWhiteSpace(scope.Definition.Key.Store.Value))
        {
            return true;
        }

        if (scope.Definition.Key.EventModelName is not { } name)
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(name) || name.Any(char.IsControl);
    }
}
