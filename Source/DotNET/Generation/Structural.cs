// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Screenplay.Generation;

static class Structural
{
    public static string SemanticKey(string family, params string?[] parts) => Node([family, .. parts]);

    public static string ArtifactKey(ArtifactKey key) =>
        Node(key.Subject.Value, Integer((int)key.Kind));

    public static string Artifact(ArtifactDefinition definition) =>
        Node(
            ArtifactKey(definition.Key),
            definition.Name,
            definition.Description,
            definition.File,
            Sequence(definition.Properties, Property));

    public static string Placement(ArtifactPlacement placement) =>
        Node(
            placement.Module,
            Sequence(placement.Features, value => value),
            placement.Slice,
            Integer((int)placement.SliceKind));

    public static string RelationshipKey(RelationshipKey key) =>
        Node(Integer((int)key.Kind), key.Source.Value, key.Target.Value, key.Discriminator);

    public static string Relationship(RelationshipDefinition definition) =>
        Node(
            RelationshipKey(definition.Key),
            definition.SourceMember,
            definition.TargetMember,
            Boolean(definition.IsCollection),
            Boolean(definition.IsOptional));

    public static string ConceptRepresentation(ConceptRepresentationDefinition definition) =>
        Node(
            definition.Concept.Value,
            Integer((int)definition.Kind),
            NullableInteger(definition.Primitive is null ? null : (int)definition.Primitive.Value),
            Sequence(definition.EnumerationValues, value => value));

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
            definition.Message,
            definition.ImplementationFile);

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
        ArtifactFact artifact => Node("artifact", Artifact(artifact.Definition)),
        ArtifactPlacementFact placement => Node("placement", ArtifactKey(placement.Artifact), Placement(placement.Placement)),
        RelationshipFact relationship => Node("relationship", Relationship(relationship.Definition)),
        ConceptRepresentationFact representation => Node("concept-representation", ConceptRepresentation(representation.Definition)),
        ConceptAttributeFact attribute => Node("concept-attribute", ConceptAttribute(attribute.Definition)),
        ConceptValidationRuleFact validation => Node("concept-validation-rule", ConceptValidationRule(validation.Definition)),
        SpecificationScenarioFact scenario => Node("specification-scenario", SpecificationScenario(scenario.Definition)),
        SpecificationStepFact step => Node("specification-step", SpecificationStep(step.Definition)),
        SpecificationValueFact value => Node("specification-value", SpecificationValue(value.Definition)),
        _ => Node("unknown", fact.GetType().FullName ?? fact.GetType().Name)
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

    public static string Diagnostic(GenerationDiagnostic diagnostic) =>
        Node(
            diagnostic.Code,
            Integer((int)diagnostic.Severity),
            diagnostic.Message,
            NullableInteger(diagnostic.Outcome is null ? null : (int)diagnostic.Outcome.Value),
            diagnostic.Source is null ? null : Source(diagnostic.Source),
            diagnostic.Subject?.Value);

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
        RelationshipFact => (int)GenerationFactCapability.Relationship,
        ConceptRepresentationFact => (int)GenerationFactCapability.ConceptRepresentation,
        ConceptAttributeFact => (int)GenerationFactCapability.ConceptAttribute,
        ConceptValidationRuleFact => (int)GenerationFactCapability.ConceptValidationRule,
        SpecificationScenarioFact => (int)GenerationFactCapability.SpecificationScenario,
        SpecificationStepFact => (int)GenerationFactCapability.SpecificationStep,
        SpecificationValueFact => (int)GenerationFactCapability.SpecificationValue,
        _ => int.MaxValue
    };

    static string Property(PropertyDefinition property) =>
        Node(property.Name, TypeReference(property.Type), Boolean(property.IsIdentifier));

    static string TypeReference(TypeReferenceDefinition type) =>
        Node(
            type.Name,
            type.Subject?.Value,
            Boolean(type.IsCollection),
            Boolean(type.IsOptional));

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
