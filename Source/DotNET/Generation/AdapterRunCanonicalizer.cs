// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation;

static class AdapterRunCanonicalizer
{
    public static ImmutableArray<GenerationFactRecord> FactRecords(IEnumerable<GenerationFactRecord> records) =>
    [
        .. records.Select(record => new GenerationFactRecord
        {
            Fact = Fact(record.Fact),
            Lineage = record.Lineage is null ? null : Lineage(record.Lineage),
            Disposition = record.Disposition,
            Diagnostics = Diagnostics(record.Diagnostics)
        })
    ];

    public static GenerationDerivationSnapshot Derivation(GenerationDerivationSnapshot snapshot) => new()
    {
        Rules =
        [
            .. snapshot.Rules
                .Select(rule => new GenerationDerivationRuleRecord
                {
                    Rule = Rule(rule.Rule),
                    Inputs = [.. rule.Inputs.Select(id => new FactId { Value = id.Value }).Distinct().OrderBy(id => id.Value, StringComparer.Ordinal)],
                    Outputs = [.. rule.Outputs.Select(id => new FactId { Value = id.Value }).Distinct().OrderBy(id => id.Value, StringComparer.Ordinal)],
                    Diagnostics = Diagnostics(rule.Diagnostics)
                })
                .OrderBy(rule => rule.Rule.Id, StringComparer.Ordinal)
                .ThenBy(rule => rule.Rule.Version, StringComparer.Ordinal)
        ],
        Facts =
        [
            .. FactRecords(snapshot.Facts)
                .OrderBy(record => record.Fact.Id.Value, StringComparer.Ordinal)
                .ThenBy(record => record.Fact.Subject.Value, StringComparer.Ordinal)
                .ThenBy(record => Structural.FactFamily(record.Fact))
                .ThenBy(record => Structural.FactDefinition(record.Fact), StringComparer.Ordinal)
        ],
        Diagnostics = Diagnostics(snapshot.Diagnostics)
    };

    public static ImmutableArray<GenerationDiagnostic> Diagnostics(IEnumerable<GenerationDiagnostic> diagnostics) =>
    [
        .. diagnostics
            .Select(Diagnostic)
            .GroupBy(Structural.Diagnostic, StringComparer.Ordinal)
            .OrderBy(group => Canonical.Diagnostic(group.First()), StringComparer.Ordinal)
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => group.First())
    ];

    public static GenerationFact Fact(GenerationFact fact) => ArtifactFactDefinition(fact) with
    {
        Id = new FactId { Value = fact.Id.Value },
        Subject = Subject(fact.Subject),
        Evidence = Evidence(fact.Evidence)
    };

    public static AdapterRunRecord Adapter(AdapterRunRecord record)
    {
        var descriptor = Descriptor(record.Descriptor);
        return new AdapterRunRecord
        {
            Considered = record.Considered,
            Probed = record.Probed,
            Executed = record.Executed,
            Descriptor = descriptor,
            Probe = Probe(record.Probe),
            Execution = Execution(record.Execution),
            Disposition = record.Disposition
        };
    }

    static GenerationFact ArtifactFactDefinition(GenerationFact fact) => fact switch
    {
        ArtifactFact artifact => artifact with { Definition = Artifact(artifact.Definition) },
        ArtifactPlacementFact placement => placement with { Artifact = ArtifactKey(placement.Artifact), Placement = Placement(placement.Placement), MixedSliceFallback = MixedSliceFallbackStructure.Copy(placement.MixedSliceFallback) },
        _ => MemberFactDefinition(fact)
    };

    static GenerationFact MemberFactDefinition(GenerationFact fact) => fact switch
    {
        ArtifactDeclarationFact declaration => declaration with { Definition = ArtifactDeclaration(declaration.Definition) },
        ArtifactMemberDeclarationFact member => member with { Definition = ArtifactMemberDeclaration(member.Definition) },
        ArtifactMemberTypeUseFact typeUse => typeUse with { Definition = ArtifactMemberTypeUse(typeUse.Definition) },
        TypeUseBindingFact binding => binding with { Definition = TypeUseBinding(binding.Definition) },
        _ => TypeUseOmissionDefinition(fact)
    };

    static GenerationFact TypeUseOmissionDefinition(GenerationFact fact) => fact switch
    {
        TypeUseCompatibilityProjectionFact projection => Projection(projection),
        TypeUseBindingPolicyFact policy => policy with
        {
            Definition = policy.Definition with { TypeUse = new() { Value = policy.Definition.TypeUse.Value } }
        },
        _ => PlacementOmissionDefinition(fact)
    };

    static GenerationFact PlacementOmissionDefinition(GenerationFact fact) => fact switch
    {
        ArtifactSourcePlacementCandidateFact candidate => candidate with { Definition = SourcePlacementCandidateStructure.Copy(candidate.Definition, ArtifactKey) },
        MethodDiagnosticProjectionFact projection => Projection(projection),
        OccurrenceOmissionDiagnosticFact candidate => Omission(candidate),
        TypeUseTargetOmittedFact omission => omission with
        {
            Definition = omission.Definition with { Diagnostic = Diagnostic(omission.Definition.Diagnostic) }
        },
        _ => UsageFactDefinition(fact)
    };

    static GenerationFact UsageFactDefinition(GenerationFact fact) => fact switch
    {
        ArtifactMemberRoleFact role => role with { Definition = ArtifactMemberRole(role.Definition) },
        BackendScopeFact scope => scope with { Definition = scope.Definition with { Key = Scope(scope.Definition.Key)! } },
        ArtifactUsageDeclarationFact declaration => declaration with
        {
            Definition = declaration.Definition with { Declaration = Subject(declaration.Definition.Declaration), Artifact = ArtifactKey(declaration.Definition.Artifact) }
        },
        EntryPointFact entry => entry with
        {
            Definition = entry.Definition with { Method = Subject(entry.Definition.Method), Artifact = ArtifactKey(entry.Definition.Artifact) }
        },
        MethodArtifactUsageFact usage => usage with
        {
            Definition = usage.Definition with { Method = Subject(usage.Definition.Method), Declaration = Subject(usage.Definition.Declaration), SourceScope = Scope(usage.Definition.SourceScope), TargetScope = Scope(usage.Definition.TargetScope) }
        },
        _ => ObservationFactDefinition(fact)
    };

    static GenerationFact ObservationFactDefinition(GenerationFact fact) => fact switch
    {
        MethodExecutionClassificationFact classification => Classification(classification),
        MethodFactSupportFact support => Support(support),
        EntryPointInputTransferFact transfer => transfer with { Definition = transfer.Definition with { Method = Subject(transfer.Definition.Method), Artifact = ArtifactKey(transfer.Definition.Artifact) } },
        RelationshipFact relationship => relationship with { Definition = Relationship(relationship.Definition) },
        _ => DeclarationInputFactDefinition(fact)
    };

    static GenerationFact DeclarationInputFactDefinition(GenerationFact fact) => fact switch
    {
        DeclarationInputMemberTargetOmittedFact omission => omission with
        {
            Definition = omission.Definition with { Member = DeclarationMember(omission.Definition.Member), Diagnostic = Diagnostic(omission.Definition.Diagnostic) }
        },
        DeclarationInputMemberDeclarationFact declaration => declaration with { Definition = declaration.Definition with { Member = DeclarationMember(declaration.Definition.Member) } },
        DeclarationInputMemberTypeUseFact typeUse => typeUse with
        {
            Definition = typeUse.Definition with
            {
                Member = DeclarationMember(typeUse.Definition.Member),
                Type = TypeUse(typeUse.Definition.Type)
            }
        },
        _ => DomainFactDefinition(fact)
    };

    static GenerationFact DomainFactDefinition(GenerationFact fact) => fact switch
    {
        ConceptRepresentationFact representation => representation with { Definition = ConceptRepresentation(representation.Definition) },
        ConceptAttributeFact attribute => attribute with { Definition = ConceptAttribute(attribute.Definition) },
        ConceptValidationRuleFact validation => validation with { Definition = ConceptValidationRule(validation.Definition) },
        _ => SpecificationFactDefinition(fact)
    };

    static GenerationFact SpecificationFactDefinition(GenerationFact fact) => fact switch
    {
        SpecificationScenarioFact scenario => scenario with { Definition = SpecificationScenario(scenario.Definition) },
        SpecificationStepFact step => step with { Definition = SpecificationStep(step.Definition) },
        SpecificationValueFact value => value with { Definition = SpecificationValue(value.Definition) },
        _ => fact
    };

    static MethodExecutionClassificationFact Classification(MethodExecutionClassificationFact fact) => fact.Definition is null ? fact : fact with
    {
        Definition = fact.Definition with
        {
            Method = Subject(fact.Definition.Method),
            Scope = Scope(fact.Definition.Scope),
            OmissionReason = fact.Definition.OmissionReason is { } reason ? Diagnostic(reason) : null
        }
    };

    static DeclarationMemberKey DeclarationMember(DeclarationMemberKey member) => member with { Declaration = Subject(member.Declaration), Scope = Scope(member.Scope) };

    static MethodFactSupportFact Support(MethodFactSupportFact fact) => fact.Definition is not { Candidate: not null, Alternatives: not null } || fact.Definition.Alternatives.Any(alternative => alternative is null) ? fact : fact with
    {
        Definition = fact.Definition with
        {
            Candidate = new FactId { Value = fact.Definition.Candidate.Value },
            Alternatives = [.. fact.Definition.Alternatives.Select(alternative => alternative with
            {
                Method = Subject(alternative.Method),
                Scope = Scope(alternative.Scope),
                EnclosingExecutables = [.. alternative.EnclosingExecutables.Select(Subject).Distinct().OrderBy(subject => subject.Value, StringComparer.Ordinal)],
                Diagnostics = Diagnostics(alternative.Diagnostics)
            }).GroupBy(Structural.MethodSupport, StringComparer.Ordinal).OrderBy(group => group.Key, StringComparer.Ordinal).Select(group => group.First())]
        }
    };

    static MethodDiagnosticProjectionFact Projection(MethodDiagnosticProjectionFact fact) => fact.Definition is not { Method: not null, Diagnostic: not null } ? fact : fact with
    {
        Definition = fact.Definition with
        {
            Method = Subject(fact.Definition.Method), Scope = Scope(fact.Definition.Scope), Diagnostic = Diagnostic(fact.Definition.Diagnostic)
        }
    };

    static OccurrenceOmissionDiagnosticFact Omission(OccurrenceOmissionDiagnosticFact fact) => !OccurrenceOmissionDiagnosticValidation.Valid(fact) ? fact : fact with
    {
        Definition = fact.Definition with
        {
            Family = new() { Value = fact.Definition.Family.Value }, Occurrence = ArtifactKey(fact.Definition.Occurrence), Diagnostic = Diagnostic(fact.Definition.Diagnostic)
        }
    };

    static GenerationFactLineage Lineage(GenerationFactLineage lineage) => new()
    {
        Producer = Rule(lineage.Producer),
        Inputs = [.. lineage.Inputs.Select(id => new FactId { Value = id.Value }).Distinct().OrderBy(id => id.Value, StringComparer.Ordinal)],
        Evidence = [.. lineage.Evidence.Select(Evidence)]
    };

    static GenerationDerivationRuleIdentity Rule(GenerationDerivationRuleIdentity rule) => new()
    {
        Id = rule.Id,
        Version = rule.Version
    };

    static AdapterDescriptor Descriptor(AdapterDescriptor descriptor) =>
        AdapterDescriptorAdmission.Admit(descriptor).Descriptor;

    static AdapterProbeResult Probe(AdapterProbeResult probe)
    {
        var evidence = ProbeEvidence(probe.Evidence);
        return probe switch
        {
            AdapterProbeNotRun => new AdapterProbeNotRun { Evidence = evidence },
            AdapterProbeNotApplicable => new AdapterProbeNotApplicable { Evidence = evidence },
            AdapterProbeApplicable => new AdapterProbeApplicable { Evidence = evidence },
            AdapterProbeBlocked blocked => new AdapterProbeBlocked
            {
                Evidence = evidence,
                Diagnostics = Diagnostics(blocked.Diagnostics)
            },
            _ => probe with { Evidence = evidence }
        };
    }

    static AdapterExecutionResult Execution(AdapterExecutionResult execution)
    {
        var diagnostics = Diagnostics(execution.Diagnostics);
        return execution switch
        {
            AdapterExecutionNotRun => new AdapterExecutionNotRun { Diagnostics = diagnostics },
            AdapterExecutionFailed => new AdapterExecutionFailed { Diagnostics = diagnostics },
            AdapterExecutionRejected rejected => new AdapterExecutionRejected
            {
                Diagnostics = diagnostics,
                AdmissionDiagnostics = AdmissionDiagnostics(rejected.AdmissionDiagnostics)
            },
            AdapterExecutionCompleted completed => new AdapterExecutionCompleted
            {
                Diagnostics = diagnostics,
                Contribution = Contribution(completed.Contribution)
            },
            _ => execution with { Diagnostics = diagnostics }
        };
    }

    static AdapterContributionSnapshot Contribution(AdapterContributionSnapshot contribution)
    {
        var descriptor = Descriptor(contribution.Descriptor);
        var producer = descriptor.Identity;
        var facts = contribution.Facts.Select(Fact);
        return new AdapterContributionSnapshot
        {
            Descriptor = descriptor,
            Facts =
            [
                .. facts
                    .OrderBy(_ => producer.Id, StringComparer.Ordinal)
                    .ThenBy(_ => producer.Version, StringComparer.Ordinal)
                    .ThenBy(fact => fact.Id.Value, StringComparer.Ordinal)
                    .ThenBy(fact => fact.Subject.Value, StringComparer.Ordinal)
                    .ThenBy(Structural.FactFamily)
                    .ThenBy(Structural.FactDefinition, StringComparer.Ordinal)
                    .ThenBy(fact => Structural.Evidence(fact.Evidence), StringComparer.Ordinal)
            ],
            Diagnostics = Diagnostics(contribution.Diagnostics)
        };
    }

    static ImmutableArray<AdapterProbeEvidence> ProbeEvidence(IEnumerable<AdapterProbeEvidence> evidence) =>
    [
        .. evidence
            .Select(item => new AdapterProbeEvidence
            {
                Description = item.Description,
                ApiCapability = item.ApiCapability is null ? null : new AdapterApiCapability { Id = item.ApiCapability.Id },
                Source = item.Source is null ? null : Source(item.Source),
                Subject = item.Subject is null ? null : Subject(item.Subject)
            })
            .OrderBy(item => item.ApiCapability?.Id, StringComparer.Ordinal)
            .ThenBy(item => item.Source?.FileIdentity?.Project, StringComparer.Ordinal)
            .ThenBy(item => item.Source?.FileIdentity?.Path, StringComparer.Ordinal)
            .ThenBy(item => item.Source?.Path, StringComparer.Ordinal)
            .ThenBy(item => item.Source?.StartLine)
            .ThenBy(item => item.Source?.StartColumn)
            .ThenBy(item => item.Source?.EndLine)
            .ThenBy(item => item.Source?.EndColumn)
            .ThenBy(item => item.Subject?.Value, StringComparer.Ordinal)
            .ThenBy(item => item.Description, StringComparer.Ordinal)
            .ThenBy(Structural.ProbeEvidence, StringComparer.Ordinal)
    ];

    static ImmutableArray<AdapterContributionAdmissionDiagnostic> AdmissionDiagnostics(
        IEnumerable<AdapterContributionAdmissionDiagnostic> diagnostics) =>
    [
        .. diagnostics
            .Select(diagnostic => new AdapterContributionAdmissionDiagnostic
            {
                Code = diagnostic.Code,
                Path = diagnostic.Path,
                Message = diagnostic.Message,
                Fact = diagnostic.Fact is null ? null : new FactId { Value = diagnostic.Fact.Value },
                Subject = diagnostic.Subject is null ? null : Subject(diagnostic.Subject),
                Source = diagnostic.Source is null ? null : Source(diagnostic.Source)
            })
            .GroupBy(Structural.AdmissionDiagnostic, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(diagnostic => diagnostic.Code)
            .ThenBy(diagnostic => diagnostic.Path, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Fact?.Value, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Subject?.Value, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Source is null ? 0 : 1)
            .ThenBy(diagnostic => diagnostic.Source?.FileIdentity?.Project, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Source?.FileIdentity?.Path, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Source?.Path, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Source?.StartLine)
            .ThenBy(diagnostic => diagnostic.Source?.StartColumn)
            .ThenBy(diagnostic => diagnostic.Source?.EndLine)
            .ThenBy(diagnostic => diagnostic.Source?.EndColumn)
            .ThenBy(Structural.AdmissionDiagnostic, StringComparer.Ordinal)
    ];

    static GenerationDiagnostic Diagnostic(GenerationDiagnostic diagnostic) => new()
    {
        Code = diagnostic.Code,
        Severity = diagnostic.Severity,
        Message = diagnostic.Message,
        Outcome = diagnostic.Outcome,
        Facts =
        [
            .. diagnostic.Facts
                .Select(fact => new FactId { Value = fact.Value })
                .Distinct()
                .OrderBy(fact => fact.Value, StringComparer.Ordinal)
        ],
        Source = diagnostic.Source is null ? null : Source(diagnostic.Source),
        Subject = diagnostic.Subject is null ? null : Subject(diagnostic.Subject),
        Lineage = diagnostic.Lineage is { } lineage ? DiagnosticLineage(lineage) : null
    };

    static Evidence Evidence(Evidence evidence) => new()
    {
        Adapter = new AdapterIdentity { Id = evidence.Adapter.Id, Version = evidence.Adapter.Version },
        Strength = evidence.Strength,
        Source = evidence.Source is null ? null : Source(evidence.Source),
        Explanation = evidence.Explanation
    };

    static SourceRange Source(SourceRange source) => new()
    {
        Path = source.Path,
        FileIdentity = source.FileIdentity is null
            ? null
            : new SourceFileIdentity
            {
                Project = source.FileIdentity.Project,
                Path = source.FileIdentity.Path
            },
        StartLine = source.StartLine,
        StartColumn = source.StartColumn,
        EndLine = source.EndLine,
        EndColumn = source.EndColumn
    };

    static GenerationFactLineage DiagnosticLineage(GenerationFactLineage lineage)
    {
        var dependencies = lineage.Inputs.Zip(lineage.Evidence, (id, evidence) => (Id: id, Evidence: evidence))
            .OrderBy(dependency => dependency.Id.Value, StringComparer.Ordinal).ThenBy(dependency => Structural.Evidence(dependency.Evidence), StringComparer.Ordinal).ToArray();
        return new()
        {
            Producer = Rule(lineage.Producer), Inputs = [.. dependencies.Select(dependency => new FactId { Value = dependency.Id.Value })],
            Evidence = [.. dependencies.Select(dependency => Evidence(dependency.Evidence))]
        };
    }

    static SubjectId Subject(SubjectId subject) => new() { Value = subject.Value };

    static BackendScopeKey? Scope(BackendScopeKey? key) => key is null ? null : new BackendScopeKey { Store = Subject(key.Store), EventModelName = key.EventModelName };

    static ArtifactKey ArtifactKey(ArtifactKey key) => new()
    {
        Subject = Subject(key.Subject),
        Kind = key.Kind,
        Scope = Scope(key.Scope)
    };

    static TypeReferenceDefinition TypeReference(TypeReferenceDefinition type) => new()
    {
        Name = type.Name,
        Subject = type.Subject is null ? null : Subject(type.Subject),
        TargetArtifactKind = type.TargetArtifactKind,
        Scope = Scope(type.Scope),
        IsCollection = type.IsCollection,
        IsOptional = type.IsOptional
    };

    static ArtifactDefinition Artifact(ArtifactDefinition definition) => new()
    {
        Key = ArtifactKey(definition.Key),
        Name = definition.Name,
        Description = definition.Description,
        File = definition.File,
        Properties =
        [
            .. definition.Properties.Select(property => new PropertyDefinition
            {
                Name = property.Name,
                Type = TypeReference(property.Type),
                IsIdentifier = property.IsIdentifier
            })
        ]
    };

    static ArtifactDeclarationDefinition ArtifactDeclaration(ArtifactDeclarationDefinition definition) => new()
    {
        Artifact = ArtifactKey(definition.Artifact),
        Name = definition.Name,
        Description = definition.Description,
        File = definition.File,
        NameConflictYield = definition.NameConflictYield is { } marker ? marker with { Diagnostic = Diagnostic(marker.Diagnostic) } : null
    };

    static ArtifactMemberKey ArtifactMemberKey(ArtifactMemberKey member) => new()
    {
        Artifact = ArtifactKey(member.Artifact),
        Name = member.Name
    };

    static ArtifactMemberDeclarationDefinition ArtifactMemberDeclaration(ArtifactMemberDeclarationDefinition definition) => new()
    {
        Member = ArtifactMemberKey(definition.Member),
        DeclarationOrder = definition.DeclarationOrder
    };

    static ArtifactMemberTypeUseDefinition ArtifactMemberTypeUse(ArtifactMemberTypeUseDefinition definition) => new()
    {
        Member = ArtifactMemberKey(definition.Member),
        Type = TypeUse(definition.Type)
    };

    static TypeUseCompatibilityProjectionFact Projection(TypeUseCompatibilityProjectionFact fact) => fact.Definition is not { } definition ? fact : fact with
    {
        Definition = definition with
        {
            TypeUse = definition.TypeUse is { } id ? new() { Value = id.Value } : null!,
            Type = definition.Type is { } type ? TypeUse(type) : null!
        }
    };

    static TypeUseDefinition TypeUse(TypeUseDefinition type) => type with
    {
        ObservedTypeSubject = type.ObservedTypeSubject is { } subject ? Subject(subject) : null,
        ObservedScope = Scope(type.ObservedScope),
        Shape = type.Shape is { } shape ? [.. shape] : null!,
        Nodes = type.Nodes is { } nodes ? [.. nodes.Select(TypeUseNode)] : null
    };

    static TypeUseNodeObservation TypeUseNode(TypeUseNodeObservation node) => node is null ? null! : node with
    {
        Subject = node.Subject is { } subject ? Subject(subject) : null,
        Scope = Scope(node.Scope)
    };

    static TypeUseBindingDefinition TypeUseBinding(TypeUseBindingDefinition definition) => new()
    {
        Member = ArtifactMemberKey(definition.Member),
        Target = ArtifactKey(definition.Target)
    };

    static ArtifactMemberRoleDefinition ArtifactMemberRole(ArtifactMemberRoleDefinition definition) => new()
    {
        Member = ArtifactMemberKey(definition.Member),
        Role = definition.Role
    };

    static ArtifactPlacement Placement(ArtifactPlacement placement) => new()
    {
        Module = placement.Module,
        Features = [.. placement.Features],
        Slice = placement.Slice,
        SliceKind = placement.SliceKind
    };

    static RelationshipDefinition Relationship(RelationshipDefinition definition) => new()
    {
        Key = new RelationshipKey
        {
            Kind = definition.Key.Kind,
            Source = Subject(definition.Key.Source),
            Target = Subject(definition.Key.Target),
            Discriminator = definition.Key.Discriminator,
            SourceScope = Scope(definition.Key.SourceScope),
            TargetScope = Scope(definition.Key.TargetScope)
        },
        SourceMember = definition.SourceMember,
        TargetMember = definition.TargetMember,
        IsCollection = definition.IsCollection,
        IsOptional = definition.IsOptional
    };

    static ConceptRepresentationDefinition ConceptRepresentation(ConceptRepresentationDefinition definition) => new()
    {
        Concept = Subject(definition.Concept),
        Kind = definition.Kind,
        Primitive = definition.Primitive,
        EnumerationValues = [.. definition.EnumerationValues],
        EnumerationNumbers = definition.EnumerationNumbers is { } numbers ? [.. numbers] : null
    };

    static ConceptAttributeDefinition ConceptAttribute(ConceptAttributeDefinition definition) => new()
    {
        Concept = Subject(definition.Concept),
        Kind = definition.Kind,
        Name = definition.Name,
        Reason = definition.Reason
    };

    static ConceptValidationRuleDefinition ConceptValidationRule(ConceptValidationRuleDefinition definition) => new()
    {
        Concept = Subject(definition.Concept),
        RuleIdentity = definition.RuleIdentity,
        Kind = definition.Kind,
        Predicate = definition.Predicate,
        Message = definition.Message,
        ImplementationFile = definition.ImplementationFile
    };

    static SpecificationScenarioKey SpecificationScenarioKey(SpecificationScenarioKey key) => new()
    {
        Scenario = Subject(key.Scenario)
    };

    static SpecificationStepKey SpecificationStepKey(SpecificationStepKey key) => new()
    {
        Scenario = SpecificationScenarioKey(key.Scenario),
        Index = key.Index
    };

    static SpecificationValueKey SpecificationValueKey(SpecificationValueKey key) => new()
    {
        Step = SpecificationStepKey(key.Step),
        Path = [.. key.Path]
    };

    static SpecificationScenarioDefinition SpecificationScenario(SpecificationScenarioDefinition definition) => new()
    {
        Key = SpecificationScenarioKey(definition.Key),
        Name = definition.Name,
        TargetArtifact = ArtifactKey(definition.TargetArtifact),
        Steps = [.. definition.Steps.Select(SpecificationStepKey)]
    };

    static SpecificationStepDefinition SpecificationStep(SpecificationStepDefinition definition) => new()
    {
        Key = SpecificationStepKey(definition.Key),
        Phase = definition.Phase,
        Kind = definition.Kind,
        Artifact = definition.Artifact is null ? null : ArtifactKey(definition.Artifact),
        ErrorCode = definition.ErrorCode,
        ErrorMessage = definition.ErrorMessage,
        Values = [.. definition.Values.Select(SpecificationValueKey)]
    };

    static SpecificationValueDefinition SpecificationValue(SpecificationValueDefinition definition) => new()
    {
        Key = SpecificationValueKey(definition.Key),
        Kind = definition.Kind,
        Type = definition.Type is null ? null : TypeReference(definition.Type),
        Scalar = definition.Scalar,
        Children = [.. definition.Children.Select(SpecificationValueKey)]
    };
}
