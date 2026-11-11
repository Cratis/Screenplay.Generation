// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Cratis.Screenplay.Generation;

static class GranularArtifactResolver
{
    public static ArtifactFact[] Resolve(
        IReadOnlyList<GenerationFact> facts,
        IReadOnlyList<GenerationFact> rejectedFacts,
        List<GenerationDiagnostic> diagnostics)
    {
        var blockedArtifacts = rejectedFacts
            .Select(fact => new { Fact = fact, Member = MemberFor(fact) })
            .Where(item => item.Member is not null && item.Fact.Subject == item.Member.Artifact.Subject)
            .Select(item => item.Member!.Artifact)
            .ToHashSet();
        var legacy = facts.OfType<ArtifactFact>().ToArray();
        var granular = ValidGranularFacts(facts.Where(IsGranularArtifactFact), diagnostics);
        if (granular.Length == 0)
        {
            return [.. legacy.Where(fact => !blockedArtifacts.Contains(fact.Definition.Key))];
        }

        var granularDeclarations = granular.OfType<ArtifactDeclarationFact>().ToArray();
        var declaredArtifacts = legacy.Select(fact => fact.Definition.Key)
            .Concat(granularDeclarations.Select(fact => fact.Definition.Artifact))
            .Distinct()
            .ToHashSet();
        var variants = DeclarationVariants(legacy, granularDeclarations);
        var effective = new List<ArtifactFact>();
        foreach (var variant in variants
                     .OrderBy(candidate => Structural.ArtifactKey(candidate.Definition.Key), StringComparer.Ordinal)
                     .ThenBy(candidate => Structural.Artifact(candidate.Definition), StringComparer.Ordinal))
        {
            if (blockedArtifacts.Contains(variant.Definition.Key))
            {
                continue;
            }

            var overlaid = ApplyMembers(variant, granular, declaredArtifacts, diagnostics);
            if (overlaid is not null)
            {
                effective.AddRange(EffectiveFacts(overlaid));
            }
        }

        return [.. effective];
    }

    internal static GenerationFact[] ValidFactsForDerivation(
        IEnumerable<GenerationFact> facts,
        List<GenerationDiagnostic> diagnostics)
    {
        var input = facts.ToArray();
        return
        [
            .. input.Where(fact => !IsGranularArtifactFact(fact)),
            .. ValidGranularFacts(input.Where(IsGranularArtifactFact), diagnostics)
        ];
    }

    static IEnumerable<ArtifactFact> EffectiveFacts(ArtifactVariant overlaid) =>
        CanonicalFacts(overlaid.Supports).Select(support => new ArtifactFact
        {
            Id = support.Id,
            Subject = overlaid.Definition.Key.Subject,
            Evidence = support.Evidence,
            Definition = overlaid.Definition with
            {
                File = RealizationFileFor(support) ?? overlaid.Definition.File
            }
        });

    static List<ArtifactVariant> DeclarationVariants(
        IEnumerable<ArtifactFact> legacy,
        IEnumerable<ArtifactDeclarationFact> granular)
    {
        var variants = legacy
            .GroupBy(fact => Structural.Artifact(fact.Definition), StringComparer.Ordinal)
            .Select(group =>
            {
                var ordered = group
                    .OrderBy(fact => Canonical.Artifact(fact.Definition), StringComparer.Ordinal)
                    .ThenBy(fact => fact.Id.Value, StringComparer.Ordinal)
                    .ToArray();
                return new ArtifactVariant(ordered[0].Definition, [.. ordered]);
            })
            .ToList();
        foreach (var declaration in granular.OrderBy(fact => fact.Id.Value, StringComparer.Ordinal))
        {
            var metadata = DeclarationKey(declaration.Definition);
            var matching = variants
                .Where(variant => DeclarationKey(variant.Definition) == metadata)
                .ToArray();
            if (matching.Length == 0)
            {
                variants.Add(new ArtifactVariant(
                    new ArtifactDefinition
                    {
                        Key = declaration.Definition.Artifact,
                        Name = declaration.Definition.Name,
                        Description = declaration.Definition.Description,
                        File = declaration.Definition.File
                    },
                    [declaration]));
                continue;
            }

            foreach (var variant in matching)
            {
                variant.Supports.Add(declaration);
            }
        }

        return variants;
    }

    static ArtifactVariant? ApplyMembers(
        ArtifactVariant variant,
        IReadOnlyList<GenerationFact> granular,
        HashSet<ArtifactKey> declaredArtifacts,
        List<GenerationDiagnostic> diagnostics)
    {
        var key = variant.Definition.Key;
        var memberFacts = granular
            .Where(fact => MemberFor(fact)?.Artifact == key)
            .ToArray();
        if (memberFacts.Length == 0)
        {
            return variant;
        }

        var properties = variant.Definition.Properties
            .Select((property, order) => new OrderedProperty(order, property))
            .ToDictionary(item => item.Property.Name, StringComparer.Ordinal);
        var supports = variant.Supports.ToList();
        var failed = false;
        var memberNames = variant.Definition.Properties.Select(property => property.Name)
            .Concat(memberFacts.Select(fact => MemberFor(fact)!.Name))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal);
        foreach (var memberName in memberNames)
        {
            properties.TryGetValue(memberName, out var existing);
            var member = new MemberContext(
                variant,
                memberName,
                [.. memberFacts.Where(fact => MemberFor(fact)!.Name == memberName)],
                existing,
                declaredArtifacts,
                diagnostics);
            var resolved = ResolveMember(member);
            if (resolved is null)
            {
                failed = true;
                continue;
            }

            properties[memberName] = resolved;
            supports.AddRange(member.Facts);
        }

        failed |= ReportDuplicateOrders(variant, memberFacts, properties.Values, diagnostics);
        return failed ? null : WithProperties(variant, properties.Values, supports);
    }

    static OrderedProperty? ResolveMember(MemberContext member)
    {
        if (!TryDeclarationOrder(member, out var order) ||
            !TryResolveType(member, out var typeUses))
        {
            return null;
        }

        var bound = BindTarget(member, typeUses);
        if (bound is null || !TryIdentifierRole(member, out var isIdentifier))
        {
            return null;
        }

        return new OrderedProperty(
            order,
            new PropertyDefinition
            {
                Name = member.Name,
                Type = bound,
                IsIdentifier = isIdentifier
            });
    }

    static bool TryDeclarationOrder(MemberContext member, out int order)
    {
        var orders = member.Facts.OfType<ArtifactMemberDeclarationFact>()
            .Select(fact => fact.Definition.DeclarationOrder)
            .Concat(member.Existing is null ? [] : [member.Existing.Order])
            .Distinct()
            .ToArray();
        order = orders.FirstOrDefault();
        if (orders.Length == 0)
        {
            return member.Incomplete(member.Facts, "has no declaration order");
        }

        return orders.Length == 1 ||
               member.Conflict(member.Facts.Concat(member.Variant.Supports), "has incompatible declaration orders");
    }

    static bool TryResolveType(MemberContext member, [NotNullWhen(true)] out MemberTypeUses? typeUses)
    {
        var typeUseFacts = member.Facts.OfType<ArtifactMemberTypeUseFact>().ToArray();
        var typeUseVariants = typeUseFacts
            .GroupBy(fact => Structural.TypeUse(fact.Definition.Type), StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToArray();
        typeUses = null;
        if (typeUseVariants.Length > 1)
        {
            return member.Conflict(typeUseFacts, "has incompatible exact type uses");
        }

        var selected = typeUseVariants.Length == 1 ? typeUseVariants[0] : null;
        var candidate = new MemberTypeUses(typeUseFacts, selected, member.Existing?.Property.Type);
        if (selected is not null)
        {
            var overlaid = OverlayTypeUse(member, candidate);
            if (overlaid is null)
            {
                return false;
            }

            candidate = candidate with { Type = overlaid };
        }

        if (candidate.Type is null)
        {
            return member.Incomplete(member.Facts, "has no exact type use");
        }

        typeUses = candidate;
        return true;
    }

    static TypeReferenceDefinition? OverlayTypeUse(MemberContext member, MemberTypeUses typeUses)
    {
        var current = typeUses.Type;
        var typeUse = typeUses.Selected!;
        if (!TryTypeReference(typeUse.Definition.Type, out var granularType))
        {
            member.Fail(
                GenerationDiagnosticCodes.UnsupportedTypeUseShape,
                GenerationDiagnosticOutcome.Unsupported,
                typeUses.Variant!,
                $"uses exact shape '{Shape(typeUse.Definition.Type)}' that Screenplay cannot represent");
            return null;
        }

        if (current is not null && !SameUnboundType(current, granularType))
        {
            member.Conflict(
                typeUses.Variant!.Cast<GenerationFact>().Concat(member.Variant.Supports),
                "has incompatible legacy and granular type uses");
            return null;
        }

        return granularType with
        {
            Subject = current?.Subject,
            TargetArtifactKind = current?.TargetArtifactKind
        };
    }

    static TypeReferenceDefinition? BindTarget(MemberContext member, MemberTypeUses typeUses)
    {
        var type = typeUses.Type!;
        var bindings = member.Facts.OfType<TypeUseBindingFact>().ToArray();
        var bindingVariants = bindings
            .GroupBy(fact => Structural.ArtifactKey(fact.Definition.Target), StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToArray();
        if (bindingVariants.Length > 1)
        {
            member.Conflict(bindings, "has incompatible exact type bindings");
            return null;
        }

        if (bindingVariants.Length == 1)
        {
            return ApplyBinding(member, bindingVariants[0], typeUses);
        }

        if (typeUses.Selected?.Definition.Type.ObservedTypeSubject is not null && type.Subject is null)
        {
            member.Incomplete(
                typeUses.Facts.Concat(member.Variant.Supports),
                "has an observed exact type subject but no admitted binding");
            return null;
        }

        return type;
    }

    static TypeReferenceDefinition? ApplyBinding(
        MemberContext member,
        IGrouping<string, TypeUseBindingFact> bindingVariant,
        MemberTypeUses typeUses)
    {
        var type = typeUses.Type!;
        var binding = bindingVariant.OrderBy(fact => fact.Id.Value, StringComparer.Ordinal).First();
        var target = binding.Definition.Target;
        if (!member.Declared.Contains(target))
        {
            member.Fail(
                GenerationDiagnosticCodes.MissingTypeUseBindingTarget,
                GenerationDiagnosticOutcome.Unknown,
                bindingVariant,
                $"binds undeclared target role '{target.Kind}' for subject '{target.Subject.Value}'");
            return null;
        }

        if (ConflictsWithTarget(type, typeUses.Selected, target))
        {
            member.Conflict(
                bindingVariant.Cast<GenerationFact>().Concat(typeUses.Facts).Concat(member.Variant.Supports),
                "binds incompatible exact type subjects");
            return null;
        }

        return type with
        {
            Subject = target.Subject,
            TargetArtifactKind = target.Kind
        };
    }

    static bool ConflictsWithTarget(
        TypeReferenceDefinition type,
        ArtifactMemberTypeUseFact? typeUse,
        ArtifactKey target) =>
        (type.Subject is not null && type.Subject != target.Subject) ||
        (type.TargetArtifactKind is not null && type.TargetArtifactKind != target.Kind) ||
        (typeUse?.Definition.Type.ObservedTypeSubject is not null && typeUse.Definition.Type.ObservedTypeSubject != target.Subject);

    static bool TryIdentifierRole(MemberContext member, out bool isIdentifier)
    {
        isIdentifier = false;
        var roleFacts = member.Facts.OfType<ArtifactMemberRoleFact>().ToArray();
        var roleVariants = roleFacts.Select(fact => fact.Definition.Role).Distinct().ToArray();
        if (roleVariants.Length > 1)
        {
            return member.Conflict(roleFacts, "has incompatible identifier roles");
        }

        isIdentifier = member.Existing?.Property.IsIdentifier == true ||
                       roleVariants.SingleOrDefault() == ArtifactMemberRoleKind.EventSourceIdentifier;
        return true;
    }

    static bool ReportDuplicateOrders(
        ArtifactVariant variant,
        GenerationFact[] memberFacts,
        IEnumerable<OrderedProperty> properties,
        List<GenerationDiagnostic> diagnostics)
    {
        var key = variant.Definition.Key;
        var duplicateOrders = properties
            .GroupBy(property => property.Order)
            .Where(group => group.Count() > 1)
            .ToArray();
        foreach (var duplicateOrder in duplicateOrders.OrderBy(group => group.Key))
        {
            var duplicateNames = duplicateOrder
                .Select(property => property.Property.Name)
                .ToHashSet(StringComparer.Ordinal);
            var involvedFacts = memberFacts
                .Where(fact => duplicateNames.Contains(MemberFor(fact)!.Name))
                .Concat(variant.Supports.OfType<ArtifactFact>());
            AddDiagnostic(
                diagnostics,
                GenerationDiagnosticCodes.ConflictingArtifactMember,
                GenerationDiagnosticOutcome.Conflict,
                key.Subject,
                involvedFacts,
                $"Artifact '{key.Subject.Value}' has multiple members at declaration order {duplicateOrder.Key}");
        }

        return duplicateOrders.Length > 0;
    }

    static ArtifactVariant WithProperties(
        ArtifactVariant variant,
        IEnumerable<OrderedProperty> properties,
        IEnumerable<GenerationFact> supports) => new(
            variant.Definition with
            {
                Properties =
                [
                    .. properties
                        .OrderBy(property => property.Order)
                        .ThenBy(property => property.Property.Name, StringComparer.Ordinal)
                        .Select(property => property.Property)
                ]
            },
            [.. CanonicalFacts(supports)]);

    static bool TryTypeReference(TypeUseDefinition type, out TypeReferenceDefinition reference)
    {
        var shape = type.Shape;
        var isCollection = false;
        var isOptional = false;
        var supported = true;
        switch (shape)
        {
            case [TypeUseShapeKind.Named]:
                break;
            case [TypeUseShapeKind.Optional, TypeUseShapeKind.Named]:
                isOptional = true;
                break;
            case [TypeUseShapeKind.Collection, TypeUseShapeKind.Named]:
                isCollection = true;
                break;
            case [TypeUseShapeKind.Optional, TypeUseShapeKind.Collection, TypeUseShapeKind.Named]:
                isOptional = true;
                isCollection = true;
                break;
            default:
                supported = false;
                break;
        }
        reference = new TypeReferenceDefinition
        {
            Name = type.Name,
            IsCollection = isCollection,
            IsOptional = isOptional
        };
        return supported;
    }

    static bool SameUnboundType(TypeReferenceDefinition first, TypeReferenceDefinition second) =>
        first.Name == second.Name &&
        first.IsCollection == second.IsCollection &&
        first.IsOptional == second.IsOptional;

    static GenerationFact[] ValidGranularFacts(
        IEnumerable<GenerationFact> facts,
        List<GenerationDiagnostic> diagnostics)
    {
        var valid = new List<GenerationFact>();
        foreach (var fact in facts.OrderBy(fact => fact.Id.Value, StringComparer.Ordinal))
        {
            var owner = fact switch
            {
                ArtifactDeclarationFact declaration => declaration.Definition.Artifact.Subject,
                _ => MemberFor(fact)?.Artifact.Subject
            };
            if (owner == fact.Subject)
            {
                valid.Add(fact);
                continue;
            }

            diagnostics.Add(new GenerationDiagnostic
            {
                Code = GenerationDiagnosticCodes.InvalidGranularFactOwnership,
                Severity = GenerationDiagnosticSeverity.Error,
                Outcome = GenerationDiagnosticOutcome.Unknown,
                Message = $"Granular fact '{fact.Id.Value}' describes artifact owner '{owner?.Value}' but asserts subject '{fact.Subject.Value}'; the fact was omitted",
                Facts = [fact.Id],
                Source = fact.Evidence.Source,
                Subject = fact.Subject
            });
        }

        return [.. valid];
    }

    static string? RealizationFileFor(GenerationFact fact) => fact switch
    {
        ArtifactFact artifact => artifact.Definition.File,
        ArtifactDeclarationFact declaration => declaration.Definition.File,
        _ => null
    };

    static ArtifactMemberKey? MemberFor(GenerationFact fact) => fact switch
    {
        ArtifactMemberDeclarationFact declaration => declaration.Definition.Member,
        ArtifactMemberTypeUseFact typeUse => typeUse.Definition.Member,
        TypeUseBindingFact binding => binding.Definition.Member,
        ArtifactMemberRoleFact role => role.Definition.Member,
        _ => null
    };

    static bool IsGranularArtifactFact(GenerationFact fact) => fact is
        ArtifactDeclarationFact or
        ArtifactMemberDeclarationFact or
        ArtifactMemberTypeUseFact or
        TypeUseBindingFact or
        ArtifactMemberRoleFact;

    static string DeclarationKey(ArtifactDeclarationDefinition definition) =>
        Structural.ArtifactDeclaration(definition);

    static string DeclarationKey(ArtifactDefinition definition) =>
        Structural.ArtifactDeclaration(new ArtifactDeclarationDefinition
        {
            Artifact = definition.Key,
            Name = definition.Name,
            Description = definition.Description,
            File = definition.File
        });

    static GenerationFact[] CanonicalFacts(IEnumerable<GenerationFact> facts) =>
    [
        .. facts
            .GroupBy(fact => fact.Id.Value, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => group.OrderBy(Structural.Fact, StringComparer.Ordinal).First())
    ];

    static void AddDiagnostic(
        List<GenerationDiagnostic> diagnostics,
        string code,
        GenerationDiagnosticOutcome outcome,
        SubjectId subject,
        IEnumerable<GenerationFact> facts,
        string message)
    {
        var inputs = CanonicalFacts(facts);
        diagnostics.Add(new GenerationDiagnostic
        {
            Code = code,
            Severity = GenerationDiagnosticSeverity.Error,
            Outcome = outcome,
            Message = $"{message}. Input facts: {string.Join(", ", inputs.Select(fact => $"'{fact.Id.Value}'"))}",
            Facts = [.. inputs.Select(fact => fact.Id)],
            Source = inputs.FirstOrDefault()?.Evidence.Source,
            Subject = subject
        });
    }

    static string Shape(TypeUseDefinition type) => type.Shape.Count == 0
        ? "<empty>"
        : string.Join(" -> ", type.Shape);

    sealed record OrderedProperty(int Order, PropertyDefinition Property);

    sealed record ArtifactVariant(ArtifactDefinition Definition, List<GenerationFact> Supports);

    sealed record MemberTypeUses(
        ArtifactMemberTypeUseFact[] Facts,
        IGrouping<string, ArtifactMemberTypeUseFact>? Variant,
        TypeReferenceDefinition? Type)
    {
        public ArtifactMemberTypeUseFact? Selected =>
            Variant?.OrderBy(fact => fact.Id.Value, StringComparer.Ordinal).First();
    }

    sealed record MemberContext(
        ArtifactVariant Variant,
        string Name,
        GenerationFact[] Facts,
        OrderedProperty? Existing,
        HashSet<ArtifactKey> Declared,
        List<GenerationDiagnostic> Diagnostics)
    {
        public bool Conflict(IEnumerable<GenerationFact> facts, string description) =>
            Fail(GenerationDiagnosticCodes.ConflictingArtifactMember, GenerationDiagnosticOutcome.Conflict, facts, description);

        public bool Incomplete(IEnumerable<GenerationFact> facts, string description) =>
            Fail(GenerationDiagnosticCodes.IncompleteArtifactMember, GenerationDiagnosticOutcome.Unknown, facts, description);

        public bool Fail(string code, GenerationDiagnosticOutcome outcome, IEnumerable<GenerationFact> facts, string description)
        {
            AddDiagnostic(Diagnostics, code, outcome, Variant.Definition.Key.Subject, facts, $"Artifact member '{Name}' {description}");
            return false;
        }
    }
}
