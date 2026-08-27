// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation;

static class AdapterContributionFreezer
{
    public static FrozenAdapterContributionInput Freeze(
        AdapterDescriptor? descriptor,
        AdapterContribution? contribution,
        AdapterContributionAdmissionContext context)
    {
        var frozenDescriptor = FreezeDescriptor(descriptor, context);
        if (contribution is null)
        {
            context.Add(
                AdapterContributionAdmissionDiagnosticCode.MissingRequiredValue,
                "Contribution",
                "The adapter contribution is required");
            return new(
                frozenDescriptor,
                new AdapterIdentity { Id = string.Empty, Version = string.Empty },
                [],
                []);
        }

        var adapter = FreezeIdentity(contribution.Adapter, "Contribution.Adapter", context);
        var facts = FreezeFacts(contribution.Facts, context);
        var diagnostics = FreezeDiagnostics(contribution.Diagnostics, context);
        return new(frozenDescriptor, adapter, facts, diagnostics);
    }

    internal static AdapterDescriptor FreezeDescriptor(
        AdapterDescriptor? descriptor,
        AdapterContributionAdmissionContext context)
    {
        if (descriptor is null)
        {
            context.Add(
                AdapterContributionAdmissionDiagnosticCode.MissingRequiredValue,
                "Descriptor",
                "The adapter descriptor is required");
            return new AdapterDescriptor
            {
                Identity = new AdapterIdentity { Id = string.Empty, Version = string.Empty },
                SourceLanguage = AdapterSourceLanguage.Unknown,
                Category = AdapterCategory.Unknown
            };
        }

        var range = descriptor.CompatibleGenerationVersions;
        if (range is null)
        {
            context.Add(
                AdapterContributionAdmissionDiagnosticCode.MissingRequiredValue,
                "Descriptor.CompatibleGenerationVersions",
                "The compatible Generation version range is required");
            range = GenerationVersionRange.Any;
        }

        return new AdapterDescriptor
        {
            Identity = FreezeIdentity(descriptor.Identity, "Descriptor.Identity", context),
            SourceLanguage = descriptor.SourceLanguage,
            Category = descriptor.Category,
            CompatibleGenerationVersions = new GenerationVersionRange
            {
                MinimumInclusive = range.MinimumInclusive,
                MaximumExclusive = range.MaximumExclusive
            },
            RequiredHostCapabilities = Canonical(descriptor.RequiredHostCapabilities),
            RequiredApiCapabilities = FreezeApiCapabilities(descriptor.RequiredApiCapabilities, context),
            EmittedFactCapabilities = Canonical(descriptor.EmittedFactCapabilities)
        };
    }

    static ImmutableArray<AdapterApiCapability> FreezeApiCapabilities(
        ImmutableArray<AdapterApiCapability> capabilities,
        AdapterContributionAdmissionContext context)
    {
        if (capabilities.IsDefault)
        {
            return [];
        }

        var frozen = ImmutableArray.CreateBuilder<AdapterApiCapability>();
        for (var index = 0; index < capabilities.Length; index++)
        {
            var capability = capabilities[index];
            if (capability is null)
            {
                context.Missing($"Descriptor.RequiredApiCapabilities[{index}]");
                continue;
            }

            frozen.Add(new AdapterApiCapability { Id = capability.Id ?? string.Empty });
        }

        return [.. frozen.OrderBy(capability => capability.Id, StringComparer.Ordinal)];
    }

    static ImmutableArray<T> Canonical<T>(ImmutableArray<T> values)
        where T : struct, Enum =>
        values.IsDefault
            ? []
            : [.. values.Distinct().OrderBy(value => Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture))];

    static AdapterIdentity FreezeIdentity(
        AdapterIdentity? identity,
        string path,
        AdapterContributionAdmissionContext context)
    {
        if (identity is null)
        {
            context.Add(
                AdapterContributionAdmissionDiagnosticCode.MissingRequiredValue,
                path,
                $"{path} is required");
            return new AdapterIdentity { Id = string.Empty, Version = string.Empty };
        }

        return new AdapterIdentity { Id = identity.Id ?? string.Empty, Version = identity.Version ?? string.Empty };
    }

    static ImmutableArray<GenerationFact> FreezeFacts(
        IReadOnlyList<GenerationFact>? facts,
        AdapterContributionAdmissionContext context)
    {
        if (facts is null)
        {
            context.Add(
                AdapterContributionAdmissionDiagnosticCode.NullRequiredCollection,
                "Contribution.Facts",
                "Contribution.Facts must not be null");
            return [];
        }

        var frozen = ImmutableArray.CreateBuilder<GenerationFact>();
        for (var index = 0; index < facts.Count; index++)
        {
            var fact = facts[index];
            var path = FactPath(fact);
            if (fact is null)
            {
                context.Add(
                    AdapterContributionAdmissionDiagnosticCode.MissingRequiredValue,
                    path,
                    $"{path} is required");
                continue;
            }

            var copy = FreezeFact(fact, path, context);
            if (copy is not null)
            {
                frozen.Add(copy);
            }
        }

        return
        [
            .. frozen
                .OrderBy(fact => fact.Id.Value, StringComparer.Ordinal)
                .ThenBy(fact => fact.Subject.Value, StringComparer.Ordinal)
                .ThenBy(FactFamily)
        ];
    }

    static GenerationFact? FreezeFact(
        GenerationFact fact,
        string path,
        AdapterContributionAdmissionContext context)
    {
        var id = fact.Id is null
            ? MissingFactId(path, context)
            : new FactId { Value = fact.Id.Value ?? string.Empty };
        var subject = FreezeSubject(fact.Subject, $"{path}.Subject", context);
        var evidence = FreezeEvidence(fact.Evidence, $"{path}.Evidence", context);

        return fact switch
        {
            ArtifactFact artifact => new ArtifactFact
            {
                Id = id,
                Subject = subject,
                Evidence = evidence,
                Definition = FreezeArtifactDefinition(artifact.Definition, $"{path}.Definition", context)
            },
            ArtifactPlacementFact placement => new ArtifactPlacementFact
            {
                Id = id,
                Subject = subject,
                Evidence = evidence,
                Artifact = FreezeArtifactKey(placement.Artifact, $"{path}.Artifact", context),
                Placement = FreezePlacement(placement.Placement, $"{path}.Placement", context)
            },
            ArtifactDeclarationFact declaration => GranularFactFreezer.Freeze(
                declaration,
                id,
                subject,
                evidence,
                path,
                context),
            ArtifactMemberDeclarationFact member => GranularFactFreezer.Freeze(
                member,
                id,
                subject,
                evidence,
                path,
                context),
            ArtifactMemberTypeUseFact typeUse => GranularFactFreezer.Freeze(
                typeUse,
                id,
                subject,
                evidence,
                path,
                context),
            TypeUseBindingFact binding => GranularFactFreezer.Freeze(
                binding,
                id,
                subject,
                evidence,
                path,
                context),
            ArtifactMemberRoleFact role => GranularFactFreezer.Freeze(
                role,
                id,
                subject,
                evidence,
                path,
                context),
            RelationshipFact relationship => new RelationshipFact
            {
                Id = id,
                Subject = subject,
                Evidence = evidence,
                Definition = FreezeRelationship(relationship.Definition, $"{path}.Definition", context)
            },
            ConceptRepresentationFact representation => new ConceptRepresentationFact
            {
                Id = id,
                Subject = subject,
                Evidence = evidence,
                Definition = FreezeConceptRepresentation(representation.Definition, $"{path}.Definition", context)
            },
            ConceptAttributeFact attribute => new ConceptAttributeFact
            {
                Id = id,
                Subject = subject,
                Evidence = evidence,
                Definition = FreezeConceptAttribute(attribute.Definition, $"{path}.Definition", context)
            },
            ConceptValidationRuleFact validation => new ConceptValidationRuleFact
            {
                Id = id,
                Subject = subject,
                Evidence = evidence,
                Definition = FreezeConceptValidation(validation.Definition, $"{path}.Definition", context)
            },
            SpecificationScenarioFact scenario => new SpecificationScenarioFact
            {
                Id = id,
                Subject = subject,
                Evidence = evidence,
                Definition = FreezeScenario(scenario.Definition, $"{path}.Definition", context)
            },
            SpecificationStepFact step => new SpecificationStepFact
            {
                Id = id,
                Subject = subject,
                Evidence = evidence,
                Definition = FreezeStep(step.Definition, $"{path}.Definition", context)
            },
            SpecificationValueFact value => new SpecificationValueFact
            {
                Id = id,
                Subject = subject,
                Evidence = evidence,
                Definition = FreezeValue(value.Definition, $"{path}.Definition", context)
            },
            _ => UnsupportedFact(fact, path, context)
        };
    }

    static FactId MissingFactId(string path, AdapterContributionAdmissionContext context)
    {
        context.Add(
            AdapterContributionAdmissionDiagnosticCode.MissingRequiredValue,
            $"{path}.Id",
            $"{path}.Id is required");
        return new FactId { Value = string.Empty };
    }

    static GenerationFact? UnsupportedFact(
        GenerationFact fact,
        string path,
        AdapterContributionAdmissionContext context)
    {
        context.Add(
            AdapterContributionAdmissionDiagnosticCode.UnsupportedFactType,
            path,
            $"Fact type '{fact.GetType().FullName}' is not a supported neutral fact family",
            fact.Id,
            fact.Subject);
        return null;
    }

    static ArtifactDefinition FreezeArtifactDefinition(
        ArtifactDefinition? definition,
        string path,
        AdapterContributionAdmissionContext context)
    {
        if (definition is null)
        {
            context.Missing(path);
            return new ArtifactDefinition
            {
                Key = FreezeArtifactKey(null, $"{path}.Key", context),
                Name = string.Empty
            };
        }

        return new ArtifactDefinition
        {
            Key = FreezeArtifactKey(definition.Key, $"{path}.Key", context),
            Name = definition.Name ?? string.Empty,
            Description = definition.Description,
            File = definition.File,
            Properties = FreezeProperties(definition.Properties, $"{path}.Properties", context)
        };
    }

    static ImmutableArray<PropertyDefinition> FreezeProperties(
        IReadOnlyList<PropertyDefinition>? properties,
        string path,
        AdapterContributionAdmissionContext context)
    {
        if (properties is null)
        {
            context.NullCollection(path);
            return [];
        }

        var frozen = ImmutableArray.CreateBuilder<PropertyDefinition>();
        for (var index = 0; index < properties.Count; index++)
        {
            var property = properties[index];
            var itemPath = $"{path}[{index}]";
            if (property is null)
            {
                context.Missing(itemPath);
                continue;
            }

            frozen.Add(new PropertyDefinition
            {
                Name = property.Name ?? string.Empty,
                Type = FreezeType(property.Type, $"{itemPath}.Type", context),
                IsIdentifier = property.IsIdentifier
            });
        }

        return frozen.ToImmutable();
    }

    static TypeReferenceDefinition FreezeType(
        TypeReferenceDefinition? type,
        string path,
        AdapterContributionAdmissionContext context)
    {
        if (type is null)
        {
            context.Missing(path);
            return new TypeReferenceDefinition { Name = string.Empty };
        }

        return new TypeReferenceDefinition
        {
            Name = type.Name ?? string.Empty,
            Subject = type.Subject is null ? null : FreezeSubject(type.Subject, $"{path}.Subject", context),
            IsCollection = type.IsCollection,
            IsOptional = type.IsOptional
        };
    }

    static ArtifactKey FreezeArtifactKey(
        ArtifactKey? key,
        string path,
        AdapterContributionAdmissionContext context)
    {
        if (key is null)
        {
            context.Missing(path);
            return new ArtifactKey
            {
                Subject = new SubjectId { Value = string.Empty },
                Kind = ArtifactKind.Unknown
            };
        }

        return new ArtifactKey
        {
            Subject = FreezeSubject(key.Subject, $"{path}.Subject", context),
            Kind = key.Kind
        };
    }

    static ArtifactPlacement FreezePlacement(
        ArtifactPlacement? placement,
        string path,
        AdapterContributionAdmissionContext context)
    {
        if (placement is null)
        {
            context.Missing(path);
            return new ArtifactPlacement
            {
                Module = string.Empty,
                Slice = string.Empty,
                SliceKind = GenerationSliceKind.Unknown
            };
        }

        return new ArtifactPlacement
        {
            Module = placement.Module ?? string.Empty,
            Features = FreezeStrings(placement.Features, $"{path}.Features", context),
            Slice = placement.Slice ?? string.Empty,
            SliceKind = placement.SliceKind
        };
    }

    static RelationshipDefinition FreezeRelationship(
        RelationshipDefinition? definition,
        string path,
        AdapterContributionAdmissionContext context)
    {
        if (definition is null)
        {
            context.Missing(path);
            return new RelationshipDefinition
            {
                Key = new RelationshipKey
                {
                    Kind = RelationshipKind.Unknown,
                    Source = new SubjectId { Value = string.Empty },
                    Target = new SubjectId { Value = string.Empty }
                }
            };
        }

        var key = definition.Key;
        if (key is null)
        {
            context.Missing($"{path}.Key");
            key = new RelationshipKey
            {
                Kind = RelationshipKind.Unknown,
                Source = new SubjectId { Value = string.Empty },
                Target = new SubjectId { Value = string.Empty }
            };
        }

        return new RelationshipDefinition
        {
            Key = new RelationshipKey
            {
                Kind = key.Kind,
                Source = FreezeSubject(key.Source, $"{path}.Key.Source", context),
                Target = FreezeSubject(key.Target, $"{path}.Key.Target", context),
                Discriminator = key.Discriminator
            },
            SourceMember = definition.SourceMember,
            TargetMember = definition.TargetMember,
            IsCollection = definition.IsCollection,
            IsOptional = definition.IsOptional
        };
    }

    static ConceptRepresentationDefinition FreezeConceptRepresentation(
        ConceptRepresentationDefinition? definition,
        string path,
        AdapterContributionAdmissionContext context)
    {
        if (definition is null)
        {
            context.Missing(path);
            return new ConceptRepresentationDefinition
            {
                Concept = new SubjectId { Value = string.Empty },
                Kind = ConceptRepresentationKind.Unknown
            };
        }

        return new ConceptRepresentationDefinition
        {
            Concept = FreezeSubject(definition.Concept, $"{path}.Concept", context),
            Kind = definition.Kind,
            Primitive = definition.Primitive,
            EnumerationValues = FreezeStrings(definition.EnumerationValues, $"{path}.EnumerationValues", context)
        };
    }

    static ConceptAttributeDefinition FreezeConceptAttribute(
        ConceptAttributeDefinition? definition,
        string path,
        AdapterContributionAdmissionContext context)
    {
        if (definition is null)
        {
            context.Missing(path);
            return new ConceptAttributeDefinition
            {
                Concept = new SubjectId { Value = string.Empty },
                Kind = ConceptAttributeKind.Unknown,
                Name = string.Empty
            };
        }

        return new ConceptAttributeDefinition
        {
            Concept = FreezeSubject(definition.Concept, $"{path}.Concept", context),
            Kind = definition.Kind,
            Name = definition.Name ?? string.Empty,
            Reason = definition.Reason
        };
    }

    static ConceptValidationRuleDefinition FreezeConceptValidation(
        ConceptValidationRuleDefinition? definition,
        string path,
        AdapterContributionAdmissionContext context)
    {
        if (definition is null)
        {
            context.Missing(path);
            return new ConceptValidationRuleDefinition
            {
                Concept = new SubjectId { Value = string.Empty },
                RuleIdentity = string.Empty,
                Kind = ConceptValidationRuleKind.Unknown
            };
        }

        return new ConceptValidationRuleDefinition
        {
            Concept = FreezeSubject(definition.Concept, $"{path}.Concept", context),
            RuleIdentity = definition.RuleIdentity ?? string.Empty,
            Kind = definition.Kind,
            Predicate = definition.Predicate,
            Message = definition.Message,
            ImplementationFile = definition.ImplementationFile
        };
    }

    static SpecificationScenarioDefinition FreezeScenario(
        SpecificationScenarioDefinition? definition,
        string path,
        AdapterContributionAdmissionContext context)
    {
        if (definition is null)
        {
            context.Missing(path);
            return new SpecificationScenarioDefinition
            {
                Key = FreezeScenarioKey(null, $"{path}.Key", context),
                Name = string.Empty,
                TargetArtifact = FreezeArtifactKey(null, $"{path}.TargetArtifact", context)
            };
        }

        return new SpecificationScenarioDefinition
        {
            Key = FreezeScenarioKey(definition.Key, $"{path}.Key", context),
            Name = definition.Name ?? string.Empty,
            TargetArtifact = FreezeArtifactKey(definition.TargetArtifact, $"{path}.TargetArtifact", context),
            Steps = FreezeStepKeys(definition.Steps, $"{path}.Steps", context)
        };
    }

    static SpecificationStepDefinition FreezeStep(
        SpecificationStepDefinition? definition,
        string path,
        AdapterContributionAdmissionContext context)
    {
        if (definition is null)
        {
            context.Missing(path);
            return new SpecificationStepDefinition
            {
                Key = FreezeStepKey(null, $"{path}.Key", context),
                Phase = SpecificationStepPhase.Unknown,
                Kind = SpecificationStepKind.Unknown
            };
        }

        return new SpecificationStepDefinition
        {
            Key = FreezeStepKey(definition.Key, $"{path}.Key", context),
            Phase = definition.Phase,
            Kind = definition.Kind,
            Artifact = definition.Artifact is null
                ? null
                : FreezeArtifactKey(definition.Artifact, $"{path}.Artifact", context),
            ErrorCode = definition.ErrorCode,
            ErrorMessage = definition.ErrorMessage,
            Values = FreezeValueKeys(definition.Values, $"{path}.Values", context)
        };
    }

    static SpecificationValueDefinition FreezeValue(
        SpecificationValueDefinition? definition,
        string path,
        AdapterContributionAdmissionContext context)
    {
        if (definition is null)
        {
            context.Missing(path);
            return new SpecificationValueDefinition
            {
                Key = FreezeValueKey(null, $"{path}.Key", context),
                Kind = SpecificationValueKind.Unknown
            };
        }

        return new SpecificationValueDefinition
        {
            Key = FreezeValueKey(definition.Key, $"{path}.Key", context),
            Kind = definition.Kind,
            Type = definition.Type is null ? null : FreezeType(definition.Type, $"{path}.Type", context),
            Scalar = definition.Scalar,
            Children = FreezeValueKeys(definition.Children, $"{path}.Children", context)
        };
    }

    static SpecificationScenarioKey FreezeScenarioKey(
        SpecificationScenarioKey? key,
        string path,
        AdapterContributionAdmissionContext context)
    {
        if (key is null)
        {
            context.Missing(path);
            return new SpecificationScenarioKey { Scenario = new SubjectId { Value = string.Empty } };
        }

        return new SpecificationScenarioKey
        {
            Scenario = FreezeSubject(key.Scenario, $"{path}.Scenario", context)
        };
    }

    static SpecificationStepKey FreezeStepKey(
        SpecificationStepKey? key,
        string path,
        AdapterContributionAdmissionContext context)
    {
        if (key is null)
        {
            context.Missing(path);
            return new SpecificationStepKey
            {
                Scenario = FreezeScenarioKey(null, $"{path}.Scenario", context),
                Index = -1
            };
        }

        return new SpecificationStepKey
        {
            Scenario = FreezeScenarioKey(key.Scenario, $"{path}.Scenario", context),
            Index = key.Index
        };
    }

    static SpecificationValueKey FreezeValueKey(
        SpecificationValueKey? key,
        string path,
        AdapterContributionAdmissionContext context)
    {
        if (key is null)
        {
            context.Missing(path);
            return new SpecificationValueKey
            {
                Step = FreezeStepKey(null, $"{path}.Step", context)
            };
        }

        return new SpecificationValueKey
        {
            Step = FreezeStepKey(key.Step, $"{path}.Step", context),
            Path = FreezeStrings(key.Path, $"{path}.Path", context)
        };
    }

    static ImmutableArray<SpecificationStepKey> FreezeStepKeys(
        IReadOnlyList<SpecificationStepKey>? keys,
        string path,
        AdapterContributionAdmissionContext context)
    {
        if (keys is null)
        {
            context.NullCollection(path);
            return [];
        }

        var frozen = ImmutableArray.CreateBuilder<SpecificationStepKey>();
        for (var index = 0; index < keys.Count; index++)
        {
            frozen.Add(FreezeStepKey(keys[index], $"{path}[{index}]", context));
        }

        return frozen.ToImmutable();
    }

    static ImmutableArray<SpecificationValueKey> FreezeValueKeys(
        IReadOnlyList<SpecificationValueKey>? keys,
        string path,
        AdapterContributionAdmissionContext context)
    {
        if (keys is null)
        {
            context.NullCollection(path);
            return [];
        }

        var frozen = ImmutableArray.CreateBuilder<SpecificationValueKey>();
        for (var index = 0; index < keys.Count; index++)
        {
            frozen.Add(FreezeValueKey(keys[index], $"{path}[{index}]", context));
        }

        return frozen.ToImmutable();
    }

    static ImmutableArray<string> FreezeStrings(
        IReadOnlyList<string>? values,
        string path,
        AdapterContributionAdmissionContext context)
    {
        if (values is null)
        {
            context.NullCollection(path);
            return [];
        }

        return [.. values.Select(value => value ?? string.Empty)];
    }

    static SubjectId FreezeSubject(
        SubjectId? subject,
        string path,
        AdapterContributionAdmissionContext context)
    {
        if (subject is null)
        {
            context.Missing(path);
            return new SubjectId { Value = string.Empty };
        }

        return new SubjectId { Value = subject.Value ?? string.Empty };
    }

    static Evidence FreezeEvidence(
        Evidence? evidence,
        string path,
        AdapterContributionAdmissionContext context)
    {
        if (evidence is null)
        {
            context.Missing(path);
            return new Evidence
            {
                Adapter = new AdapterIdentity { Id = string.Empty, Version = string.Empty },
                Strength = EvidenceStrength.Unknown
            };
        }

        return new Evidence
        {
            Adapter = FreezeIdentity(evidence.Adapter, $"{path}.Adapter", context),
            Strength = evidence.Strength,
            Source = evidence.Source is null ? null : FreezeSource(evidence.Source),
            Explanation = evidence.Explanation
        };
    }

    static ImmutableArray<GenerationDiagnostic> FreezeDiagnostics(
        IReadOnlyList<GenerationDiagnostic>? diagnostics,
        AdapterContributionAdmissionContext context)
    {
        if (diagnostics is null)
        {
            context.NullCollection("Contribution.Diagnostics");
            return [];
        }

        var frozen = ImmutableArray.CreateBuilder<GenerationDiagnostic>();
        for (var index = 0; index < diagnostics.Count; index++)
        {
            var diagnostic = diagnostics[index];
            var path = DiagnosticPath(diagnostic);
            if (diagnostic is null)
            {
                context.Missing(path);
                continue;
            }

            frozen.Add(new GenerationDiagnostic
            {
                Code = diagnostic.Code ?? string.Empty,
                Severity = diagnostic.Severity,
                Message = diagnostic.Message ?? string.Empty,
                Outcome = diagnostic.Outcome,
                Source = diagnostic.Source is null ? null : FreezeSource(diagnostic.Source),
                Subject = diagnostic.Subject is null ? null : FreezeSubject(diagnostic.Subject, $"{path}.Subject", context)
            });
        }

        return
        [
            .. frozen
                .OrderBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
                .ThenBy(diagnostic => (int)diagnostic.Severity)
                .ThenBy(diagnostic => diagnostic.Outcome is null ? int.MinValue : (int)diagnostic.Outcome.Value)
                .ThenBy(diagnostic => diagnostic.Source?.FileIdentity?.Project, StringComparer.Ordinal)
                .ThenBy(diagnostic => diagnostic.Source?.FileIdentity?.Path, StringComparer.Ordinal)
                .ThenBy(diagnostic => diagnostic.Source?.Path, StringComparer.Ordinal)
                .ThenBy(diagnostic => diagnostic.Source?.StartLine)
                .ThenBy(diagnostic => diagnostic.Source?.StartColumn)
                .ThenBy(diagnostic => diagnostic.Source?.EndLine)
                .ThenBy(diagnostic => diagnostic.Source?.EndColumn)
                .ThenBy(diagnostic => diagnostic.Subject?.Value, StringComparer.Ordinal)
                .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
        ];
    }

    static SourceRange FreezeSource(SourceRange source) => new()
    {
        Path = source.Path ?? string.Empty,
        FileIdentity = source.FileIdentity is null
            ? null
            : new SourceFileIdentity
            {
                Project = source.FileIdentity.Project ?? string.Empty,
                Path = source.FileIdentity.Path ?? string.Empty
            },
        StartLine = source.StartLine,
        StartColumn = source.StartColumn,
        EndLine = source.EndLine,
        EndColumn = source.EndColumn
    };

    static string FactPath(GenerationFact? fact)
    {
        var identity = StablePathComponent(fact?.Id?.Value, "invalid-id");
        if (string.IsNullOrEmpty(fact?.Id?.Value))
        {
            var family = fact?.GetType().Name ?? "null";
            var subject = StablePathComponent(fact?.Subject?.Value, "unidentified-subject");
            identity = $"{family}:{subject}";
        }

        return $"Contribution.Facts[{identity}]";
    }

    static string DiagnosticPath(GenerationDiagnostic? diagnostic)
    {
        var code = StablePathComponent(diagnostic?.Code, "unidentified-code");
        return $"Contribution.Diagnostics[{code}]";
    }

    static string StablePathComponent(string? value, string fallback) =>
        AdapterContributionText.IsNormalized(value, false)
            ? value!
            : $"{fallback}:{Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(value ?? string.Empty))}";

    static int FactFamily(GenerationFact fact) => fact switch
    {
        ArtifactFact => (int)GenerationFactCapability.Artifact,
        ArtifactPlacementFact => (int)GenerationFactCapability.ArtifactPlacement,
        RelationshipFact => (int)GenerationFactCapability.Relationship,
        ConceptRepresentationFact => (int)GenerationFactCapability.ConceptRepresentation,
        ConceptAttributeFact => (int)GenerationFactCapability.ConceptAttribute,
        ConceptValidationRuleFact => (int)GenerationFactCapability.ConceptValidationRule,
        SpecificationScenarioFact => (int)GenerationFactCapability.SpecificationScenario,
        SpecificationStepFact => (int)GenerationFactCapability.SpecificationStep,
        SpecificationValueFact => (int)GenerationFactCapability.SpecificationValue,
        ArtifactDeclarationFact => (int)GenerationFactCapability.ArtifactDeclaration,
        ArtifactMemberDeclarationFact => (int)GenerationFactCapability.ArtifactMemberDeclaration,
        ArtifactMemberTypeUseFact => (int)GenerationFactCapability.ArtifactMemberTypeUse,
        TypeUseBindingFact => (int)GenerationFactCapability.TypeUseBinding,
        ArtifactMemberRoleFact => (int)GenerationFactCapability.ArtifactMemberRole,
        _ => int.MaxValue
    };
}

sealed record FrozenAdapterContributionInput(
    AdapterDescriptor Descriptor,
    AdapterIdentity ContributionAdapter,
    ImmutableArray<GenerationFact> Facts,
    ImmutableArray<GenerationDiagnostic> Diagnostics);
