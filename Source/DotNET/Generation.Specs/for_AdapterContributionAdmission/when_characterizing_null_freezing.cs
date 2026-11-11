// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_characterizing_null_freezing : given.a_characterized_admission
{
    string[] _observed = null!;

    void Because()
    {
        var cases = new List<(string, AdapterContributionAdmissionResult)>();
        foreach (var fact in EveryFact())
        {
            cases.Add(($"{fact.GetType().Name}-null-definition", AdmitFacts(NullDefinition(fact))));
        }

        AddNestedNullCases(cases);
        AddEnvelopeNullCases(cases);
        _observed = RenderCompact(cases);
    }

    [Fact] void should_report_the_null_freeze_diagnostics_in_order() => string.Join('\n', _observed).ShouldEqual(Expected);

    const string Expected = """
ArtifactFact-null-definition => MissingRequiredValue|[artifact].D
ArtifactFact-null-definition => MissingRequiredValue|[artifact].D.Key
ArtifactFact-null-definition => MissingRequiredValue|[artifact].D.Name
ArtifactFact-null-definition => UnknownEnumValue|[artifact].D.Key.Kind
ArtifactFact-null-definition => InvalidSubject|[artifact].D.Key.Subject
ArtifactFact-null-definition => OwnershipMismatch|[artifact].D.Key.Subject
ArtifactPlacementFact-null-definition => MissingRequiredValue|[placement].Artifact
ArtifactPlacementFact-null-definition => MissingRequiredValue|[placement].Placement
ArtifactPlacementFact-null-definition => MissingRequiredValue|[placement].Placement.Module
ArtifactPlacementFact-null-definition => MissingRequiredValue|[placement].Placement.Slice
ArtifactPlacementFact-null-definition => UnknownEnumValue|[placement].Artifact.Kind
ArtifactPlacementFact-null-definition => UnknownEnumValue|[placement].Placement.SliceKind
ArtifactPlacementFact-null-definition => InvalidSubject|[placement].Artifact.Subject
ArtifactPlacementFact-null-definition => OwnershipMismatch|[placement].Artifact.Subject
ArtifactDeclarationFact-null-definition => MissingRequiredValue|[artifact-declaration].D
ArtifactDeclarationFact-null-definition => MissingRequiredValue|[artifact-declaration].D.Artifact
ArtifactDeclarationFact-null-definition => MissingRequiredValue|[artifact-declaration].D.Name
ArtifactDeclarationFact-null-definition => UnknownEnumValue|[artifact-declaration].D.Artifact.Kind
ArtifactDeclarationFact-null-definition => InvalidSubject|[artifact-declaration].D.Artifact.Subject
ArtifactDeclarationFact-null-definition => OwnershipMismatch|[artifact-declaration].D.Artifact.Subject
ArtifactMemberDeclarationFact-null-definition => MissingRequiredValue|[artifact-member].D
ArtifactMemberDeclarationFact-null-definition => MissingRequiredValue|[artifact-member].D.Member
ArtifactMemberDeclarationFact-null-definition => MissingRequiredValue|[artifact-member].D.Member.Artifact
ArtifactMemberDeclarationFact-null-definition => MissingRequiredValue|[artifact-member].D.Member.Name
ArtifactMemberDeclarationFact-null-definition => UnknownEnumValue|[artifact-member].D.Member.Artifact.Kind
ArtifactMemberDeclarationFact-null-definition => InvalidSubject|[artifact-member].D.Member.Artifact.Subject
ArtifactMemberDeclarationFact-null-definition => OwnershipMismatch|[artifact-member].D.Member.Artifact.Subject
ArtifactMemberDeclarationFact-null-definition => InvalidDeclarationOrder|[artifact-member].D.DeclarationOrder
ArtifactMemberTypeUseFact-null-definition => MissingRequiredValue|[artifact-member-type-use].D
ArtifactMemberTypeUseFact-null-definition => MissingRequiredValue|[artifact-member-type-use].D.Member
ArtifactMemberTypeUseFact-null-definition => MissingRequiredValue|[artifact-member-type-use].D.Member.Artifact
ArtifactMemberTypeUseFact-null-definition => MissingRequiredValue|[artifact-member-type-use].D.Member.Name
ArtifactMemberTypeUseFact-null-definition => MissingRequiredValue|[artifact-member-type-use].D.Type
ArtifactMemberTypeUseFact-null-definition => MissingRequiredValue|[artifact-member-type-use].D.Type.Name
ArtifactMemberTypeUseFact-null-definition => UnknownEnumValue|[artifact-member-type-use].D.Member.Artifact.Kind
ArtifactMemberTypeUseFact-null-definition => InvalidSubject|[artifact-member-type-use].D.Member.Artifact.Subject
ArtifactMemberTypeUseFact-null-definition => OwnershipMismatch|[artifact-member-type-use].D.Member.Artifact.Subject
ArtifactMemberTypeUseFact-null-definition => InvalidTypeUseShape|[artifact-member-type-use].D.Type.Shape
TypeUseBindingFact-null-definition => MissingRequiredValue|[type-use-binding].D
TypeUseBindingFact-null-definition => MissingRequiredValue|[type-use-binding].D.Member
TypeUseBindingFact-null-definition => MissingRequiredValue|[type-use-binding].D.Member.Artifact
TypeUseBindingFact-null-definition => MissingRequiredValue|[type-use-binding].D.Member.Name
TypeUseBindingFact-null-definition => MissingRequiredValue|[type-use-binding].D.Target
TypeUseBindingFact-null-definition => UnknownEnumValue|[type-use-binding].D.Member.Artifact.Kind
TypeUseBindingFact-null-definition => UnknownEnumValue|[type-use-binding].D.Target.Kind
TypeUseBindingFact-null-definition => InvalidSubject|[type-use-binding].D.Member.Artifact.Subject
TypeUseBindingFact-null-definition => InvalidSubject|[type-use-binding].D.Target.Subject
TypeUseBindingFact-null-definition => OwnershipMismatch|[type-use-binding].D.Member.Artifact.Subject
ArtifactMemberRoleFact-null-definition => MissingRequiredValue|[artifact-member-role].D
ArtifactMemberRoleFact-null-definition => MissingRequiredValue|[artifact-member-role].D.Member
ArtifactMemberRoleFact-null-definition => MissingRequiredValue|[artifact-member-role].D.Member.Artifact
ArtifactMemberRoleFact-null-definition => MissingRequiredValue|[artifact-member-role].D.Member.Name
ArtifactMemberRoleFact-null-definition => UnknownEnumValue|[artifact-member-role].D.Member.Artifact.Kind
ArtifactMemberRoleFact-null-definition => UnknownEnumValue|[artifact-member-role].D.Role
ArtifactMemberRoleFact-null-definition => InvalidSubject|[artifact-member-role].D.Member.Artifact.Subject
ArtifactMemberRoleFact-null-definition => OwnershipMismatch|[artifact-member-role].D.Member.Artifact.Subject
RelationshipFact-null-definition => MissingRequiredValue|[relationship].D
RelationshipFact-null-definition => UnknownEnumValue|[relationship].D.Key.Kind
RelationshipFact-null-definition => InvalidSubject|[relationship].D.Key.Source
RelationshipFact-null-definition => InvalidSubject|[relationship].D.Key.Target
RelationshipFact-null-definition => OwnershipMismatch|[relationship].D.Key.Source
ConceptRepresentationFact-null-definition => MissingRequiredValue|[concept-representation].D
ConceptRepresentationFact-null-definition => UnknownEnumValue|[concept-representation].D.Kind
ConceptRepresentationFact-null-definition => InvalidSubject|[concept-representation].D.Concept
ConceptRepresentationFact-null-definition => OwnershipMismatch|[concept-representation].D.Concept
ConceptAttributeFact-null-definition => MissingRequiredValue|[concept-attribute].D
ConceptAttributeFact-null-definition => MissingRequiredValue|[concept-attribute].D.Name
ConceptAttributeFact-null-definition => UnknownEnumValue|[concept-attribute].D.Kind
ConceptAttributeFact-null-definition => InvalidSubject|[concept-attribute].D.Concept
ConceptAttributeFact-null-definition => OwnershipMismatch|[concept-attribute].D.Concept
ConceptValidationRuleFact-null-definition => MissingRequiredValue|[concept-validation].D
ConceptValidationRuleFact-null-definition => MissingRequiredValue|[concept-validation].D.RuleIdentity
ConceptValidationRuleFact-null-definition => UnknownEnumValue|[concept-validation].D.Kind
ConceptValidationRuleFact-null-definition => InvalidSubject|[concept-validation].D.Concept
ConceptValidationRuleFact-null-definition => OwnershipMismatch|[concept-validation].D.Concept
SpecificationScenarioFact-null-definition => MissingRequiredValue|[scenario].D
SpecificationScenarioFact-null-definition => MissingRequiredValue|[scenario].D.Key
SpecificationScenarioFact-null-definition => MissingRequiredValue|[scenario].D.Name
SpecificationScenarioFact-null-definition => MissingRequiredValue|[scenario].D.TargetArtifact
SpecificationScenarioFact-null-definition => UnknownEnumValue|[scenario].D.TargetArtifact.Kind
SpecificationScenarioFact-null-definition => InvalidSubject|[scenario].D.Key.Scenario
SpecificationScenarioFact-null-definition => InvalidSubject|[scenario].D.TargetArtifact.Subject
SpecificationScenarioFact-null-definition => OwnershipMismatch|[scenario].D.Key.Scenario
SpecificationStepFact-null-definition => MissingRequiredValue|[step].D
SpecificationStepFact-null-definition => MissingRequiredValue|[step].D.Key
SpecificationStepFact-null-definition => MissingRequiredValue|[step].D.Key.Scenario
SpecificationStepFact-null-definition => UnknownEnumValue|[step].D.Kind
SpecificationStepFact-null-definition => UnknownEnumValue|[step].D.Phase
SpecificationStepFact-null-definition => InvalidSubject|[step].D.Key.Scenario.Scenario
SpecificationStepFact-null-definition => InvalidKindOperand|[step].D.Key.Index
SpecificationValueFact-null-definition => MissingRequiredValue|[value].D
SpecificationValueFact-null-definition => MissingRequiredValue|[value].D.Key
SpecificationValueFact-null-definition => MissingRequiredValue|[value].D.Key.Step
SpecificationValueFact-null-definition => MissingRequiredValue|[value].D.Key.Step.Scenario
SpecificationValueFact-null-definition => UnknownEnumValue|[value].D.Kind
SpecificationValueFact-null-definition => InvalidSubject|[value].D.Key.Step.Scenario.Scenario
SpecificationValueFact-null-definition => InvalidKindOperand|[value].D.Key.Step.Index
artifact-null-key-name-properties => MissingRequiredValue|[artifact].D.Key
artifact-null-key-name-properties => MissingRequiredValue|[artifact].D.Name
artifact-null-key-name-properties => NullRequiredCollection|[artifact].D.Properties
artifact-null-key-name-properties => UnknownEnumValue|[artifact].D.Key.Kind
artifact-null-key-name-properties => InvalidSubject|[artifact].D.Key.Subject
artifact-null-key-name-properties => OwnershipMismatch|[artifact].D.Key.Subject
artifact-null-property-and-type => MissingRequiredValue|[artifact].D.Properties[0]
artifact-null-property-and-type => MissingRequiredValue|[artifact].D.Properties[0].Name
artifact-null-property-and-type => MissingRequiredValue|[artifact].D.Properties[0].Type.Name
artifact-null-property-and-type => MissingRequiredValue|[artifact].D.Properties[1].Type
artifact-null-property-and-type => MissingRequiredValue|[artifact].D.Properties[1].Type.Name
artifact-null-property-and-type => InvalidSubject|[artifact].D.Properties[1].Type.Subject
artifact-null-key-subject => MissingRequiredValue|[artifact].D.Key.Subject
artifact-null-key-subject => InvalidSubject|[artifact].D.Key.Subject
artifact-null-key-subject => OwnershipMismatch|[artifact].D.Key.Subject
relationship-null-key => MissingRequiredValue|[relationship].D.Key
relationship-null-key => UnknownEnumValue|[relationship].D.Key.Kind
relationship-null-key => InvalidSubject|[relationship].D.Key.Source
relationship-null-key => InvalidSubject|[relationship].D.Key.Target
relationship-null-key => OwnershipMismatch|[relationship].D.Key.Source
relationship-null-key-parts => MissingRequiredValue|[relationship].D.Key.Source
relationship-null-key-parts => MissingRequiredValue|[relationship].D.Key.Target
relationship-null-key-parts => InvalidSubject|[relationship].D.Key.Source
relationship-null-key-parts => InvalidSubject|[relationship].D.Key.Target
relationship-null-key-parts => OwnershipMismatch|[relationship].D.Key.Source
placement-null-fields => MissingRequiredValue|[placement].Placement.Module
placement-null-fields => MissingRequiredValue|[placement].Placement.Slice
placement-null-fields => NullRequiredCollection|[placement].Placement.Features
placement-null-feature-item => MissingRequiredValue|[placement].Placement.Features[0]
concept-representation-null-parts => MissingRequiredValue|[concept-representation].D.Concept
concept-representation-null-parts => NullRequiredCollection|[concept-representation].D.EnumerationValues
concept-representation-null-parts => InvalidSubject|[concept-representation].D.Concept
concept-representation-null-parts => OwnershipMismatch|[concept-representation].D.Concept
concept-attribute-null-parts => MissingRequiredValue|[concept-attribute].D.Concept
concept-attribute-null-parts => MissingRequiredValue|[concept-attribute].D.Name
concept-attribute-null-parts => InvalidSubject|[concept-attribute].D.Concept
concept-attribute-null-parts => OwnershipMismatch|[concept-attribute].D.Concept
concept-validation-null-parts => MissingRequiredValue|[concept-validation].D.Concept
concept-validation-null-parts => MissingRequiredValue|[concept-validation].D.RuleIdentity
concept-validation-null-parts => InvalidSubject|[concept-validation].D.Concept
concept-validation-null-parts => OwnershipMismatch|[concept-validation].D.Concept
scenario-null-parts => MissingRequiredValue|[scenario].D.Key
scenario-null-parts => MissingRequiredValue|[scenario].D.Name
scenario-null-parts => MissingRequiredValue|[scenario].D.TargetArtifact
scenario-null-parts => NullRequiredCollection|[scenario].D.Steps
scenario-null-parts => UnknownEnumValue|[scenario].D.TargetArtifact.Kind
scenario-null-parts => InvalidSubject|[scenario].D.Key.Scenario
scenario-null-parts => InvalidSubject|[scenario].D.TargetArtifact.Subject
scenario-null-parts => OwnershipMismatch|[scenario].D.Key.Scenario
scenario-null-step-key => MissingRequiredValue|[scenario].D.Steps[0]
scenario-null-step-key => MissingRequiredValue|[scenario].D.Steps[0].Scenario
scenario-null-step-key => InvalidSubject|[scenario].D.Steps[0].Scenario.Scenario
scenario-null-step-key => OwnershipMismatch|[scenario].D.Steps[0].Index
scenario-null-step-key => OwnershipMismatch|[scenario].D.Steps[0].Scenario
scenario-null-step-key => OwnershipMismatch|[scenario].D.Steps[1].Index
scenario-null-step-key => InvalidKindOperand|[scenario].D.Steps[0].Index
scenario-null-step-key-scenario => MissingRequiredValue|[scenario].D.Steps[0].Scenario
scenario-null-step-key-scenario => InvalidSubject|[scenario].D.Steps[0].Scenario.Scenario
scenario-null-step-key-scenario => OwnershipMismatch|[scenario].D.Steps[0].Scenario
scenario-null-key-scenario => MissingRequiredValue|[scenario].D.Key.Scenario
scenario-null-key-scenario => InvalidSubject|[scenario].D.Key.Scenario
scenario-null-key-scenario => OwnershipMismatch|[scenario].D.Key.Scenario
scenario-null-key-scenario => OwnershipMismatch|[scenario].D.Steps[0].Scenario
step-null-parts => MissingRequiredValue|[step].D.Key
step-null-parts => MissingRequiredValue|[step].D.Key.Scenario
step-null-parts => NullRequiredCollection|[step].D.Values
step-null-parts => InvalidSubject|[step].D.Key.Scenario.Scenario
step-null-parts => InvalidKindOperand|[step].D.Artifact
step-null-parts => InvalidKindOperand|[step].D.Key.Index
step-null-value-keys => MissingRequiredValue|[step].D.Values[0]
step-null-value-keys => MissingRequiredValue|[step].D.Values[0].Step
step-null-value-keys => MissingRequiredValue|[step].D.Values[0].Step.Scenario
step-null-value-keys => MissingRequiredValue|[step].D.Values[1].Step
step-null-value-keys => MissingRequiredValue|[step].D.Values[1].Step.Scenario
step-null-value-keys => NullRequiredCollection|[step].D.Values[1].Path
step-null-value-keys => InvalidSubject|[step].D.Values[0].Step.Scenario.Scenario
step-null-value-keys => InvalidSubject|[step].D.Values[1].Step.Scenario.Scenario
step-null-value-keys => OwnershipMismatch|[step].D.Values
step-null-value-keys => OwnershipMismatch|[step].D.Values[0].Step
step-null-value-keys => OwnershipMismatch|[step].D.Values[1].Step
step-null-value-keys => InvalidKindOperand|[step].D.Values[0].Step.Index
step-null-value-keys => InvalidKindOperand|[step].D.Values[1].Step.Index
value-null-parts => MissingRequiredValue|[value].D.Key
value-null-parts => MissingRequiredValue|[value].D.Key.Step
value-null-parts => MissingRequiredValue|[value].D.Key.Step.Scenario
value-null-parts => NullRequiredCollection|[value].D.Children
value-null-parts => InvalidSubject|[value].D.Key.Step.Scenario.Scenario
value-null-parts => InvalidKindOperand|[value].D.Key.Step.Index
value-null-path-item => MissingRequiredValue|[value].D.Key.Path[0]
value-null-key-step-and-path => MissingRequiredValue|[value].D.Key.Step
value-null-key-step-and-path => MissingRequiredValue|[value].D.Key.Step.Scenario
value-null-key-step-and-path => NullRequiredCollection|[value].D.Key.Path
value-null-key-step-and-path => InvalidSubject|[value].D.Key.Step.Scenario.Scenario
value-null-key-step-and-path => InvalidKindOperand|[value].D.Key.Step.Index
fact-null-id => MissingRequiredValue|Contribution.Facts[ArtifactFact:dotnet://Accounts/artifact].Id
fact-null-id => InvalidFactId|Contribution.Facts[].Id
fact-null-id-value => InvalidFactId|Contribution.Facts[].Id
fact-null-subject => MissingRequiredValue|[artifact].Subject
fact-null-subject => InvalidSubject|[artifact].Subject
fact-null-subject => OwnershipMismatch|[artifact].D.Key.Subject
fact-null-evidence => MissingRequiredValue|[artifact].Evidence
fact-null-evidence => UnknownEnumValue|[artifact].Evidence.Strength
fact-null-evidence => EvidenceAdapterMismatch|[artifact].Evidence.Adapter
fact-null-evidence-adapter => MissingRequiredValue|[artifact].Evidence.Adapter
fact-null-evidence-adapter => EvidenceAdapterMismatch|[artifact].Evidence.Adapter
fact-null-source-fields => InvalidSourceRange|[artifact].Evidence.Source
fact-null-in-list => MissingRequiredValue|Contribution.Facts[null:unidentified-subject:]
facts-null => NullRequiredCollection|Contribution.Facts
diagnostics-null => NullRequiredCollection|Contribution.Diagnostics
contribution-null => MissingRequiredValue|Contribution
contribution-null => ContributionAdapterMismatch|Contribution.Adapter
descriptor-null => MissingRequiredValue|Descriptor
descriptor-null => UnknownEnumValue|Descriptor.Category
descriptor-null => UnknownEnumValue|Descriptor.SourceLanguage
descriptor-null => InvalidDescriptorIdentity|Descriptor.Identity.Id
descriptor-null => InvalidDescriptorIdentity|Descriptor.Identity.Version
descriptor-null => ContributionAdapterMismatch|Contribution.Adapter
contribution-adapter-null => MissingRequiredValue|Contribution.Adapter
contribution-adapter-null => ContributionAdapterMismatch|Contribution.Adapter
diagnostic-null-in-list => MissingRequiredValue|Contribution.Diagnostics[unidentified-code:]
diagnostic-null-fields => InvalidSubject|Contribution.Diagnostics[0].Subject
diagnostic-null-fields => InvalidContributionDiagnostic|Contribution.Diagnostics[0].Code
diagnostic-null-fields => InvalidContributionDiagnostic|Contribution.Diagnostics[0].Message
unsupported-fact => UnsupportedFactType|[unsupported]
""";

    static GenerationFact NullDefinition(GenerationFact fact) => fact switch
    {
        ArtifactFact artifact => artifact with { Definition = null! },
        ArtifactPlacementFact placement => placement with { Artifact = null!, Placement = null! },
        RelationshipFact relationship => relationship with { Definition = null! },
        ConceptRepresentationFact representation => representation with { Definition = null! },
        ConceptAttributeFact attribute => attribute with { Definition = null! },
        ConceptValidationRuleFact validation => validation with { Definition = null! },
        SpecificationScenarioFact scenario => scenario with { Definition = null! },
        SpecificationStepFact step => step with { Definition = null! },
        SpecificationValueFact value => value with { Definition = null! },
        ArtifactDeclarationFact declaration => declaration with { Definition = null! },
        ArtifactMemberDeclarationFact member => member with { Definition = null! },
        ArtifactMemberTypeUseFact typeUse => typeUse with { Definition = null! },
        TypeUseBindingFact binding => binding with { Definition = null! },
        ArtifactMemberRoleFact role => role with { Definition = null! },
        _ => fact
    };

    static void AddNestedNullCases(List<(string, AdapterContributionAdmissionResult)> cases)
    {
        var artifact = EveryFact().OfType<ArtifactFact>().Single();
        cases.Add(("artifact-null-key-name-properties", AdmitFacts(artifact with { Definition = artifact.Definition with { Key = null!, Name = null!, Properties = null! } })));
        cases.Add(("artifact-null-property-and-type", AdmitFacts(artifact with
        {
            Definition = artifact.Definition with
            {
                Properties = [null!, new PropertyDefinition { Name = null!, Type = null! }, new PropertyDefinition { Name = "x", Type = new TypeReferenceDefinition { Name = null!, Subject = new SubjectId { Value = null! } } }]
            }
        })));
        cases.Add(("artifact-null-key-subject", AdmitFacts(artifact with { Definition = artifact.Definition with { Key = new ArtifactKey { Subject = null!, Kind = ArtifactKind.Command } } })));

        var relationship = EveryFact().OfType<RelationshipFact>().Single();
        cases.Add(("relationship-null-key", AdmitFacts(relationship with { Definition = relationship.Definition with { Key = null! } })));
        cases.Add(("relationship-null-key-parts", AdmitFacts(relationship with { Definition = relationship.Definition with { Key = relationship.Definition.Key with { Source = null!, Target = null! } } })));

        var placement = EveryFact().OfType<ArtifactPlacementFact>().Single();
        cases.Add(("placement-null-fields", AdmitFacts(placement with { Placement = placement.Placement with { Module = null!, Features = null!, Slice = null! } })));
        cases.Add(("placement-null-feature-item", AdmitFacts(placement with { Placement = placement.Placement with { Features = [null!, "x"] } })));

        var representation = EveryFact().OfType<ConceptRepresentationFact>().Single();
        cases.Add(("concept-representation-null-parts", AdmitFacts(representation with { Definition = representation.Definition with { Concept = null!, EnumerationValues = null! } })));
        var attribute = EveryFact().OfType<ConceptAttributeFact>().Single();
        cases.Add(("concept-attribute-null-parts", AdmitFacts(attribute with { Definition = attribute.Definition with { Concept = null!, Name = null! } })));
        var validation = EveryFact().OfType<ConceptValidationRuleFact>().Single();
        cases.Add(("concept-validation-null-parts", AdmitFacts(validation with { Definition = validation.Definition with { Concept = null!, RuleIdentity = null! } })));

        var scenario = EveryFact().OfType<SpecificationScenarioFact>().Single();
        cases.Add(("scenario-null-parts", AdmitFacts(scenario with { Definition = scenario.Definition with { Key = null!, Name = null!, TargetArtifact = null!, Steps = null! } })));
        cases.Add(("scenario-null-step-key", AdmitFacts(scenario with { Definition = scenario.Definition with { Steps = [null!, StepKey()] } })));
        cases.Add(("scenario-null-step-key-scenario", AdmitFacts(scenario with { Definition = scenario.Definition with { Steps = [new SpecificationStepKey { Scenario = null!, Index = 0 }] } })));
        cases.Add(("scenario-null-key-scenario", AdmitFacts(scenario with { Definition = scenario.Definition with { Key = new SpecificationScenarioKey { Scenario = null! } } })));

        var step = EveryFact().OfType<SpecificationStepFact>().Single();
        cases.Add(("step-null-parts", AdmitFacts(step with { Definition = step.Definition with { Key = null!, Artifact = null, Values = null! } })));
        cases.Add(("step-null-value-keys", AdmitFacts(step with { Definition = step.Definition with { Values = [null!, ValueKey(["a"]) with { Step = null!, Path = null! }] } })));

        var value = EveryFact().OfType<SpecificationValueFact>().Single();
        cases.Add(("value-null-parts", AdmitFacts(value with { Definition = value.Definition with { Key = null!, Type = null, Children = null! } })));
        cases.Add(("value-null-path-item", AdmitFacts(value with { Definition = value.Definition with { Key = ValueKey([null!, "a"]) } })));
        cases.Add(("value-null-key-step-and-path", AdmitFacts(value with { Definition = value.Definition with { Key = new SpecificationValueKey { Step = null!, Path = null! } } })));
    }

    static void AddEnvelopeNullCases(List<(string, AdapterContributionAdmissionResult)> cases)
    {
        var artifact = EveryFact().OfType<ArtifactFact>().Single();
        cases.Add(("fact-null-id", AdmitFacts(artifact with { Id = null! })));
        cases.Add(("fact-null-id-value", AdmitFacts(artifact with { Id = new FactId { Value = null! } })));
        cases.Add(("fact-null-subject", AdmitFacts(artifact with { Subject = null! })));
        cases.Add(("fact-null-evidence", AdmitFacts(artifact with { Evidence = null! })));
        cases.Add(("fact-null-evidence-adapter", AdmitFacts(artifact with { Evidence = artifact.Evidence with { Adapter = null! } })));
        cases.Add(("fact-null-source-fields", AdmitFacts(artifact with { Evidence = artifact.Evidence with { Source = Source() with { Path = null!, FileIdentity = new SourceFileIdentity { Project = null!, Path = null! } } } })));
        cases.Add(("fact-null-in-list", Admit(contribution: Contribution([null!, artifact]))));
        cases.Add(("facts-null", Admit(contribution: new AdapterContribution { Adapter = Adapter, Facts = null!, Diagnostics = [] })));
        cases.Add(("diagnostics-null", Admit(contribution: new AdapterContribution { Adapter = Adapter, Facts = [], Diagnostics = null! })));
        cases.Add(("contribution-null", AdapterContributionAdmission.Admit(Descriptor(), null)));
        cases.Add(("descriptor-null", AdapterContributionAdmission.Admit(null, Contribution([]))));
        cases.Add(("contribution-adapter-null", Admit(contribution: new AdapterContribution { Adapter = null!, Facts = [], Diagnostics = [] })));
        cases.Add(("diagnostic-null-in-list", Admit(contribution: Contribution([], [null!]))));
        cases.Add(("diagnostic-null-fields", Admit(contribution: Contribution([], [new GenerationDiagnostic { Code = null!, Severity = GenerationDiagnosticSeverity.Error, Message = null!, Subject = new SubjectId { Value = null! } }]))));
        cases.Add(("unsupported-fact", Admit(contribution: Contribution([new UnsupportedFact { Id = Id("unsupported"), Subject = ArtifactSubject, Evidence = Evidence() }]))));
    }

    sealed record UnsupportedFact : GenerationFact;
}
