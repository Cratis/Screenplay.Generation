// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Screenplay.Generation;

static class Structural
{
    public static string SemanticKey(string family, params string?[] parts) => Node([family, .. parts]);

    public static string ArtifactKey(ArtifactKey key) =>
        WithScopes(Node(key.Subject.Value, Integer((int)key.Kind)), key.Scope);

    public static string BackendScopeKey(BackendScopeKey key) => Node(key.Store?.Value, key.EventModelName);

    public static string BackendScope(BackendScopeDefinition definition) => Node(BackendScopeKey(definition.Key), definition.Name);

    public static string WithScopes(string legacy, params BackendScopeKey?[] scopes) =>
        scopes.All(scope => scope is null) ? legacy : Node(legacy, "backend-scopes", Sequence(scopes, scope => scope is null ? null : BackendScopeKey(scope)));

    public static string Artifact(ArtifactDefinition definition) =>
        Node(
            ArtifactKey(definition.Key),
            definition.Name,
            definition.Description,
            Sequence(definition.Properties, Property));

    public static string ArtifactDeclaration(ArtifactDeclarationDefinition definition)
    {
        var declaration = Node(ArtifactKey(definition.Artifact), definition.Name, definition.Description);
        return definition.NameConflictYield is { } marker
            ? Node(declaration, "name-conflict-yield", Diagnostic(marker.Diagnostic))
            : declaration;
    }

    public static string ArtifactMemberKey(ArtifactMemberKey member) =>
        Node(ArtifactKey(member.Artifact), member.Name);

    public static string ArtifactMemberDeclaration(ArtifactMemberDeclarationDefinition definition) =>
        Node(ArtifactMemberKey(definition.Member), Integer(definition.DeclarationOrder));

    public static string ArtifactMemberTypeUse(ArtifactMemberTypeUseDefinition definition) =>
        TypeUseCompatibilityProjectionValidation.HasLegacyJaggedArrayData(definition)
            ? Node(Node(ArtifactMemberKey(definition.Member), TypeUse(definition.Type)), "jagged-array-view", definition.UseLegacyJaggedArrayReference ? "1" : "0", Sequence(definition.CollectionOrigins ?? [], origin => Integer((int)origin)))
            : Node(ArtifactMemberKey(definition.Member), TypeUse(definition.Type));

    public static string TypeUse(TypeUseDefinition definition)
    {
        var legacy = Node(definition.Name, definition.ObservedTypeSubject?.Value, Shape(definition.Shape));
        var scoped = WithScopes(legacy, definition.ObservedScope);
        return definition.Nodes is { } nodes ? Node(scoped, "type-use-nodes", Sequence(nodes, TypeUseNode)) : scoped;
    }

    public static string TypeUseProjection(TypeUseCompatibilityProjectionDefinition? definition) => definition is null
        ? Node("invalid-type-use-compatibility-projection")
        : Node("type-use-compatibility-projection", definition.TypeUse?.Value, Integer((int)definition.Profile), Integer(definition.Version), Integer(definition.SourceNodeIndex), ProjectedType(definition.Type));

    public static string TypeUseBinding(TypeUseBindingDefinition definition) =>
        Node(ArtifactMemberKey(definition.Member), ArtifactKey(definition.Target));

    public static string ArtifactMemberRole(ArtifactMemberRoleDefinition definition) =>
        Node(ArtifactMemberKey(definition.Member), Integer((int)definition.Role));

    public static string ArtifactUsageDeclaration(ArtifactUsageDeclarationDefinition definition) =>
        Node(definition.Declaration.Value, ArtifactKey(definition.Artifact));

    public static string MethodArtifactUsage(MethodArtifactUsageDefinition definition)
    {
        var legacy = WithScopes(Node(definition.Method.Value, definition.Declaration.Value, Integer((int)definition.Kind)), definition.SourceScope, definition.TargetScope);
        return definition.ExecutionOrder is { } order ? Node(legacy, Integer(order)) : legacy;
    }

    public static string DeclarationMember(DeclarationMemberKey member) => WithScopes(Node(member.Declaration.Value, member.Name), member.Scope);

    public static string MethodSupport(MethodFactSupportAlternative? alternative) => alternative is null
        ? Node("missing-alternative")
        : WithScopes(Node(alternative.Method?.Value, Sequence((alternative.EnclosingExecutables ?? []).Select(subject => subject?.Value).Distinct().Order(StringComparer.Ordinal), value => value), Sequence((alternative.Diagnostics ?? []).Select(diagnostic => diagnostic is null ? null : Diagnostic(diagnostic)).Distinct().Order(StringComparer.Ordinal), value => value)), alternative.Scope);

    public static string EntryPoint(EntryPointDefinition definition) =>
        Node(definition.Method.Value, ArtifactKey(definition.Artifact));

    public static string Placement(ArtifactPlacement placement) =>
        Node(
            placement.Module,
            Sequence(placement.Features, value => value),
            placement.Slice,
            Integer((int)placement.SliceKind));

    public static string RelationshipKey(RelationshipKey key) =>
        WithScopes(Node(Integer((int)key.Kind), key.Source.Value, key.Target.Value, key.Discriminator), key.SourceScope, key.TargetScope);

    public static string Relationship(RelationshipDefinition definition) =>
        Node(
            RelationshipKey(definition.Key),
            definition.SourceMember,
            definition.TargetMember,
            Boolean(definition.IsCollection),
            Boolean(definition.IsOptional));

    public static string ConceptRepresentation(ConceptRepresentationDefinition definition)
    {
        var legacy = Node(
            definition.Concept.Value,
            Integer((int)definition.Kind),
            NullableInteger(definition.Primitive is null ? null : (int)definition.Primitive.Value),
            Sequence(definition.EnumerationValues, value => value));
        return definition.EnumerationNumbers is { } numbers
            ? Node(legacy, "enumeration-numbers", Sequence(numbers, Integer))
            : legacy;
    }

    public static string ConceptAttributeKey(ConceptAttributeDefinition definition) =>
        Node(definition.Concept.Value, Integer((int)definition.Kind), definition.Name);

    public static string ConceptAttribute(ConceptAttributeDefinition definition) =>
        Node(ConceptAttributeKey(definition), definition.Reason);

    public static string ConceptValidationRuleKey(ConceptValidationRuleDefinition definition) =>
        Node(definition.Concept.Value, definition.RuleIdentity);

    public static string ConceptValidationRule(ConceptValidationRuleDefinition definition) =>
        Node(
            ConceptValidationRuleKey(definition),
            Integer((int)definition.Kind),
            definition.Predicate,
            definition.Message);

    public static string SpecificationScenarioKey(SpecificationScenarioKey key) =>
        Node(key.Scenario.Value);

    public static string SpecificationStepKey(SpecificationStepKey key) =>
        Node(SpecificationScenarioKey(key.Scenario), Integer(key.Index));

    public static string SpecificationValueKey(SpecificationValueKey key) =>
        Node(SpecificationStepKey(key.Step), Sequence(key.Path, value => value));

    public static string SpecificationScenario(SpecificationScenarioDefinition definition) =>
        Node(
            SpecificationScenarioKey(definition.Key),
            definition.Name,
            ArtifactKey(definition.TargetArtifact),
            Sequence(definition.Steps, SpecificationStepKey));

    public static string SpecificationStep(SpecificationStepDefinition definition) =>
        Node(
            SpecificationStepKey(definition.Key),
            Integer((int)definition.Phase),
            Integer((int)definition.Kind),
            definition.Artifact is null ? null : ArtifactKey(definition.Artifact),
            definition.ErrorCode,
            definition.ErrorMessage,
            Sequence(definition.Values, SpecificationValueKey));

    public static string SpecificationValue(SpecificationValueDefinition definition) =>
        Node(
            SpecificationValueKey(definition.Key),
            Integer((int)definition.Kind),
            definition.Type is null ? null : TypeReference(definition.Type),
            definition.Scalar,
            Sequence(definition.Children, SpecificationValueKey));

    public static string FactDefinition(GenerationFact fact) => fact switch
    {
        BackendScopeFact scope => Node("backend-scope", BackendScope(scope.Definition)),
        ArtifactFact artifact => Node("artifact", Artifact(artifact.Definition)),
        ArtifactPlacementFact placement => MixedSliceFallbackStructure.WithIdentity(Node("placement", ArtifactKey(placement.Artifact), Placement(placement.Placement)), placement.MixedSliceFallback),
        ArtifactSourcePlacementCandidateFact candidate => SourcePlacementCandidateStructure.Definition(candidate.Definition),
        ArtifactDeclarationFact declaration => Node("artifact-declaration", ArtifactDeclaration(declaration.Definition)),
        ArtifactMemberDeclarationFact member => Node("artifact-member-declaration", ArtifactMemberDeclaration(member.Definition)),
        _ => GranularFactDefinition(fact)
    };

    public static string Fact(GenerationFact fact) =>
        Node(
            fact.Id.Value,
            fact.Subject.Value,
            Integer(FactFamily(fact)),
            FactDefinition(fact),
            Evidence(fact.Evidence));

    public static string Evidence(Evidence evidence) =>
        Node(
            Identity(evidence.Adapter),
            Integer((int)evidence.Strength),
            evidence.Source is null ? null : Source(evidence.Source),
            evidence.Explanation);

    public static string Diagnostic(GenerationDiagnostic diagnostic)
    {
        var identity = Node(
            diagnostic.Code,
            Integer((int)diagnostic.Severity),
            diagnostic.Message,
            NullableInteger(diagnostic.Outcome is null ? null : (int)diagnostic.Outcome.Value),
            Sequence((diagnostic.Facts.IsDefault ? Enumerable.Empty<FactId>() : diagnostic.Facts).OrderBy(fact => fact?.Value, StringComparer.Ordinal), fact => fact?.Value),
            diagnostic.Source is null ? null : Source(diagnostic.Source),
            diagnostic.Subject?.Value);
        return diagnostic.Lineage is null ? identity : Node(identity, DiagnosticLineage(diagnostic.Lineage));
    }

    public static string DiagnosticLineage(GenerationFactLineage lineage) => Node(
        lineage.Producer.Id,
        lineage.Producer.Version,
        Sequence(lineage.Inputs.Zip(lineage.Evidence, (id, evidence) => Node(id.Value, Evidence(evidence))).Order(StringComparer.Ordinal), value => value));

    public static string AdmissionDiagnostic(AdapterContributionAdmissionDiagnostic diagnostic) =>
        Node(
            Integer((int)diagnostic.Code),
            diagnostic.Path,
            diagnostic.Message,
            diagnostic.Fact?.Value,
            diagnostic.Subject?.Value,
            diagnostic.Source is null ? null : Source(diagnostic.Source));

    public static string ProbeEvidence(AdapterProbeEvidence evidence) =>
        Node(
            evidence.Description,
            evidence.ApiCapability?.Id,
            evidence.Source is null ? null : Source(evidence.Source),
            evidence.Subject?.Value);

    public static string Descriptor(AdapterDescriptor descriptor) =>
        Node(
            Identity(descriptor.Identity),
            Integer((int)descriptor.SourceLanguage),
            Integer((int)descriptor.Category),
            VersionRange(descriptor.CompatibleGenerationVersions),
            Sequence(descriptor.RequiredHostCapabilities, capability => Integer((int)capability)),
            Sequence(descriptor.RequiredApiCapabilities, capability => capability.Id),
            Sequence(descriptor.EmittedFactCapabilities, capability => Integer((int)capability)));

    public static string AdapterRecord(AdapterRunRecord record) =>
        Node(
            Boolean(record.Considered),
            Boolean(record.Probed),
            Boolean(record.Executed),
            Descriptor(record.Descriptor),
            Probe(record.Probe),
            Execution(record.Execution),
            Integer((int)record.Disposition));

    internal static int FactFamily(GenerationFact fact) => fact switch
    {
        ArtifactFact => (int)GenerationFactCapability.Artifact,
        ArtifactPlacementFact => (int)GenerationFactCapability.ArtifactPlacement,
        ArtifactSourcePlacementCandidateFact => (int)GenerationFactCapability.ArtifactSourcePlacementCandidate,
        RelationshipFact => (int)GenerationFactCapability.Relationship,
        _ => ConceptFamily(fact)
    };

    static int ConceptFamily(GenerationFact fact) => fact switch
    {
        ConceptRepresentationFact => (int)GenerationFactCapability.ConceptRepresentation,
        ConceptAttributeFact => (int)GenerationFactCapability.ConceptAttribute,
        ConceptValidationRuleFact => (int)GenerationFactCapability.ConceptValidationRule,
        _ => SpecificationFamily(fact)
    };

    static string Shape(IReadOnlyList<TypeUseShapeKind>? shape) => shape is null
        ? Node("invalid-type-use-shape")
        : Sequence(shape, node => Integer((int)node));

    static string? ProjectedType(TypeUseDefinition? type) => type is null ? null : TypeUse(type);

    static string TypeUseNode(TypeUseNodeObservation? node) => node is null
        ? Node("invalid-type-use-node")
        : WithScopes(Node(node.Name, node.Subject?.Value), node.Scope);

    static int SpecificationFamily(GenerationFact fact) => fact switch
    {
        SpecificationScenarioFact => (int)GenerationFactCapability.SpecificationScenario,
        SpecificationStepFact => (int)GenerationFactCapability.SpecificationStep,
        SpecificationValueFact => (int)GenerationFactCapability.SpecificationValue,
        _ => GranularFamily(fact)
    };

    static int GranularFamily(GenerationFact fact) => fact switch
    {
        ArtifactDeclarationFact => (int)GenerationFactCapability.ArtifactDeclaration,
        ArtifactMemberDeclarationFact => (int)GenerationFactCapability.ArtifactMemberDeclaration,
        ArtifactMemberTypeUseFact => (int)GenerationFactCapability.ArtifactMemberTypeUse,
        TypeUseBindingFact => (int)GenerationFactCapability.TypeUseBinding,
        ArtifactMemberRoleFact => (int)GenerationFactCapability.ArtifactMemberRole,
        _ => TypeUseOmissionFamily(fact)
    };

    static int TypeUseOmissionFamily(GenerationFact fact) => fact switch
    {
        MethodDiagnosticProjectionFact => (int)GenerationFactCapability.MethodDiagnosticProjection,
        OccurrenceOmissionDiagnosticFact => (int)GenerationFactCapability.OccurrenceOmissionDiagnostic,
        TypeUseTargetOmittedFact => (int)GenerationFactCapability.TypeUseTargetOmitted,
        _ => ProjectionFamily(fact)
    };

    static int ProjectionFamily(GenerationFact fact) => fact switch
    {
        TypeUseCompatibilityProjectionFact => (int)GenerationFactCapability.TypeUseCompatibilityProjection,
        TypeUseBindingPolicyFact => (int)GenerationFactCapability.TypeUseBindingPolicy,
        DeclarationInputMemberTargetOmittedFact => (int)GenerationFactCapability.DeclarationInputMemberTargetOmitted,
        ArtifactMemberRoleSelectionFact => (int)GenerationFactCapability.ArtifactMemberRoleSelection,
        _ => UsageFamily(fact)
    };

    static int UsageFamily(GenerationFact fact) => fact switch
    {
        ArtifactUsageDeclarationFact => (int)GenerationFactCapability.ArtifactUsageDeclaration,
        MethodArtifactUsageFact => (int)GenerationFactCapability.MethodArtifactUsage,
        EntryPointFact => (int)GenerationFactCapability.EntryPoint,
        BackendScopeFact => (int)GenerationFactCapability.BackendScope,
        _ => ObservationFamily(fact)
    };

    static int ObservationFamily(GenerationFact fact) => fact switch
    {
        MethodExecutionClassificationFact => (int)GenerationFactCapability.MethodExecutionClassification,
        MethodFactSupportFact => (int)GenerationFactCapability.MethodFactSupport,
        _ => InputObservationFamily(fact)
    };

    static int InputObservationFamily(GenerationFact fact) => fact switch
    {
        DeclarationInputMemberDeclarationFact => (int)GenerationFactCapability.DeclarationInputMemberDeclaration,
        DeclarationInputMemberTypeUseFact => (int)GenerationFactCapability.DeclarationInputMemberTypeUse,
        EntryPointInputTransferFact => (int)GenerationFactCapability.EntryPointInputTransfer,
        MethodDeclarationExecutionFact => (int)GenerationFactCapability.MethodDeclarationExecution,
        _ => int.MaxValue
    };

    static string GranularFactDefinition(GenerationFact fact) => fact switch
    {
        ArtifactMemberTypeUseFact typeUse => Node("artifact-member-type-use", ArtifactMemberTypeUse(typeUse.Definition)),
        TypeUseBindingFact binding => Node("type-use-binding", TypeUseBinding(binding.Definition)),
        ArtifactMemberRoleFact role => Node("artifact-member-role", ArtifactMemberRole(role.Definition)),
        ArtifactUsageDeclarationFact declaration => Node("artifact-usage-declaration", ArtifactUsageDeclaration(declaration.Definition)),
        MethodArtifactUsageFact usage => Node("method-artifact-usage", MethodArtifactUsage(usage.Definition)),
        EntryPointFact entry => Node("entry-point", EntryPoint(entry.Definition)),
        _ => TypeUseOmissionDefinition(fact)
    };

    static string TypeUseOmissionDefinition(GenerationFact fact) => fact switch
    {
        MethodDiagnosticProjectionFact projection => DiagnosticProjection(projection),
        OccurrenceOmissionDiagnosticFact candidate => OmissionDiagnostic(candidate),
        TypeUseTargetOmittedFact omission => Node("type-use-target-omitted", ArtifactMemberKey(omission.Definition.Member), Diagnostic(omission.Definition.Diagnostic)),
        TypeUseCompatibilityProjectionFact projection => TypeUseProjection(projection.Definition),
        TypeUseBindingPolicyFact policy => Node("type-use-binding-policy", policy.Definition.TypeUse.Value, Integer((int)policy.Definition.Policy)),
        _ => ObservationFactDefinition(fact)
    };

    static string OmissionDiagnostic(OccurrenceOmissionDiagnosticFact candidate) => !OccurrenceOmissionDiagnosticValidation.Valid(candidate)
        ? Node("invalid-occurrence-omission-diagnostic")
        : Node("occurrence-omission-diagnostic", candidate.Definition.Family.Value, ArtifactKey(candidate.Definition.Occurrence), Integer((int)candidate.Definition.Policy), Integer((int)candidate.Definition.Preference), Diagnostic(candidate.Definition.Diagnostic));

    static string DiagnosticProjection(MethodDiagnosticProjectionFact projection) => WithScopes(Node("method-diagnostic-projection", projection.Definition?.Method?.Value, Integer((int)(projection.Definition?.Mode ?? MethodDiagnosticProjectionMode.Unknown)), Integer((int)(projection.Definition?.EntryPointRole ?? ArtifactKind.Unknown)), projection.Definition?.Diagnostic is { } payload ? Diagnostic(payload) : null), projection.Definition?.Scope);

    static string ObservationFactDefinition(GenerationFact fact) => fact switch
    {
        MethodExecutionClassificationFact classification => MethodExecutionDefinition(classification),
        DeclarationInputMemberDeclarationFact declaration => Node("declaration-input-member", DeclarationMember(declaration.Definition.Member), Integer(declaration.Definition.DeclarationOrder)),
        DeclarationInputMemberTargetOmittedFact omission => Node("declaration-input-target-omitted", DeclarationMember(omission.Definition.Member), Diagnostic(omission.Definition.Diagnostic)),
        DeclarationInputMemberTypeUseFact typeUse => Node("declaration-input-type", DeclarationMember(typeUse.Definition.Member), TypeUse(typeUse.Definition.Type)),
        _ => ExecutionFactDefinition(fact)
    };

    static string MethodExecutionDefinition(MethodExecutionClassificationFact fact) =>
        WithScopes(Node("method-execution", fact.Definition?.Method?.Value, Integer((int)(fact.Definition?.Kind ?? MethodExecutionKind.Unknown)), fact.Definition?.OmissionReason is { } reason ? Diagnostic(reason) : null), fact.Definition?.Scope);

    static string ExecutionFactDefinition(GenerationFact fact) => fact switch
    {
        MethodDeclarationExecutionFact execution => WithScopes(Node("method-declaration-execution", execution.Definition?.Method?.Value, execution.Definition?.Declaration?.Value, NullableInteger(execution.Definition?.ExecutionOrder)), execution.Definition?.SourceScope, execution.Definition?.TargetScope),
        EntryPointInputTransferFact transfer => Node("entry-input-transfer", transfer.Definition.Method.Value, ArtifactKey(transfer.Definition.Artifact), Integer((int)transfer.Definition.Kind)),
        MethodFactSupportFact support => MethodSupportDefinition(support),
        ArtifactMemberRoleSelectionFact selection => ArtifactMemberRoleSelectionStructure.Definition(selection.Definition),
        _ => DomainFactDefinition(fact)
    };

    static string MethodSupportDefinition(MethodFactSupportFact fact) =>
        Node("method-support", fact.Definition?.Candidate?.Value, Sequence((fact.Definition?.Alternatives ?? []).Select(MethodSupport).Distinct().Order(StringComparer.Ordinal), value => value));

    static string DomainFactDefinition(GenerationFact fact) => fact switch
    {
        RelationshipFact relationship => Node("relationship", Relationship(relationship.Definition)),
        ConceptRepresentationFact representation => Node("concept-representation", ConceptRepresentation(representation.Definition)),
        ConceptAttributeFact attribute => Node("concept-attribute", ConceptAttribute(attribute.Definition)),
        ConceptValidationRuleFact validation => Node("concept-validation-rule", ConceptValidationRule(validation.Definition)),
        _ => SpecificationFactDefinition(fact)
    };

    static string SpecificationFactDefinition(GenerationFact fact) => fact switch
    {
        SpecificationScenarioFact scenario => Node("specification-scenario", SpecificationScenario(scenario.Definition)),
        SpecificationStepFact step => Node("specification-step", SpecificationStep(step.Definition)),
        SpecificationValueFact value => Node("specification-value", SpecificationValue(value.Definition)),
        _ => Node("unknown", fact.GetType().FullName ?? fact.GetType().Name)
    };

    static string Property(PropertyDefinition property) =>
        Node(property.Name, TypeReference(property.Type), Boolean(property.IsIdentifier));

    static string TypeReference(TypeReferenceDefinition type)
    {
        var legacy = Node(
            type.Name,
            type.Subject?.Value,
            NullableInteger(type.TargetArtifactKind is null ? null : (int)type.TargetArtifactKind.Value),
            Boolean(type.IsCollection),
            Boolean(type.IsOptional));
        return WithScopes(legacy, type.Scope);
    }

    static string Identity(AdapterIdentity identity) => Node(identity.Id, identity.Version);

    static string VersionRange(GenerationVersionRange range) =>
        Node(range.MinimumInclusive?.ToString(), range.MaximumExclusive?.ToString());

    static string Source(SourceRange source) =>
        Node(
            source.Path,
            source.FileIdentity is null ? null : Node(source.FileIdentity.Project, source.FileIdentity.Path),
            Integer(source.StartLine),
            Integer(source.StartColumn),
            Integer(source.EndLine),
            Integer(source.EndColumn));

    static string Probe(AdapterProbeResult probe)
    {
        var type = probe switch
        {
            AdapterProbeNotRun => nameof(AdapterProbeNotRun),
            AdapterProbeNotApplicable => nameof(AdapterProbeNotApplicable),
            AdapterProbeApplicable => nameof(AdapterProbeApplicable),
            AdapterProbeBlocked => nameof(AdapterProbeBlocked),
            _ => probe.GetType().FullName ?? probe.GetType().Name
        };
        var diagnostics = probe is AdapterProbeBlocked blocked
            ? Sequence(blocked.Diagnostics, Diagnostic)
            : Sequence(Array.Empty<GenerationDiagnostic>(), Diagnostic);
        return Node(type, Sequence(probe.Evidence, ProbeEvidence), diagnostics);
    }

    static string Execution(AdapterExecutionResult execution)
    {
        var diagnostics = Sequence(execution.Diagnostics, Diagnostic);
        return execution switch
        {
            AdapterExecutionNotRun => Node(nameof(AdapterExecutionNotRun), diagnostics),
            AdapterExecutionFailed => Node(nameof(AdapterExecutionFailed), diagnostics),
            AdapterExecutionRejected rejected => Node(
                nameof(AdapterExecutionRejected),
                diagnostics,
                Sequence(rejected.AdmissionDiagnostics, AdmissionDiagnostic)),
            AdapterExecutionCompleted completed => Node(
                nameof(AdapterExecutionCompleted),
                diagnostics,
                Contribution(completed.Contribution)),
            _ => Node(execution.GetType().FullName ?? execution.GetType().Name, diagnostics)
        };
    }

    static string Contribution(AdapterContributionSnapshot contribution) =>
        Node(
            Descriptor(contribution.Descriptor),
            Sequence(contribution.Facts, Fact),
            Sequence(contribution.Diagnostics, Diagnostic));

    static string Sequence<T>(IEnumerable<T> values, Func<T, string?> value) =>
        Node([Integer(values.Count()), .. values.Select(value)]);

    static string Node(params string?[] values) => string.Concat(values.Select(Encode));

    static string Encode(string? value) => value is null
        ? "-1:"
        : $"{value.Length.ToString(CultureInfo.InvariantCulture)}:{value}";

    static string Integer(int value) => value.ToString(CultureInfo.InvariantCulture);

    static string? NullableInteger(int? value) => value?.ToString(CultureInfo.InvariantCulture);

    static string Boolean(bool value) => value ? "1" : "0";
}
