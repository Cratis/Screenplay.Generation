// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation;

static class EntryPointInputMemberDerivation
{
    public static GenerationDerivationRuleIdentity Rule { get; } = new() { Id = "cratis.screenplay.entry-point-input-members", Version = "1.0.0" };

    public static EntryPointInputMemberDerivationResult Derive(ImmutableArray<GenerationFactRecord> baseFacts)
    {
        var facts = baseFacts.Select(record => record.Fact).ToArray();
        var intents = facts.OfType<EntryPointInputTransferFact>().ToArray();
        var records = new List<GenerationFactRecord>();
        var diagnostics = new List<GenerationDiagnostic>();
        foreach (var group in intents.GroupBy(fact => fact.Definition.Artifact).OrderBy(group => Structural.ArtifactKey(group.Key), StringComparer.Ordinal))
        {
            var selection = new EndpointSelection(baseFacts, group.Key, [.. group]);
            records.AddRange(selection.Derive());
            diagnostics.AddRange(selection.Diagnostics);
        }

        return new(
            intents.Length == 0 ? [] : [.. facts.Select(fact => fact.Id).Distinct().OrderBy(id => id.Value, StringComparer.Ordinal)],
            [.. records],
            [.. diagnostics.DistinctBy(Canonical.Diagnostic).OrderBy(Canonical.Diagnostic, StringComparer.Ordinal)]);
    }

    static ArtifactKey? Artifact(GenerationFact fact) => fact switch
    {
        ArtifactFact artifact => artifact.Definition.Key,
        ArtifactDeclarationFact declaration => declaration.Definition.Artifact,
        _ => DirectMember(fact)?.Artifact
    };

    static ArtifactMemberKey? DirectMember(GenerationFact fact) => fact switch
    {
        ArtifactMemberDeclarationFact member => member.Definition.Member,
        ArtifactMemberTypeUseFact typeUse => typeUse.Definition.Member,
        TypeUseBindingFact binding => binding.Definition.Member,
        ArtifactMemberRoleFact role => role.Definition.Member,
        _ => null
    };

    static DeclarationMemberKey? DeclarationMember(GenerationFact fact) => fact switch
    {
        DeclarationInputMemberDeclarationFact declaration => declaration.Definition.Member,
        DeclarationInputMemberTypeUseFact typeUse => typeUse.Definition.Member,
        DeclarationInputMemberTargetOmittedFact omission => omission.Definition.Member,
        _ => null
    };

    static IEnumerable<string> DirectNames(GenerationFact fact) => fact switch
    {
        ArtifactFact artifact => artifact.Definition.Properties.Select(property => property.Name),
        _ => DirectMember(fact) is { } member ? [member.Name] : []
    };

    static IEnumerable<int> DirectOrders(GenerationFact fact) => fact switch
    {
        ArtifactFact artifact => artifact.Definition.Properties.Select((_, index) => index),
        ArtifactMemberDeclarationFact member => [member.Definition.DeclarationOrder],
        _ => []
    };

    static GenerationFact[] Inputs(IEnumerable<GenerationFact> facts) =>
        [.. facts.DistinctBy(fact => (fact.Id, Structural.FactDefinition(fact), Structural.Evidence(fact.Evidence))).OrderBy(fact => fact.Id.Value, StringComparer.Ordinal).ThenBy(Structural.FactDefinition, StringComparer.Ordinal)];

    static GenerationFact[] PolicyOutputs(ArtifactMemberTypeUseFact typeUse, ArtifactMemberKey member, bool conceptOnly) => conceptOnly ?
    [
        new TypeUseBindingPolicyFact
        {
            Id = Id(member, "policy"),
            Subject = typeUse.Subject,
            Evidence = typeUse.Evidence,
            Definition = new() { TypeUse = typeUse.Id, Policy = TypeUseBindingPolicyKind.ConceptOnlyWithUnboundFallback }
        }
    ] : [];

    static FactId Id(ArtifactMemberKey member, string facet) => new()
    {
        Value = $"generation:derive:entry-point-input-members:1:{facet}:{Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(Structural.ArtifactMemberKey(member)))}"
    };

    sealed record Candidate(string Execution, int? ExecutionOrder, DeclarationMemberKey Member, int MemberOrder, TypeUseDefinition Type, GenerationFact[] Inputs);

    sealed class EndpointSelection(ImmutableArray<GenerationFactRecord> baseFacts, ArtifactKey endpoint, EntryPointInputTransferFact[] intents)
    {
        readonly GenerationFact[] _facts = [.. baseFacts.Select(record => record.Fact)];
        readonly List<GenerationDiagnostic> _diagnostics = [];
        readonly List<Candidate> _candidates = [];
        GenerationFact[] _endpointInputs = [];

        public IReadOnlyList<GenerationDiagnostic> Diagnostics => _diagnostics;

        public GenerationFactRecord[] Derive()
        {
            _endpointInputs = [.. _facts.Where(fact => Artifact(fact) == endpoint), .. intents];
            if (!ValidEndpoint())
            {
                return [];
            }

            var names = _endpointInputs.SelectMany(DirectNames).ToHashSet(StringComparer.Ordinal);
            foreach (var group in RelevantUsages().GroupBy(fact => Structural.WithScopes(Structural.SemanticKey("input-execution", fact.Definition.Method.Value, fact.Definition.Declaration.Value), fact.Definition.SourceScope, fact.Definition.TargetScope), StringComparer.Ordinal))
            {
                Observe([.. group], names);
            }

            if (_diagnostics.Count > 0 || !ProveOrder())
            {
                return [];
            }

            return CreateMembers();
        }

        static bool ValidMemberIdentity(Candidate candidate) => !string.IsNullOrWhiteSpace(candidate.Member.Name) &&
            candidate.Member.Name == candidate.Member.Name.Trim() &&
            candidate.Inputs.Where(fact => fact is DeclarationInputMemberDeclarationFact or DeclarationInputMemberTypeUseFact).All(fact => fact.Subject == candidate.Member.Declaration && fact.Evidence.Strength == EvidenceStrength.Exact);

        static bool ValidType(TypeUseDefinition type) => !string.IsNullOrWhiteSpace(type.Name) && type.Shape.Count > 0 &&
            type.Shape[^1] == TypeUseShapeKind.Named && type.Shape.Take(type.Shape.Count - 1).All(kind => Enum.IsDefined(kind) && kind is not (TypeUseShapeKind.Unknown or TypeUseShapeKind.Named));

        static (string Name, string? Description, string? File)? EndpointHeader(GenerationFact fact) => fact switch
        {
            ArtifactFact artifact => (artifact.Definition.Name, artifact.Definition.Description, artifact.Definition.File),
            ArtifactDeclarationFact artifact => (artifact.Definition.Name, artifact.Definition.Description, artifact.Definition.File),
            _ => null
        };

        static bool ValidIntent(EntryPointInputTransferFact intent) => intent.Subject == intent.Definition.Method &&
            intent.Definition.Kind == RelationshipKind.Reads && intent.Evidence.Strength == EvidenceStrength.Exact;

        static bool ExactUsage(MethodArtifactUsageFact fact) => fact.Subject == fact.Definition.Method && fact.Evidence.Strength == EvidenceStrength.Exact;

        static bool ValidOrders(int?[] orders) => orders.Length == 1 && orders[0] is not < 0;

        static bool CompleteMember(Candidate candidate) => ValidMemberIdentity(candidate) &&
            candidate.Inputs.OfType<DeclarationInputMemberDeclarationFact>().Any() && candidate.Inputs.OfType<DeclarationInputMemberTypeUseFact>().Any();

        static bool ConsistentMember(int[] orders, DeclarationInputMemberTypeUseFact[] types) => orders.Length == 1 &&
            orders[0] >= 0 && types.Select(fact => Structural.TypeUse(fact.Definition.Type)).Distinct(StringComparer.Ordinal).Count() == 1;

        static bool SurfaceMember(GenerationFact fact, MethodArtifactUsageDefinition usage, HashSet<string> directNames) => DeclarationMember(fact) is { } member &&
            member.Declaration == usage.Declaration && member.Scope == usage.TargetScope && !directNames.Contains(member.Name);

        bool ValidEndpoint()
        {
            if (!DeclaredQuery())
            {
                Error(_endpointInputs, "Input transfer requires a declared query endpoint");
                return false;
            }

            if (_endpointInputs.Select(EndpointHeader).OfType<(string, string?, string?)>().Distinct().Count() > 1)
            {
                Error(_endpointInputs, "Endpoint declarations disagree on exact metadata", GenerationDiagnosticOutcome.Conflict);
                return false;
            }

            foreach (var intent in intents)
            {
                var entries = _facts.OfType<EntryPointFact>().Where(fact => fact.Definition.Method == intent.Definition.Method && fact.Definition.Artifact.Scope == endpoint.Scope).ToArray();
                if (!ValidIntent(intent) || !UniqueMapping(entries, intent.Definition.Method))
                {
                    Error([intent, .. entries], "Transfer intent has no unique exact entry-point mapping", GenerationDiagnosticOutcome.Conflict);
                    return false;
                }

                _endpointInputs = [.. _endpointInputs, .. entries];
            }

            return true;
        }

        bool DeclaredQuery() => endpoint.Kind == ArtifactKind.Query && _endpointInputs.Any(fact => fact is ArtifactFact or ArtifactDeclarationFact);

        bool UniqueMapping(EntryPointFact[] entries, SubjectId method) => entries.Length > 0 && entries.All(fact =>
            fact.Subject == method && fact.Definition.Artifact == endpoint && fact.Evidence.Strength == EvidenceStrength.Exact);

        IEnumerable<MethodArtifactUsageFact> RelevantUsages() => _facts.OfType<MethodArtifactUsageFact>().Where(fact =>
            fact.Definition.SourceScope == endpoint.Scope && intents.Any(intent => intent.Definition.Method == fact.Definition.Method && intent.Definition.Kind == fact.Definition.Kind));

        void Observe(MethodArtifactUsageFact[] usages, HashSet<string> directNames)
        {
            var usage = usages[0].Definition;
            var surface = _facts.Where(fact => SurfaceMember(fact, usage, directNames)).ToArray();
            if (surface.Length == 0)
            {
                return;
            }

            var orders = usages.Select(fact => fact.Definition.ExecutionOrder).Distinct().ToArray();
            if (!ValidOrders(orders) || !usages.All(ExactUsage))
            {
                Error(usages, "Execution observations disagree on exact ownership or semantic order", GenerationDiagnosticOutcome.Conflict);
                return;
            }

            var execution = Structural.WithScopes(Structural.SemanticKey("input-execution", usage.Method.Value, usage.Declaration.Value), usage.SourceScope, usage.TargetScope);
            foreach (var group in surface.GroupBy(fact => DeclarationMember(fact)!))
            {
                ObserveMember(new(execution, orders[0], group.Key, 0, new TypeUseDefinition { Name = string.Empty, Shape = [] }, [.. usages, .. group]));
            }
        }

        void ObserveMember(Candidate candidate)
        {
            var declarations = candidate.Inputs.OfType<DeclarationInputMemberDeclarationFact>().ToArray();
            var typeUses = candidate.Inputs.OfType<DeclarationInputMemberTypeUseFact>().ToArray();
            if (!CompleteMember(candidate))
            {
                Error(candidate.Inputs, "Transferred member requires exact declaration and type-use proof");
                return;
            }

            var orders = declarations.Select(fact => fact.Definition.DeclarationOrder).Distinct().ToArray();
            if (!ConsistentMember(orders, typeUses))
            {
                Error(candidate.Inputs, "Declaration input members disagree on semantic order or full type", GenerationDiagnosticOutcome.Conflict);
                return;
            }

            var type = typeUses[0].Definition.Type;
            if (!ValidType(type))
            {
                Error(candidate.Inputs, "Declaration input member has an incomplete type shape");
                return;
            }

            _candidates.Add(candidate with { MemberOrder = orders[0], Type = type });
        }

        bool ProveOrder()
        {
            var executions = _candidates.GroupBy(candidate => candidate.Execution, StringComparer.Ordinal).ToArray();
            var ambiguousExecution = executions.Length > 1 && (executions.Any(group => group.First().ExecutionOrder is null) || executions.Select(group => group.First().ExecutionOrder).Distinct().Count() != executions.Length);
            var ambiguousMembers = executions.Any(group => group.GroupBy(candidate => candidate.MemberOrder).Any(order => order.Select(candidate => candidate.Member.Name).Distinct(StringComparer.Ordinal).Count() > 1));
            if (ambiguousExecution || ambiguousMembers)
            {
                Error(_candidates.SelectMany(candidate => candidate.Inputs), "Input transfer has no unambiguous semantic precedence", GenerationDiagnosticOutcome.Conflict);
                return false;
            }

            return true;
        }

        GenerationFactRecord[] CreateMembers()
        {
            var selected = _candidates.OrderBy(candidate => candidate.ExecutionOrder ?? 0).ThenBy(candidate => candidate.MemberOrder).DistinctBy(candidate => candidate.Member.Name, StringComparer.Ordinal).ToArray();
            var records = new List<GenerationFactRecord>();
            var lastOrder = _endpointInputs.SelectMany(DirectOrders).DefaultIfEmpty(-1).Max();
            if ((long)lastOrder + selected.Length > int.MaxValue)
            {
                Error(_endpointInputs, "No semantic declaration order remains after direct endpoint members");
                return [];
            }

            for (var index = 0; index < selected.Length; index++)
            {
                records.AddRange(CreateMember(selected[index], lastOrder + index + 1));
            }

            return _diagnostics.TrueForAll(diagnostic => diagnostic.Severity != GenerationDiagnosticSeverity.Error) ? [.. records] : [];
        }

        GenerationFactRecord[] CreateMember(Candidate candidate, int order)
        {
            var member = new ArtifactMemberKey { Artifact = endpoint, Name = candidate.Member.Name };
            var (conceptOnly, proof) = MemberProof(candidate);
            var typeUse = new ArtifactMemberTypeUseFact
            {
                Id = candidate.Inputs.OfType<DeclarationInputMemberTypeUseFact>().OrderBy(fact => fact.Id.Value, StringComparer.Ordinal).First().Id,
                Subject = endpoint.Subject,
                Evidence = proof[0].Evidence,
                Definition = new ArtifactMemberTypeUseDefinition { Member = member, Type = candidate.Type }
            };
            var binding = TypeUseBindingDerivation.BindTransferredType(typeUse, baseFacts, conceptOnly);
            var omissions = binding.Diagnostic?.Code == GenerationDiagnosticCodes.MissingTypeUseTarget || (conceptOnly && binding.Target is null)
                ? DeclarationInputMemberTargetOmissions.For(candidate.Inputs)
                : [];
            if (omissions.Length > 0)
            {
                binding = (null, binding.Inputs, null);
                _diagnostics.AddRange(omissions.Select(omission => omission.Definition.Diagnostic));
            }

            if (binding.Diagnostic is { } diagnostic)
            {
                _diagnostics.Add(diagnostic with { Facts = [.. proof.Select(fact => fact.Id).Concat(diagnostic.Facts).Distinct().OrderBy(id => id.Value, StringComparer.Ordinal)] });
                return [];
            }

            proof = Inputs(proof.Concat(binding.Inputs));
            var declaration = new ArtifactMemberDeclarationFact
            {
                Id = Id(member, "declaration"),
                Subject = endpoint.Subject,
                Evidence = typeUse.Evidence,
                Definition = new ArtifactMemberDeclarationDefinition { Member = member, DeclarationOrder = order }
            };
            var outputTypeUse = typeUse with { Id = Id(member, "type") };
            List<GenerationFact> outputs = [declaration, outputTypeUse];
            outputs.AddRange(PolicyOutputs(outputTypeUse, member, conceptOnly));
            outputs.AddRange(omissions.Select(omission => DeclarationInputMemberTargetOmissions.Transfer(omission, typeUse, Id(member, $"omission:{omission.Id.Value}"))));
            if (binding.Target is { } target)
            {
                outputs.Add(new TypeUseBindingFact
                {
                    Id = Id(member, "binding"),
                    Subject = endpoint.Subject,
                    Evidence = typeUse.Evidence,
                    Definition = new TypeUseBindingDefinition { Member = member, Target = target }
                });
            }

            return [.. outputs.Select(fact => BackendScopeFactResolver.AttachScopeLineage(
                new GenerationFactRecord
                {
                    Fact = fact,
                    Lineage = new GenerationFactLineage { Producer = Rule, Inputs = [.. proof.Select(input => input.Id)], Evidence = [.. proof.Select(input => input.Evidence)] }
                },
                _facts))];
        }

        (bool ConceptOnly, GenerationFact[] Proof) MemberProof(Candidate candidate)
        {
            var observations = candidate.Inputs.OfType<DeclarationInputMemberTypeUseFact>().ToArray();
            var policies = _facts.OfType<TypeUseBindingPolicyFact>().Where(policy => observations.Any(observation => observation.Id == policy.Definition.TypeUse)).ToArray();
            return (observations.All(observation => TypeUseBindingPolicies.OptedIn(observation, _facts)), Inputs(_endpointInputs.Concat(candidate.Inputs).Concat(policies)));
        }

        void Error(IEnumerable<GenerationFact> facts, string message, GenerationDiagnosticOutcome outcome = GenerationDiagnosticOutcome.Unknown)
        {
            var inputs = Inputs(_endpointInputs.Concat(facts));
            _diagnostics.Add(new()
            {
                Code = GenerationDiagnosticCodes.InvalidEntryPointInputTransfer,
                Severity = GenerationDiagnosticSeverity.Error,
                Outcome = outcome,
                Message = message,
                Subject = endpoint.Subject,
                Facts = [.. inputs.Select(fact => fact.Id).Distinct()],
                Source = LoweringSupport.FirstSource(inputs.Select(fact => fact.Evidence))
            });
        }
    }
}

sealed record EntryPointInputMemberDerivationResult(ImmutableArray<FactId> Inputs, ImmutableArray<GenerationFactRecord> Facts, ImmutableArray<GenerationDiagnostic> Diagnostics);
