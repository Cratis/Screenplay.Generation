// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class GranularArtifactResolver
{
    public static ArtifactFact[] Resolve(
        IReadOnlyList<GenerationFact> facts,
        List<GenerationDiagnostic> diagnostics)
    {
        var legacy = facts.OfType<ArtifactFact>().ToArray();
        var granular = ValidGranularFacts(facts.Where(IsGranularArtifactFact), diagnostics);
        if (granular.Length == 0)
        {
            return legacy;
        }

        var variants = DeclarationVariants(legacy, granular.OfType<ArtifactDeclarationFact>());
        var effective = new List<ArtifactFact>();
        foreach (var variant in variants
                     .OrderBy(candidate => Structural.ArtifactKey(candidate.Definition.Key), StringComparer.Ordinal)
                     .ThenBy(candidate => Structural.Artifact(candidate.Definition), StringComparer.Ordinal))
        {
            var overlaid = ApplyMembers(variant, granular, diagnostics);
            if (overlaid is not null)
            {
                foreach (var support in CanonicalFacts(overlaid.Supports))
                {
                    effective.Add(new ArtifactFact
                    {
                        Id = support.Id,
                        Subject = overlaid.Definition.Key.Subject,
                        Evidence = support.Evidence,
                        Definition = overlaid.Definition
                    });
                }
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

    static List<ArtifactVariant> DeclarationVariants(
        IEnumerable<ArtifactFact> legacy,
        IEnumerable<ArtifactDeclarationFact> granular)
    {
        var variants = legacy
            .GroupBy(fact => Structural.Artifact(fact.Definition), StringComparer.Ordinal)
            .Select(group => new ArtifactVariant(group.First().Definition, [.. group]))
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
            var currentFacts = memberFacts
                .Where(fact => MemberFor(fact)!.Name == memberName)
                .ToArray();
            properties.TryGetValue(memberName, out var existing);
            var declarationFacts = currentFacts.OfType<ArtifactMemberDeclarationFact>().ToArray();
            var orders = declarationFacts.Select(fact => fact.Definition.DeclarationOrder)
                .Concat(existing is null ? [] : [existing.Order])
                .Distinct()
                .ToArray();
            if (orders.Length == 0)
            {
                AddDiagnostic(
                    diagnostics,
                    GenerationDiagnosticCodes.IncompleteArtifactMember,
                    GenerationDiagnosticOutcome.Unknown,
                    key.Subject,
                    currentFacts,
                    $"Artifact member '{memberName}' has no declaration order");
                failed = true;
                continue;
            }

            if (orders.Length > 1)
            {
                AddDiagnostic(
                    diagnostics,
                    GenerationDiagnosticCodes.ConflictingArtifactMember,
                    GenerationDiagnosticOutcome.Conflict,
                    key.Subject,
                    currentFacts.Concat(variant.Supports),
                    $"Artifact member '{memberName}' has incompatible declaration orders");
                failed = true;
                continue;
            }

            var typeUseFacts = currentFacts.OfType<ArtifactMemberTypeUseFact>().ToArray();
            var typeUseVariants = typeUseFacts
                .GroupBy(fact => Structural.TypeUse(fact.Definition.Type), StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .ToArray();
            if (typeUseVariants.Length > 1)
            {
                AddDiagnostic(
                    diagnostics,
                    GenerationDiagnosticCodes.ConflictingArtifactMember,
                    GenerationDiagnosticOutcome.Conflict,
                    key.Subject,
                    typeUseFacts,
                    $"Artifact member '{memberName}' has incompatible exact type uses");
                failed = true;
                continue;
            }

            var type = existing?.Property.Type;
            ArtifactMemberTypeUseFact? typeUse = null;
            if (typeUseVariants.Length == 1)
            {
                typeUse = typeUseVariants[0].OrderBy(fact => fact.Id.Value, StringComparer.Ordinal).First();
                if (!TryTypeReference(typeUse.Definition.Type, out var granularType))
                {
                    AddDiagnostic(
                        diagnostics,
                        GenerationDiagnosticCodes.UnsupportedTypeUseShape,
                        GenerationDiagnosticOutcome.Unsupported,
                        key.Subject,
                        typeUseVariants[0],
                        $"Artifact member '{memberName}' uses exact shape '{Shape(typeUse.Definition.Type)}' that Screenplay cannot represent");
                    failed = true;
                    continue;
                }

                if (type is not null && !SameUnboundType(type, granularType))
                {
                    AddDiagnostic(
                        diagnostics,
                        GenerationDiagnosticCodes.ConflictingArtifactMember,
                        GenerationDiagnosticOutcome.Conflict,
                        key.Subject,
                        typeUseVariants[0].Cast<GenerationFact>().Concat(variant.Supports),
                        $"Artifact member '{memberName}' has incompatible legacy and granular type uses");
                    failed = true;
                    continue;
                }

                type = granularType with { Subject = type?.Subject };
            }

            if (type is null)
            {
                AddDiagnostic(
                    diagnostics,
                    GenerationDiagnosticCodes.IncompleteArtifactMember,
                    GenerationDiagnosticOutcome.Unknown,
                    key.Subject,
                    currentFacts,
                    $"Artifact member '{memberName}' has no exact type use");
                failed = true;
                continue;
            }

            var bindings = currentFacts.OfType<TypeUseBindingFact>().ToArray();
            var bindingVariants = bindings
                .GroupBy(fact => Structural.ArtifactKey(fact.Definition.Target), StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .ToArray();
            if (bindingVariants.Length > 1)
            {
                AddDiagnostic(
                    diagnostics,
                    GenerationDiagnosticCodes.ConflictingArtifactMember,
                    GenerationDiagnosticOutcome.Conflict,
                    key.Subject,
                    bindings,
                    $"Artifact member '{memberName}' has incompatible exact type bindings");
                failed = true;
                continue;
            }

            if (bindingVariants.Length == 0 &&
                typeUse?.Definition.Type.ObservedTypeSubject is not null &&
                type.Subject is null)
            {
                AddDiagnostic(
                    diagnostics,
                    GenerationDiagnosticCodes.IncompleteArtifactMember,
                    GenerationDiagnosticOutcome.Unknown,
                    key.Subject,
                    typeUseFacts.Concat(variant.Supports),
                    $"Artifact member '{memberName}' has an observed exact type subject but no admitted binding");
                failed = true;
                continue;
            }

            if (bindingVariants.Length == 1)
            {
                var binding = bindingVariants[0].OrderBy(fact => fact.Id.Value, StringComparer.Ordinal).First();
                var target = binding.Definition.Target.Subject;
                if ((type.Subject is not null && type.Subject != target) ||
                    (typeUse?.Definition.Type.ObservedTypeSubject is not null && typeUse.Definition.Type.ObservedTypeSubject != target))
                {
                    AddDiagnostic(
                        diagnostics,
                        GenerationDiagnosticCodes.ConflictingArtifactMember,
                        GenerationDiagnosticOutcome.Conflict,
                        key.Subject,
                        bindingVariants[0].Cast<GenerationFact>().Concat(typeUseFacts).Concat(variant.Supports),
                        $"Artifact member '{memberName}' binds incompatible exact type subjects");
                    failed = true;
                    continue;
                }

                type = type with { Subject = target };
            }

            var roleFacts = currentFacts.OfType<ArtifactMemberRoleFact>().ToArray();
            var roleVariants = roleFacts.Select(fact => fact.Definition.Role).Distinct().ToArray();
            if (roleVariants.Length > 1)
            {
                AddDiagnostic(
                    diagnostics,
                    GenerationDiagnosticCodes.ConflictingArtifactMember,
                    GenerationDiagnosticOutcome.Conflict,
                    key.Subject,
                    roleFacts,
                    $"Artifact member '{memberName}' has incompatible identifier roles");
                failed = true;
                continue;
            }

            var isIdentifier = existing?.Property.IsIdentifier == true || roleVariants.Length == 1;
            properties[memberName] = new OrderedProperty(
                orders[0],
                new PropertyDefinition
                {
                    Name = memberName,
                    Type = type,
                    IsIdentifier = isIdentifier
                });
            supports.AddRange(currentFacts);
        }

        var duplicateOrders = properties.Values
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
            failed = true;
        }

        if (failed)
        {
            return variant.Supports.Exists(fact => fact is ArtifactFact)
                ? variant
                : null;
        }

        return new ArtifactVariant(
            variant.Definition with
            {
                Properties =
                [
                    .. properties.Values
                        .OrderBy(property => property.Order)
                        .ThenBy(property => property.Property.Name, StringComparer.Ordinal)
                        .Select(property => property.Property)
                ]
            },
            [.. CanonicalFacts(supports)]);
    }

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
                Source = fact.Evidence.Source,
                Subject = fact.Subject
            });
        }

        return [.. valid];
    }

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
            Source = inputs.FirstOrDefault()?.Evidence.Source,
            Subject = subject
        });
    }

    static string Shape(TypeUseDefinition type) => string.Join('(', type.Shape) + new string(')', type.Shape.Count - 1);

    sealed record OrderedProperty(int Order, PropertyDefinition Property);

    sealed record ArtifactVariant(ArtifactDefinition Definition, List<GenerationFact> Supports);
}
