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

    public static GenerationFact Fact(GenerationFact fact)
    {
        var id = new FactId { Value = fact.Id.Value };
        var subject = Subject(fact.Subject);
        var evidence = Evidence(fact.Evidence);
        return fact switch
        {
            ArtifactFact artifact => new ArtifactFact
            {
                Id = id,
                Subject = subject,
                Evidence = evidence,
                Definition = Artifact(artifact.Definition)
            },
            ArtifactPlacementFact placement => new ArtifactPlacementFact
            {
                Id = id,
                Subject = subject,
                Evidence = evidence,
                Artifact = ArtifactKey(placement.Artifact),
                Placement = Placement(placement.Placement)
            },
            ArtifactDeclarationFact declaration => new ArtifactDeclarationFact
            {
                Id = id,
                Subject = subject,
                Evidence = evidence,
                Definition = ArtifactDeclaration(declaration.Definition)
            },
            ArtifactMemberDeclarationFact member => new ArtifactMemberDeclarationFact
            {
                Id = id,
                Subject = subject,
                Evidence = evidence,
                Definition = ArtifactMemberDeclaration(member.Definition)
            },
            ArtifactMemberTypeUseFact typeUse => new ArtifactMemberTypeUseFact
            {
                Id = id,
                Subject = subject,
                Evidence = evidence,
                Definition = ArtifactMemberTypeUse(typeUse.Definition)
            },
            TypeUseBindingFact binding => new TypeUseBindingFact
            {
                Id = id,
                Subject = subject,
                Evidence = evidence,
                Definition = TypeUseBinding(binding.Definition)
            },
            ArtifactMemberRoleFact role => new ArtifactMemberRoleFact
            {
                Id = id,
                Subject = subject,
                Evidence = evidence,
                Definition = ArtifactMemberRole(role.Definition)
            },
            RelationshipFact relationship => new RelationshipFact
            {
                Id = id,
                Subject = subject,
                Evidence = evidence,
                Definition = Relationship(relationship.Definition)
            },
            ConceptRepresentationFact representation => new ConceptRepresentationFact
            {
                Id = id,
                Subject = subject,
                Evidence = evidence,
                Definition = ConceptRepresentation(representation.Definition)
            },
            ConceptAttributeFact attribute => new ConceptAttributeFact
            {
                Id = id,
                Subject = subject,
                Evidence = evidence,
                Definition = ConceptAttribute(attribute.Definition)
            },
            ConceptValidationRuleFact validation => new ConceptValidationRuleFact
            {
                Id = id,
                Subject = subject,
                Evidence = evidence,
                Definition = ConceptValidationRule(validation.Definition)
            },
            SpecificationScenarioFact scenario => new SpecificationScenarioFact
            {
                Id = id,
                Subject = subject,
                Evidence = evidence,
                Definition = SpecificationScenario(scenario.Definition)
            },
            SpecificationStepFact step => new SpecificationStepFact
            {
                Id = id,
                Subject = subject,
                Evidence = evidence,
                Definition = SpecificationStep(step.Definition)
            },
            SpecificationValueFact value => new SpecificationValueFact
            {
                Id = id,
                Subject = subject,
                Evidence = evidence,
                Definition = SpecificationValue(value.Definition)
            },
            _ => fact with { Id = id, Subject = subject, Evidence = evidence }
        };
    }

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
        Subject = diagnostic.Subject is null ? null : Subject(diagnostic.Subject)
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

    static SubjectId Subject(SubjectId subject) => new() { Value = subject.Value };

    static ArtifactKey ArtifactKey(ArtifactKey key) => new()
    {
        Subject = Subject(key.Subject),
        Kind = key.Kind
    };

    static TypeReferenceDefinition TypeReference(TypeReferenceDefinition type) => new()
    {
        Name = type.Name,
        Subject = type.Subject is null ? null : Subject(type.Subject),
        TargetArtifactKind = type.TargetArtifactKind,
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
        File = definition.File
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
        Type = new TypeUseDefinition
        {
            Name = definition.Type.Name,
            ObservedTypeSubject = definition.Type.ObservedTypeSubject is null ? null : Subject(definition.Type.ObservedTypeSubject),
            Shape = [.. definition.Type.Shape]
        }
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
            Discriminator = definition.Key.Discriminator
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
        EnumerationValues = [.. definition.EnumerationValues]
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
