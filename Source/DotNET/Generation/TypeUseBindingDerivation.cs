// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation;

static class TypeUseBindingDerivation
{
    public static GenerationDerivationRuleIdentity Rule { get; } = new()
    {
        Id = "cratis.screenplay.type-use-binding",
        Version = "1.0.0"
    };

    public static TypeUseBindingDerivationResult Derive(ImmutableArray<GenerationFactRecord> baseFacts)
    {
        var facts = baseFacts.Select(record => record.Fact).ToArray();
        var index = new DeclarationIndex(ArtifactDeclarations(facts), MemberDeclarations(facts));
        var consideredInputs = facts
            .Where(fact => fact is ArtifactFact or
                           ArtifactDeclarationFact or
                           ArtifactMemberDeclarationFact or
                           ArtifactMemberTypeUseFact)
            .Select(fact => fact.Id)
            .OrderBy(id => id.Value, StringComparer.Ordinal)
            .ToImmutableArray();
        var derived = new List<GenerationFactRecord>();
        var diagnostics = new List<GenerationDiagnostic>();
        var typeUseGroups = facts
            .OfType<ArtifactMemberTypeUseFact>()
            .GroupBy(fact => Structural.ArtifactMemberKey(fact.Definition.Member), StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal);

        foreach (var group in typeUseGroups)
        {
            var outcome = DeriveBinding([.. group], index);
            if (outcome.Record is { } record)
            {
                derived.Add(record);
            }

            if (outcome.Diagnostic is { } diagnostic)
            {
                diagnostics.Add(diagnostic);
            }
        }

        return new TypeUseBindingDerivationResult(
            consideredInputs,
            [.. derived],
            [.. diagnostics.OrderBy(Canonical.Diagnostic, StringComparer.Ordinal)]);
    }

    static BindingOutcome DeriveBinding(ArtifactMemberTypeUseFact[] typeUses, DeclarationIndex index)
    {
        var context = new BindingContext(typeUses[0].Definition.Member, typeUses);
        if (ConflictingTypeUse(context) is { } typeUseConflict)
        {
            return BindingOutcome.Failed(typeUseConflict);
        }

        var typeUse = typeUses
            .OrderBy(fact => fact.Id.Value, StringComparer.Ordinal)
            .ThenBy(DerivationInputKey, StringComparer.Ordinal)
            .First();
        if (typeUse.Definition.Type.ObservedTypeSubject is not { } observedType)
        {
            return BindingOutcome.Skipped;
        }

        var owner = ResolveOwner(context, index);
        if (owner.Failure is { } ownerFailure)
        {
            return BindingOutcome.Failed(ownerFailure);
        }

        var memberDeclarations = ResolveMember(context, owner.Value, index);
        if (memberDeclarations.Failure is { } memberFailure)
        {
            return BindingOutcome.Failed(memberFailure);
        }

        var target = ResolveTarget(context, observedType, owner.Value, index);
        if (target.Failure is { } targetFailure)
        {
            return BindingOutcome.Failed(targetFailure);
        }

        return BindingOutcome.Bound(CreateRecord(
            context,
            typeUse,
            target.Value,
            owner.Value.Select(declaration => declaration.Fact)
                .Concat(memberDeclarations.Value.Select(declaration => declaration.Fact))));
    }

    static GenerationDiagnostic? ConflictingTypeUse(BindingContext context)
    {
        var typeVariants = context.TypeUses
            .Select(fact => Structural.TypeUse(fact.Definition.Type))
            .Distinct(StringComparer.Ordinal)
            .Count();
        return typeVariants > 1
            ? context.Diagnostic(
                GenerationDiagnosticCodes.ConflictingMemberTypeUse,
                GenerationDiagnosticOutcome.Conflict,
                [],
                $"Incompatible exact type uses were asserted for member '{context.Member.Name}'")
            : null;
    }

    static Resolution<DeclaredArtifact[]> ResolveOwner(BindingContext context, DeclarationIndex index)
    {
        var owners = index.Artifacts
            .Where(declaration => declaration.Key == context.Member.Artifact)
            .ToArray();
        if (owners.Length == 0)
        {
            return Resolution<DeclaredArtifact[]>.Fail(context.Diagnostic(
                GenerationDiagnosticCodes.MissingTypeUseOwner,
                GenerationDiagnosticOutcome.Unknown,
                [],
                $"Artifact owner '{context.Member.Artifact.Subject.Value}' was not declared for member '{context.Member.Name}'"));
        }

        return HasConflictingDeclarations(owners)
            ? Resolution<DeclaredArtifact[]>.Fail(context.Diagnostic(
                GenerationDiagnosticCodes.ConflictingTypeUseDeclaration,
                GenerationDiagnosticOutcome.Conflict,
                owners.Select(declaration => declaration.Fact),
                $"Incompatible declarations were asserted for artifact owner '{context.Member.Artifact.Subject.Value}'"))
            : Resolution<DeclaredArtifact[]>.Succeed(owners);
    }

    static Resolution<DeclaredMember[]> ResolveMember(
        BindingContext context,
        DeclaredArtifact[] owners,
        DeclarationIndex index)
    {
        var memberDeclarations = index.Members
            .Where(declaration => declaration.Member == context.Member)
            .ToArray();
        if (memberDeclarations.Length == 0)
        {
            return Resolution<DeclaredMember[]>.Fail(context.Diagnostic(
                GenerationDiagnosticCodes.MissingTypeUseMember,
                GenerationDiagnosticOutcome.Unknown,
                owners.Select(declaration => declaration.Fact),
                $"Member '{context.Member.Name}' was not declared on artifact owner '{context.Member.Artifact.Subject.Value}'"));
        }

        return memberDeclarations.Select(declaration => declaration.Order).Distinct().Count() > 1
            ? Resolution<DeclaredMember[]>.Fail(context.Diagnostic(
                GenerationDiagnosticCodes.ConflictingTypeUseDeclaration,
                GenerationDiagnosticOutcome.Conflict,
                memberDeclarations.Select(declaration => declaration.Fact),
                $"Incompatible declaration orders were asserted for member '{context.Member.Name}'"))
            : Resolution<DeclaredMember[]>.Succeed(memberDeclarations);
    }

    static Resolution<DeclaredArtifact[]> ResolveTarget(
        BindingContext context,
        SubjectId observedType,
        DeclaredArtifact[] owners,
        DeclarationIndex index)
    {
        var legacyTargets = LegacyTargets(context.Member, observedType, owners);
        if (legacyTargets.Length > 1)
        {
            return Resolution<DeclaredArtifact[]>.Fail(context.Diagnostic(
                GenerationDiagnosticCodes.ConflictingTypeUseTarget,
                GenerationDiagnosticOutcome.Conflict,
                owners.Select(declaration => declaration.Fact),
                $"Artifact member '{context.Member.Name}' retains incompatible exact legacy target roles"));
        }

        var targetDeclarations = index.Artifacts
            .Where(declaration => declaration.Key.Subject == observedType &&
                                  (legacyTargets.Length == 0 || declaration.Key == legacyTargets[0]))
            .ToArray();
        if (targetDeclarations.Length == 0)
        {
            return Resolution<DeclaredArtifact[]>.Fail(context.Diagnostic(
                GenerationDiagnosticCodes.MissingTypeUseTarget,
                GenerationDiagnosticOutcome.Unknown,
                [],
                $"Observed type subject '{observedType.Value}' has no declared artifact target"));
        }

        var targetKeys = targetDeclarations
            .GroupBy(declaration => Structural.ArtifactKey(declaration.Key), StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToArray();
        if (targetKeys.Length > 1)
        {
            return Resolution<DeclaredArtifact[]>.Fail(context.Diagnostic(
                GenerationDiagnosticCodes.ConflictingTypeUseTarget,
                GenerationDiagnosticOutcome.Conflict,
                targetDeclarations.Select(declaration => declaration.Fact),
                $"Observed type subject '{observedType.Value}' declares incompatible artifact targets"));
        }

        var exactTargetDeclarations = targetKeys[0].ToArray();
        return HasConflictingDeclarations(exactTargetDeclarations)
            ? Resolution<DeclaredArtifact[]>.Fail(context.Diagnostic(
                GenerationDiagnosticCodes.ConflictingTypeUseDeclaration,
                GenerationDiagnosticOutcome.Conflict,
                exactTargetDeclarations.Select(declaration => declaration.Fact),
                $"Observed type subject '{observedType.Value}' has incompatible declarations"))
            : Resolution<DeclaredArtifact[]>.Succeed(exactTargetDeclarations);
    }

    static ArtifactKey[] LegacyTargets(ArtifactMemberKey member, SubjectId observedType, DeclaredArtifact[] owners) =>
    [
        .. owners
            .SelectMany(declaration => declaration.Fact is ArtifactFact artifact
                ? artifact.Definition.Properties
                    .Where(property => property.Name == member.Name &&
                                       property.Type.Subject == observedType &&
                                       property.Type.TargetArtifactKind is not null)
                    .Select(property => new ArtifactKey
                    {
                        Subject = observedType,
                        Kind = property.Type.TargetArtifactKind!.Value
                    })
                : [])
            .Distinct()
    ];

    static GenerationFactRecord CreateRecord(
        BindingContext context,
        ArtifactMemberTypeUseFact typeUse,
        DeclaredArtifact[] targets,
        IEnumerable<GenerationFact> declarations)
    {
        var inputs = CanonicalInputs(
            context.TypeUses
                .Cast<GenerationFact>()
                .Concat(declarations)
                .Concat(targets.Select(declaration => declaration.Fact)));
        var target = targets[0].Key;
        var fact = new TypeUseBindingFact
        {
            Id = DerivedId(context.Member, target),
            Subject = context.Member.Artifact.Subject,
            Evidence = typeUse.Evidence,
            Definition = new TypeUseBindingDefinition
            {
                Member = context.Member,
                Target = target
            }
        };
        return new GenerationFactRecord
        {
            Fact = fact,
            Lineage = new GenerationFactLineage
            {
                Producer = Rule,
                Inputs = [.. inputs.Select(input => input.Id)],
                Evidence = [.. inputs.Select(input => input.Evidence)]
            }
        };
    }

    static IReadOnlyList<DeclaredArtifact> ArtifactDeclarations(IEnumerable<GenerationFact> facts) =>
    [
        .. facts.SelectMany<GenerationFact, DeclaredArtifact>(fact => fact switch
        {
            ArtifactFact artifact =>
            [
                new DeclaredArtifact(
                    artifact.Definition.Key,
                    DeclarationKey(
                        artifact.Definition.Key,
                        artifact.Definition.Name,
                        artifact.Definition.Description,
                        artifact.Definition.File),
                    artifact)
            ],
            ArtifactDeclarationFact declaration =>
            [
                new DeclaredArtifact(
                    declaration.Definition.Artifact,
                    DeclarationKey(
                        declaration.Definition.Artifact,
                        declaration.Definition.Name,
                        declaration.Definition.Description,
                        declaration.Definition.File),
                    declaration)
            ],
            _ => []
        })
    ];

    static IReadOnlyList<DeclaredMember> MemberDeclarations(IEnumerable<GenerationFact> facts) =>
    [
        .. facts.SelectMany(fact => fact switch
        {
            ArtifactFact artifact => artifact.Definition.Properties.Select((property, order) =>
                new DeclaredMember(
                    new ArtifactMemberKey { Artifact = artifact.Definition.Key, Name = property.Name },
                    order,
                    artifact)),
            ArtifactMemberDeclarationFact member =>
            [new DeclaredMember(member.Definition.Member, member.Definition.DeclarationOrder, member)],
            _ => []
        })
    ];

    static bool HasConflictingDeclarations(IEnumerable<DeclaredArtifact> declarations) =>
        declarations.Select(declaration => declaration.Declaration).Distinct(StringComparer.Ordinal).Count() > 1;

    static string DeclarationKey(
        ArtifactKey artifact,
        string name,
        string? description,
        string? file) =>
        Structural.ArtifactDeclaration(new ArtifactDeclarationDefinition
        {
            Artifact = artifact,
            Name = name,
            Description = description,
            File = file
        });

    static GenerationFact[] CanonicalInputs(IEnumerable<GenerationFact> inputs) =>
    [
        .. inputs
            .GroupBy(input => input.Id.Value, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => group
                .OrderBy(DerivationInputKey, StringComparer.Ordinal)
                .First())
    ];

    static string DerivationInputKey(GenerationFact fact)
    {
        var source = fact.Evidence.Source;
        return Structural.SemanticKey(
            "derivation-input",
            fact.Subject.Value,
            Structural.FactFamily(fact).ToString(System.Globalization.CultureInfo.InvariantCulture),
            Structural.FactDefinition(fact),
            ((int)fact.Evidence.Strength).ToString(System.Globalization.CultureInfo.InvariantCulture),
            source?.FileIdentity?.Project,
            source?.FileIdentity?.Path,
            source?.Path,
            source?.StartLine.ToString(System.Globalization.CultureInfo.InvariantCulture),
            source?.StartColumn.ToString(System.Globalization.CultureInfo.InvariantCulture),
            source?.EndLine.ToString(System.Globalization.CultureInfo.InvariantCulture),
            source?.EndColumn.ToString(System.Globalization.CultureInfo.InvariantCulture),
            fact.Evidence.Explanation);
    }

    static FactId DerivedId(ArtifactMemberKey member, ArtifactKey target) => new()
    {
        Value = string.Join(
            ':',
            "generation",
            "derive",
            "type-use-binding",
            "1",
            Escape(member.Artifact.Subject.Value),
            ((int)member.Artifact.Kind).ToString(System.Globalization.CultureInfo.InvariantCulture),
            Escape(member.Name),
            Escape(target.Subject.Value),
            ((int)target.Kind).ToString(System.Globalization.CultureInfo.InvariantCulture))
    };

    static string Escape(string value) => string.Concat(
        value.Select(character => ((int)character).ToString("X4", System.Globalization.CultureInfo.InvariantCulture)));

    sealed record DeclarationIndex(IReadOnlyList<DeclaredArtifact> Artifacts, IReadOnlyList<DeclaredMember> Members);

    sealed record BindingContext(ArtifactMemberKey Member, ArtifactMemberTypeUseFact[] TypeUses)
    {
        public GenerationDiagnostic Diagnostic(
            string code,
            GenerationDiagnosticOutcome outcome,
            IEnumerable<GenerationFact> declarations,
            string message) =>
            Create(code, outcome, declarations.Concat(TypeUses), message);

        GenerationDiagnostic Create(
            string code,
            GenerationDiagnosticOutcome outcome,
            IEnumerable<GenerationFact> inputs,
            string message)
        {
            var canonicalInputs = CanonicalInputs(inputs);
            var identities = string.Join(", ", canonicalInputs.Select(input => $"'{input.Id.Value}'"));
            return new GenerationDiagnostic
            {
                Code = code,
                Severity = GenerationDiagnosticSeverity.Error,
                Outcome = outcome,
                Message = $"{message}. Input facts: {identities}",
                Facts = [.. canonicalInputs.Select(input => input.Id)],
                Source = canonicalInputs.FirstOrDefault()?.Evidence.Source,
                Subject = Member.Artifact.Subject
            };
        }
    }

    sealed record Resolution<T>(T Value, GenerationDiagnostic? Failure)
    {
        public static Resolution<T> Succeed(T value) => new(value, null);

        public static Resolution<T> Fail(GenerationDiagnostic failure) => new(default!, failure);
    }

    sealed record BindingOutcome(GenerationFactRecord? Record, GenerationDiagnostic? Diagnostic)
    {
        public static BindingOutcome Skipped { get; } = new(null, null);

        public static BindingOutcome Bound(GenerationFactRecord record) => new(record, null);

        public static BindingOutcome Failed(GenerationDiagnostic diagnostic) => new(null, diagnostic);
    }

    sealed record DeclaredArtifact(ArtifactKey Key, string Declaration, GenerationFact Fact);

    sealed record DeclaredMember(ArtifactMemberKey Member, int Order, GenerationFact Fact);
}

sealed record TypeUseBindingDerivationResult(
    ImmutableArray<FactId> Inputs,
    ImmutableArray<GenerationFactRecord> Facts,
    ImmutableArray<GenerationDiagnostic> Diagnostics);
