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
        var declarations = ArtifactDeclarations(facts);
        var members = MemberDeclarations(facts);
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
            DeriveBinding([.. group], declarations, members, derived, diagnostics);
        }

        return new TypeUseBindingDerivationResult(
            consideredInputs,
            [.. derived],
            [.. diagnostics.OrderBy(Canonical.Diagnostic, StringComparer.Ordinal)]);
    }

    static void DeriveBinding(
        ArtifactMemberTypeUseFact[] typeUses,
        IReadOnlyList<DeclaredArtifact> declarations,
        IReadOnlyList<DeclaredMember> members,
        List<GenerationFactRecord> derived,
        List<GenerationDiagnostic> diagnostics)
    {
        var member = typeUses[0].Definition.Member;
        var typeVariants = typeUses
            .GroupBy(fact => Structural.TypeUse(fact.Definition.Type), StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToArray();
        if (typeVariants.Length > 1)
        {
            diagnostics.Add(Diagnostic(
                GenerationDiagnosticCodes.ConflictingMemberTypeUse,
                GenerationDiagnosticOutcome.Conflict,
                member,
                typeUses,
                $"Incompatible exact type uses were asserted for member '{member.Name}'"));
            return;
        }

        var typeUse = typeVariants[0]
            .OrderBy(fact => fact.Id.Value, StringComparer.Ordinal)
            .ThenBy(DerivationInputKey, StringComparer.Ordinal)
            .First();
        if (typeUse.Definition.Type.ObservedTypeSubject is not { } observedType)
        {
            return;
        }

        var ownerDeclarations = declarations
            .Where(declaration => declaration.Key == member.Artifact)
            .ToArray();
        if (ownerDeclarations.Length == 0)
        {
            diagnostics.Add(Diagnostic(
                GenerationDiagnosticCodes.MissingTypeUseOwner,
                GenerationDiagnosticOutcome.Unknown,
                member,
                typeUses,
                $"Artifact owner '{member.Artifact.Subject.Value}' was not declared for member '{member.Name}'"));
            return;
        }

        if (HasConflictingDeclarations(ownerDeclarations))
        {
            diagnostics.Add(Diagnostic(
                GenerationDiagnosticCodes.ConflictingTypeUseDeclaration,
                GenerationDiagnosticOutcome.Conflict,
                member,
                ownerDeclarations.Select(declaration => declaration.Fact).Concat(typeUses),
                $"Incompatible declarations were asserted for artifact owner '{member.Artifact.Subject.Value}'"));
            return;
        }

        var memberDeclarations = members
            .Where(declaration => declaration.Member == member)
            .ToArray();
        if (memberDeclarations.Length == 0)
        {
            diagnostics.Add(Diagnostic(
                GenerationDiagnosticCodes.MissingTypeUseMember,
                GenerationDiagnosticOutcome.Unknown,
                member,
                ownerDeclarations.Select(declaration => declaration.Fact).Concat(typeUses),
                $"Member '{member.Name}' was not declared on artifact owner '{member.Artifact.Subject.Value}'"));
            return;
        }

        if (memberDeclarations.Select(declaration => declaration.Order).Distinct().Count() > 1)
        {
            diagnostics.Add(Diagnostic(
                GenerationDiagnosticCodes.ConflictingTypeUseDeclaration,
                GenerationDiagnosticOutcome.Conflict,
                member,
                memberDeclarations.Select(declaration => declaration.Fact).Concat(typeUses),
                $"Incompatible declaration orders were asserted for member '{member.Name}'"));
            return;
        }

        var legacyTargets = ownerDeclarations
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
            .ToArray();
        if (legacyTargets.Length > 1)
        {
            diagnostics.Add(Diagnostic(
                GenerationDiagnosticCodes.ConflictingTypeUseTarget,
                GenerationDiagnosticOutcome.Conflict,
                member,
                ownerDeclarations.Select(declaration => declaration.Fact).Concat(typeUses),
                $"Artifact member '{member.Name}' retains incompatible exact legacy target roles"));
            return;
        }

        var targetDeclarations = declarations
            .Where(declaration => declaration.Key.Subject == observedType &&
                                  (legacyTargets.Length == 0 || declaration.Key == legacyTargets[0]))
            .ToArray();
        if (targetDeclarations.Length == 0)
        {
            diagnostics.Add(Diagnostic(
                GenerationDiagnosticCodes.MissingTypeUseTarget,
                GenerationDiagnosticOutcome.Unknown,
                member,
                typeUses,
                $"Observed type subject '{observedType.Value}' has no declared artifact target"));
            return;
        }

        var targetKeys = targetDeclarations
            .GroupBy(declaration => Structural.ArtifactKey(declaration.Key), StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToArray();
        if (targetKeys.Length > 1)
        {
            diagnostics.Add(Diagnostic(
                GenerationDiagnosticCodes.ConflictingTypeUseTarget,
                GenerationDiagnosticOutcome.Conflict,
                member,
                targetDeclarations.Select(declaration => declaration.Fact).Concat(typeUses),
                $"Observed type subject '{observedType.Value}' declares incompatible artifact targets"));
            return;
        }

        var exactTargetDeclarations = targetKeys[0].ToArray();
        if (HasConflictingDeclarations(exactTargetDeclarations))
        {
            diagnostics.Add(Diagnostic(
                GenerationDiagnosticCodes.ConflictingTypeUseDeclaration,
                GenerationDiagnosticOutcome.Conflict,
                member,
                exactTargetDeclarations.Select(declaration => declaration.Fact).Concat(typeUses),
                $"Observed type subject '{observedType.Value}' has incompatible declarations"));
            return;
        }

        var inputs = CanonicalInputs(
            typeUses
                .Cast<GenerationFact>()
                .Concat(ownerDeclarations.Select(declaration => declaration.Fact))
                .Concat(memberDeclarations.Select(declaration => declaration.Fact))
                .Concat(exactTargetDeclarations.Select(declaration => declaration.Fact)));
        var target = exactTargetDeclarations[0].Key;
        var fact = new TypeUseBindingFact
        {
            Id = DerivedId(member, target),
            Subject = member.Artifact.Subject,
            Evidence = typeUse.Evidence,
            Definition = new TypeUseBindingDefinition
            {
                Member = member,
                Target = target
            }
        };
        derived.Add(new GenerationFactRecord
        {
            Fact = fact,
            Lineage = new GenerationFactLineage
            {
                Producer = Rule,
                Inputs = [.. inputs.Select(input => input.Id)],
                Evidence = [.. inputs.Select(input => input.Evidence)]
            }
        });
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
        .. facts.SelectMany<GenerationFact, DeclaredMember>(fact => fact switch
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

    static GenerationDiagnostic Diagnostic(
        string code,
        GenerationDiagnosticOutcome outcome,
        ArtifactMemberKey member,
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
            Source = canonicalInputs.FirstOrDefault()?.Evidence.Source,
            Subject = member.Artifact.Subject
        };
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

    sealed record DeclaredArtifact(ArtifactKey Key, string Declaration, GenerationFact Fact);

    sealed record DeclaredMember(ArtifactMemberKey Member, int Order, GenerationFact Fact);
}

sealed record TypeUseBindingDerivationResult(
    ImmutableArray<FactId> Inputs,
    ImmutableArray<GenerationFactRecord> Facts,
    ImmutableArray<GenerationDiagnostic> Diagnostics);
