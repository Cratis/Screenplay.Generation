// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;

namespace Cratis.Screenplay.Generation;

static class ArtifactUsageDerivation
{
    public static GenerationDerivationRuleIdentity Rule { get; } = new() { Id = "cratis.screenplay.artifact-usage", Version = "1.0.0" };

    public static ArtifactUsageDerivationResult Derive(ImmutableArray<GenerationFactRecord> baseFacts)
    {
        var diagnostics = new List<GenerationDiagnostic>();
        var facts = baseFacts.Select(record => record.Fact).Where(fact => ValidOwner(fact, diagnostics)).ToArray();
        var index = new UsageIndex(facts);
        var edges = new List<UsageEdge>();
        foreach (var group in facts.OfType<MethodArtifactUsageFact>()
                     .GroupBy(fact => Structural.WithScopes(Structural.SemanticKey("usage", fact.Definition.Method.Value, fact.Definition.Declaration.Value), fact.Definition.SourceScope, fact.Definition.TargetScope), StringComparer.Ordinal)
                     .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            Join([.. group], index, edges, diagnostics);
        }

        var blockedEntries = ConflictedEntries(facts, diagnostics);
        LinkSuppressedInputs(edges, blockedEntries, diagnostics);
        var records = edges.Where(edge => !IsBlocked(edge, blockedEntries))
            .GroupBy(edge => Structural.RelationshipKey(edge.Key), StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => BackendScopeFactResolver.AttachScopeLineage(Record(group.First().Key, group.SelectMany(edge => edge.Inputs)), facts))
            .ToImmutableArray();
        return new ArtifactUsageDerivationResult(
            [.. facts.Where(IsInput).Select(fact => fact.Id).Distinct().OrderBy(id => id.Value, StringComparer.Ordinal)],
            records,
            [.. diagnostics.OrderBy(Canonical.Diagnostic, StringComparer.Ordinal)]);
    }

    static HashSet<string> ConflictedEntries(GenerationFact[] facts, List<GenerationDiagnostic> diagnostics)
    {
        var inputs = diagnostics.Where(diagnostic => diagnostic.Outcome == GenerationDiagnosticOutcome.Conflict)
            .SelectMany(diagnostic => diagnostic.Facts).ToHashSet();
        return facts.OfType<EntryPointFact>().Where(entry => inputs.Contains(entry.Id))
            .Select(entry => MappingKey(entry.Definition.Method, entry.Definition.Artifact.Scope)).ToHashSet(StringComparer.Ordinal);
    }

    static bool IsBlocked(UsageEdge edge, HashSet<string> blockedEntries) =>
        edge.Inputs.OfType<EntryPointFact>().Any(entry => blockedEntries.Contains(MappingKey(entry.Definition.Method, entry.Definition.Artifact.Scope)));

    static void LinkSuppressedInputs(List<UsageEdge> edges, HashSet<string> blockedEntries, List<GenerationDiagnostic> diagnostics)
    {
        var suppressed = edges.Where(edge => IsBlocked(edge, blockedEntries)).ToArray();
        for (var index = 0; index < diagnostics.Count; index++)
        {
            var diagnostic = diagnostics[index];
            if (diagnostic.Outcome != GenerationDiagnosticOutcome.Conflict)
            {
                continue;
            }

            var inputs = suppressed.Where(edge => edge.Inputs.OfType<EntryPointFact>().Any(entry => diagnostic.Facts.Contains(entry.Id)))
                .SelectMany(edge => edge.Inputs).Select(fact => fact.Id);
            diagnostics[index] = diagnostic with { Facts = [.. diagnostic.Facts.Concat(inputs).Distinct().OrderBy(id => id.Value, StringComparer.Ordinal)] };
        }
    }

    static string MappingKey(SubjectId subject, BackendScopeKey? scope) =>
        Structural.WithScopes(Structural.SemanticKey("usage-mapping", subject.Value), scope);

    static void Join(MethodArtifactUsageFact[] usages, UsageIndex index, List<UsageEdge> edges, List<GenerationDiagnostic> diagnostics)
    {
        var usage = usages[0].Definition;
        var entries = index.Entries(usage);
        if (entries.Length == 0)
        {
            return;
        }

        var declarations = index.UsageDeclarations(usage);
        var inputs = usages.Cast<GenerationFact>().Concat(entries).Concat(declarations).ToArray();
        if (MappingsConflict(usages, entries, declarations))
        {
            diagnostics.Add(Diagnostic(GenerationDiagnosticOutcome.Conflict, inputs, "Incompatible exact artifact usage mappings"));
            return;
        }

        if (usage.Kind != RelationshipKind.Reads)
        {
            diagnostics.Add(Diagnostic(GenerationDiagnosticOutcome.Unsupported, inputs, $"Artifact usage role '{usage.Kind}' is not supported by the built-in join"));
            return;
        }

        if (declarations.Length == 0)
        {
            diagnostics.Add(Diagnostic(GenerationDiagnosticOutcome.Unknown, inputs, "The executed declaration has no exact artifact mapping"));
            return;
        }

        var endpoints = new UsageEndpoints(usage.Kind, entries[0].Definition.Artifact, declarations[0].Definition.Artifact, inputs);
        JoinEndpoints(endpoints, index, edges, diagnostics);
    }

    static bool MappingsConflict(MethodArtifactUsageFact[] usages, EntryPointFact[] entries, ArtifactUsageDeclarationFact[] declarations) =>
        usages.Select(fact => fact.Definition.Kind).Distinct().Count() != 1 ||
        entries.Select(fact => fact.Definition.Artifact).Distinct().Count() != 1 ||
        declarations.Select(fact => fact.Definition.Artifact).Distinct().Count() > 1;

    static void JoinEndpoints(UsageEndpoints endpoints, UsageIndex index, List<UsageEdge> edges, List<GenerationDiagnostic> diagnostics)
    {
        var sourceFacts = index.Declarations(endpoints.Source);
        var targetFacts = index.Declarations(endpoints.Target);
        GenerationFact[] inputs = [.. endpoints.Inputs, .. sourceFacts, .. targetFacts];
        if (sourceFacts.Length == 0 || targetFacts.Length == 0)
        {
            diagnostics.Add(Diagnostic(GenerationDiagnosticOutcome.Unknown, inputs, "An artifact usage endpoint lacks an exact artifact declaration"));
            return;
        }

        if (HasConflict(sourceFacts) || HasConflict(targetFacts))
        {
            diagnostics.Add(Diagnostic(GenerationDiagnosticOutcome.Conflict, inputs, "An artifact usage endpoint has incompatible declarations"));
            return;
        }

        edges.Add(new UsageEdge(new RelationshipKey { Kind = endpoints.Kind, Source = endpoints.Source.Subject, Target = endpoints.Target.Subject, SourceScope = endpoints.Source.Scope, TargetScope = endpoints.Target.Scope }, inputs));
    }

    static bool ValidOwner(GenerationFact fact, List<GenerationDiagnostic> diagnostics)
    {
        var owner = fact switch
        {
            ArtifactUsageDeclarationFact declaration => declaration.Definition.Declaration,
            EntryPointFact entry => entry.Definition.Method,
            MethodArtifactUsageFact usage => usage.Definition.Method,
            _ => fact.Subject
        };
        if (owner == fact.Subject)
        {
            return true;
        }

        diagnostics.Add(Diagnostic(GenerationDiagnosticOutcome.Unknown, [fact], "The usage declaration or method does not equal its fact subject"));
        return false;
    }

    static bool IsInput(GenerationFact fact) => fact is BackendScopeFact or ArtifactFact or ArtifactDeclarationFact or ArtifactUsageDeclarationFact or EntryPointFact or MethodArtifactUsageFact;

    static bool HasConflict(GenerationFact[] declarations)
    {
        var facets = declarations.Select(fact => fact switch
        {
            ArtifactFact artifact => Structural.ArtifactDeclaration(new ArtifactDeclarationDefinition
            {
                Artifact = artifact.Definition.Key,
                Name = artifact.Definition.Name,
                Description = artifact.Definition.Description
            }),
            ArtifactDeclarationFact declaration => Structural.ArtifactDeclaration(declaration.Definition),
            _ => string.Empty
        });
        return facets.Distinct(StringComparer.Ordinal).Count() > 1 ||
               declarations.OfType<ArtifactFact>().Select(fact => Structural.Artifact(fact.Definition)).Distinct(StringComparer.Ordinal).Count() > 1;
    }

    static GenerationFactRecord Record(RelationshipKey key, IEnumerable<GenerationFact> facts)
    {
        var inputs = Inputs(facts);
        return new GenerationFactRecord
        {
            Fact = new RelationshipFact
            {
                Id = new FactId { Value = $"generation:derive:artifact-usage:1:{Encode(Structural.RelationshipKey(key))}" },
                Subject = key.Source,
                Evidence = inputs[0].Evidence,
                Definition = new RelationshipDefinition { Key = key }
            },
            Lineage = new GenerationFactLineage
            {
                Producer = Rule,
                Inputs = [.. inputs.Select(fact => fact.Id)],
                Evidence = [.. inputs.Select(fact => fact.Evidence)]
            }
        };
    }

    static GenerationDiagnostic Diagnostic(GenerationDiagnosticOutcome outcome, IEnumerable<GenerationFact> facts, string message)
    {
        var inputs = Inputs(facts);
        return new GenerationDiagnostic
        {
            Code = outcome switch
            {
                GenerationDiagnosticOutcome.Conflict => GenerationDiagnosticCodes.ConflictingArtifactUsageMapping,
                GenerationDiagnosticOutcome.Unsupported => GenerationDiagnosticCodes.UnsupportedArtifactUsageRole,
                _ => GenerationDiagnosticCodes.MissingArtifactUsageMapping
            },
            Severity = outcome == GenerationDiagnosticOutcome.Unsupported ? GenerationDiagnosticSeverity.Warning : GenerationDiagnosticSeverity.Error,
            Outcome = outcome,
            Message = message,
            Facts = [.. inputs.Select(fact => fact.Id)],
            Source = inputs.FirstOrDefault(fact => fact.Evidence.Source is not null)?.Evidence.Source,
            Subject = inputs[0].Subject
        };
    }

    static GenerationFact[] Inputs(IEnumerable<GenerationFact> facts) =>
    [
        .. facts.GroupBy(fact => fact.Id.Value, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => group.OrderBy(Structural.Fact, StringComparer.Ordinal).First())
    ];

    static string Encode(string value) => string.Concat(value.Select(character => ((int)character).ToString("X4", CultureInfo.InvariantCulture)));

    sealed record UsageEndpoints(RelationshipKind Kind, ArtifactKey Source, ArtifactKey Target, GenerationFact[] Inputs);

    sealed record UsageEdge(RelationshipKey Key, GenerationFact[] Inputs);

    sealed class UsageIndex
    {
        readonly Dictionary<string, EntryPointFact[]> _entries;
        readonly Dictionary<string, ArtifactUsageDeclarationFact[]> _usages;
        readonly Dictionary<ArtifactKey, GenerationFact[]> _artifacts;

        public UsageIndex(GenerationFact[] facts)
        {
            _entries = facts.OfType<EntryPointFact>()
                .GroupBy(entry => MappingKey(entry.Definition.Method, entry.Definition.Artifact.Scope), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
            _usages = facts.OfType<ArtifactUsageDeclarationFact>()
                .GroupBy(declaration => MappingKey(declaration.Definition.Declaration, declaration.Definition.Artifact.Scope), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
            _artifacts = facts.Where(fact => fact is ArtifactFact or ArtifactDeclarationFact)
                .GroupBy(fact => fact is ArtifactFact artifact ? artifact.Definition.Key : ((ArtifactDeclarationFact)fact).Definition.Artifact)
                .ToDictionary(group => group.Key, group => group.ToArray());
        }

        public EntryPointFact[] Entries(MethodArtifactUsageDefinition usage) =>
            _entries.TryGetValue(MappingKey(usage.Method, usage.SourceScope), out var entries) ? entries : [];

        public ArtifactUsageDeclarationFact[] UsageDeclarations(MethodArtifactUsageDefinition usage) =>
            _usages.TryGetValue(MappingKey(usage.Declaration, usage.TargetScope), out var declarations) ? declarations : [];

        public GenerationFact[] Declarations(ArtifactKey key) =>
            _artifacts.TryGetValue(key, out var declarations) ? declarations : [];
    }
}

sealed record ArtifactUsageDerivationResult(
    ImmutableArray<FactId> Inputs,
    ImmutableArray<GenerationFactRecord> Facts,
    ImmutableArray<GenerationDiagnostic> Diagnostics);
