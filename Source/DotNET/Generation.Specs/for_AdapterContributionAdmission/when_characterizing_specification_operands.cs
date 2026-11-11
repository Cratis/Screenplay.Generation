// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_characterizing_specification_operands : given.a_characterized_admission
{
    string[] _observed = null!;

    void Because()
    {
        var cases = new List<(string, AdapterContributionAdmissionResult)>();
        AddScenarioCases(cases);
        AddStepCases(cases);
        AddValueCases(cases);
        _observed = RenderCompact(cases);
    }

    [Fact] void should_report_the_operand_diagnostics_in_order() => string.Join('\n', _observed).ShouldEqual(Expected);

    const string Expected = """
scenario-valid => admitted
scenario-owner-mismatch => OwnershipMismatch|[scenario].D.Key.Scenario
scenario-owner-mismatch => OwnershipMismatch|[scenario].D.Steps[0].Scenario
scenario-blank-name => MissingRequiredValue|[scenario].D.Name
scenario-invalid-target => UndefinedEnumValue|[scenario].D.TargetArtifact.Kind
scenario-invalid-target => InvalidSubject|[scenario].D.TargetArtifact.Subject
scenario-step-other-scenario => OwnershipMismatch|[scenario].D.Steps[0].Scenario
scenario-step-wrong-index => OwnershipMismatch|[scenario].D.Steps[0].Index
scenario-step-negative-index => OwnershipMismatch|[scenario].D.Steps[0].Index
scenario-step-negative-index => InvalidKindOperand|[scenario].D.Steps[0].Index
scenario-step-all-wrong => OwnershipMismatch|[scenario].D.Steps[1].Index
scenario-step-all-wrong => OwnershipMismatch|[scenario].D.Steps[1].Scenario
scenario-step-all-wrong => InvalidKindOperand|[scenario].D.Steps[1].Index
step-valid => admitted
step-error-valid => admitted
step-error-everything-wrong => InvalidKindOperand|[step].D.Artifact
step-error-everything-wrong => InvalidKindOperand|[step].D.Phase
step-error-everything-wrong => InvalidKindOperand|[step].D.Values
step-unknown-kind => UnknownEnumValue|[step].D.Kind
step-undefined-kind => UndefinedEnumValue|[step].D.Kind
step-unknown-phase => UnknownEnumValue|[step].D.Phase
step-missing-artifact => InvalidKindOperand|[step].D.Artifact
step-error-operands => InvalidKindOperand|[step].D
step-error-message-only => InvalidKindOperand|[step].D
step-event-ok => admitted
step-event-wrong => InvalidKindOperand|[step].D.Artifact.Kind
step-readmodel-ok => admitted
step-readmodel-wrong => InvalidKindOperand|[step].D.Artifact.Kind
step-command-wrong => InvalidKindOperand|[step].D.Artifact.Kind
step-read-ok => admitted
step-read-wrong => InvalidKindOperand|[step].D.Artifact.Kind
step-invalid-artifact-and-wrong-kind => UnknownEnumValue|[step].D.Artifact.Kind
step-invalid-artifact-and-wrong-kind => InvalidSubject|[step].D.Artifact.Subject
step-invalid-artifact-and-wrong-kind => InvalidKindOperand|[step].D.Artifact.Kind
step-error-code-and-wrong-kind => InvalidKindOperand|[step].D
step-error-code-and-wrong-kind => InvalidKindOperand|[step].D.Artifact.Kind
step-value-other-step => OwnershipMismatch|[step].D.Values[0].Step
step-duplicate-values => OwnershipMismatch|[step].D.Values
step-duplicate-values => OwnershipMismatch|[step].D.Values
step-blank-value-path => MissingRequiredValue|[step].D.Values[0].Path[1]
step-negative-index => OwnershipMismatch|[step].D.Values[0].Step
step-negative-index => InvalidKindOperand|[step].D.Key.Index
step-invalid-scenario-subject => InvalidSubject|[step].D.Key.Scenario.Scenario
step-invalid-scenario-subject => OwnershipMismatch|[step].D.Values[0].Step
value-valid => admitted
value-unknown-kind => UnknownEnumValue|[value].D.Kind
value-null-ok => admitted
value-null-with-scalar => InvalidKindOperand|[value].D
value-null-with-children => InvalidKindOperand|[value].D
value-text-missing-scalar => InvalidKindOperand|[value].D.Scalar
value-text-children => InvalidKindOperand|[value].D.Children
value-number-ok => admitted
value-number-invalid => InvalidKindOperand|[value].D.Scalar
value-number-null => InvalidKindOperand|[value].D.Scalar
value-number-children => InvalidKindOperand|[value].D.Children
value-boolean-ok => admitted
value-boolean-uppercase => InvalidKindOperand|[value].D.Scalar
value-boolean-children => InvalidKindOperand|[value].D.Children
value-collection-ok => admitted
value-collection-scalar => InvalidKindOperand|[value].D.Scalar
value-composite-scalar => InvalidKindOperand|[value].D.Scalar
value-undefined-kind => UndefinedEnumValue|[value].D.Kind
value-child-not-direct => OwnershipMismatch|[value].D.Children[0]
value-child-other-step => OwnershipMismatch|[value].D.Children[0]
value-duplicate-children => OwnershipMismatch|[value].D.Children
value-blank-path => MissingRequiredValue|[value].D.Key.Path[1]
value-invalid-type => MissingRequiredValue|[value].D.Type.Name
value-invalid-type => InvalidSubject|[value].D.Type.Subject
value-type-kind-without-subject => InvalidKindOperand|[value].D.Type.TargetArtifactKind
""";

    static SpecificationStepFact Step(Func<SpecificationStepDefinition, SpecificationStepDefinition> change)
    {
        var fact = EveryFact().OfType<SpecificationStepFact>().Single();
        return fact with { Definition = change(fact.Definition) };
    }

    static SpecificationValueFact Value(Func<SpecificationValueDefinition, SpecificationValueDefinition> change)
    {
        var fact = EveryFact().OfType<SpecificationValueFact>().Single();
        return fact with { Definition = change(fact.Definition) };
    }

    static SpecificationScenarioFact Scenario(Func<SpecificationScenarioDefinition, SpecificationScenarioDefinition> change)
    {
        var fact = EveryFact().OfType<SpecificationScenarioFact>().Single();
        return fact with { Definition = change(fact.Definition) };
    }

    static SpecificationStepKey OtherStep(int index) => new() { Scenario = new SpecificationScenarioKey { Scenario = Subject("other") }, Index = index };

    static SpecificationValueKey ChildKey(params string[] path) => ValueKey(path);

    static SpecificationStepDefinition WithKind(SpecificationStepDefinition step, SpecificationStepKind kind, ArtifactKind artifact) =>
        step with { Kind = kind, Artifact = ArtifactKey(ExternalSubject, artifact) };

    static void AddScenarioCases(List<(string, AdapterContributionAdmissionResult)> cases)
    {
        cases.Add(("scenario-valid", AdmitFacts(Scenario(definition => definition))));
        cases.Add(("scenario-owner-mismatch", AdmitFacts(Scenario(definition => definition with { Key = new SpecificationScenarioKey { Scenario = Subject("other") } }))));
        cases.Add(("scenario-blank-name", AdmitFacts(Scenario(definition => definition with { Name = " " }))));
        cases.Add(("scenario-invalid-target", AdmitFacts(Scenario(definition => definition with { TargetArtifact = ArtifactKey(new SubjectId { Value = "no scheme" }, (ArtifactKind)99) }))));
        cases.Add(("scenario-step-other-scenario", AdmitFacts(Scenario(definition => definition with { Steps = [OtherStep(0)] }))));
        cases.Add(("scenario-step-wrong-index", AdmitFacts(Scenario(definition => definition with { Steps = [StepKey() with { Index = 3 }] }))));
        cases.Add(("scenario-step-negative-index", AdmitFacts(Scenario(definition => definition with { Steps = [StepKey() with { Index = -1 }] }))));
        cases.Add(("scenario-step-all-wrong", AdmitFacts(Scenario(definition => definition with { Steps = [StepKey(), OtherStep(-2)] }))));
    }

    static void AddStepCases(List<(string, AdapterContributionAdmissionResult)> cases)
    {
        cases.Add(("step-valid", AdmitFacts(Step(definition => definition))));
        cases.Add(("step-error-valid", AdmitFacts(Step(definition => definition with { Kind = SpecificationStepKind.Error, Phase = SpecificationStepPhase.Then, Artifact = null, Values = [], ErrorCode = "X" }))));
        cases.Add(("step-error-everything-wrong", AdmitFacts(Step(definition => definition with { Kind = SpecificationStepKind.Error, Phase = SpecificationStepPhase.When }))));
        cases.Add(("step-unknown-kind", AdmitFacts(Step(definition => definition with { Kind = SpecificationStepKind.Unknown }))));
        cases.Add(("step-undefined-kind", AdmitFacts(Step(definition => definition with { Kind = (SpecificationStepKind)99 }))));
        cases.Add(("step-unknown-phase", AdmitFacts(Step(definition => definition with { Phase = SpecificationStepPhase.Unknown }))));
        cases.Add(("step-missing-artifact", AdmitFacts(Step(definition => definition with { Artifact = null }))));
        cases.Add(("step-error-operands", AdmitFacts(Step(definition => definition with { ErrorCode = "E", ErrorMessage = "m" }))));
        cases.Add(("step-error-message-only", AdmitFacts(Step(definition => definition with { ErrorMessage = "m" }))));
        cases.Add(("step-event-ok", AdmitFacts(Step(definition => WithKind(definition, SpecificationStepKind.Event, ArtifactKind.Event)))));
        cases.Add(("step-event-wrong", AdmitFacts(Step(definition => WithKind(definition, SpecificationStepKind.Event, ArtifactKind.Command)))));
        cases.Add(("step-readmodel-ok", AdmitFacts(Step(definition => WithKind(definition, SpecificationStepKind.ReadModel, ArtifactKind.ReadModel)))));
        cases.Add(("step-readmodel-wrong", AdmitFacts(Step(definition => WithKind(definition, SpecificationStepKind.ReadModel, ArtifactKind.Event)))));
        cases.Add(("step-command-wrong", AdmitFacts(Step(definition => WithKind(definition, SpecificationStepKind.Command, ArtifactKind.Query)))));
        cases.Add(("step-read-ok", AdmitFacts(Step(definition => WithKind(definition, SpecificationStepKind.Read, ArtifactKind.Query)))));
        cases.Add(("step-read-wrong", AdmitFacts(Step(definition => WithKind(definition, SpecificationStepKind.Read, ArtifactKind.Command)))));
        cases.Add(("step-invalid-artifact-and-wrong-kind", AdmitFacts(Step(definition => definition with { Kind = SpecificationStepKind.Event, Artifact = ArtifactKey(new SubjectId { Value = "bad" }, ArtifactKind.Unknown) }))));
        cases.Add(("step-error-code-and-wrong-kind", AdmitFacts(Step(definition => WithKind(definition, SpecificationStepKind.Event, ArtifactKind.Command) with { ErrorCode = "E" }))));
        cases.Add(("step-value-other-step", AdmitFacts(Step(definition => definition with { Values = [ValueKey(["a"]) with { Step = OtherStep(0) }] }))));
        cases.Add(("step-duplicate-values", AdmitFacts(Step(definition => definition with { Values = [ChildKey("a"), ChildKey("a"), ChildKey("b"), ChildKey("b")] }))));
        cases.Add(("step-blank-value-path", AdmitFacts(Step(definition => definition with { Values = [ChildKey("a", " ")] }))));
        cases.Add(("step-negative-index", AdmitFacts(Step(definition => definition with { Key = StepKey() with { Index = -5 } }))));
        cases.Add(("step-invalid-scenario-subject", AdmitFacts(Step(definition => definition with { Key = StepKey() with { Scenario = new SpecificationScenarioKey { Scenario = new SubjectId { Value = "bad" } } } }))));
    }

    static void AddValueCases(List<(string, AdapterContributionAdmissionResult)> cases)
    {
        var child = ChildKey("arguments", "name", "first");
        cases.Add(("value-valid", AdmitFacts(Value(definition => definition))));
        cases.Add(("value-unknown-kind", AdmitFacts(Value(definition => definition with { Kind = SpecificationValueKind.Unknown }))));
        cases.Add(("value-null-ok", AdmitFacts(Value(definition => definition with { Kind = SpecificationValueKind.Null, Scalar = null }))));
        cases.Add(("value-null-with-scalar", AdmitFacts(Value(definition => definition with { Kind = SpecificationValueKind.Null }))));
        cases.Add(("value-null-with-children", AdmitFacts(Value(definition => definition with { Kind = SpecificationValueKind.Null, Scalar = null, Children = [child] }))));
        cases.Add(("value-text-missing-scalar", AdmitFacts(Value(definition => definition with { Scalar = null }))));
        cases.Add(("value-text-children", AdmitFacts(Value(definition => definition with { Children = [child] }))));
        cases.Add(("value-number-ok", AdmitFacts(Value(definition => definition with { Kind = SpecificationValueKind.Number, Scalar = "-12.50" }))));
        cases.Add(("value-number-invalid", AdmitFacts(Value(definition => definition with { Kind = SpecificationValueKind.Number, Scalar = "abc" }))));
        cases.Add(("value-number-null", AdmitFacts(Value(definition => definition with { Kind = SpecificationValueKind.Number, Scalar = null }))));
        cases.Add(("value-number-children", AdmitFacts(Value(definition => definition with { Kind = SpecificationValueKind.Number, Scalar = "1", Children = [child] }))));
        cases.Add(("value-boolean-ok", AdmitFacts(Value(definition => definition with { Kind = SpecificationValueKind.Boolean, Scalar = "false" }))));
        cases.Add(("value-boolean-uppercase", AdmitFacts(Value(definition => definition with { Kind = SpecificationValueKind.Boolean, Scalar = "True" }))));
        cases.Add(("value-boolean-children", AdmitFacts(Value(definition => definition with { Kind = SpecificationValueKind.Boolean, Scalar = "true", Children = [child] }))));
        cases.Add(("value-collection-ok", AdmitFacts(Value(definition => definition with { Kind = SpecificationValueKind.Collection, Scalar = null, Children = [child] }))));
        cases.Add(("value-collection-scalar", AdmitFacts(Value(definition => definition with { Kind = SpecificationValueKind.Collection }))));
        cases.Add(("value-composite-scalar", AdmitFacts(Value(definition => definition with { Kind = SpecificationValueKind.Composite }))));
        cases.Add(("value-undefined-kind", AdmitFacts(Value(definition => definition with { Kind = (SpecificationValueKind)99 }))));
        cases.Add(("value-child-not-direct", AdmitFacts(Value(definition => definition with { Kind = SpecificationValueKind.Composite, Scalar = null, Children = [ChildKey("arguments", "name", "a", "b")] }))));
        cases.Add(("value-child-other-step", AdmitFacts(Value(definition => definition with { Kind = SpecificationValueKind.Composite, Scalar = null, Children = [child with { Step = OtherStep(0) }] }))));
        cases.Add(("value-duplicate-children", AdmitFacts(Value(definition => definition with { Kind = SpecificationValueKind.Composite, Scalar = null, Children = [child, child] }))));
        cases.Add(("value-blank-path", AdmitFacts(Value(definition => definition with { Key = ValueKey(["arguments", ""]) }))));
        cases.Add(("value-invalid-type", AdmitFacts(Value(definition => definition with { Type = new TypeReferenceDefinition { Name = " ", Subject = new SubjectId { Value = "bad" } } }))));
        cases.Add(("value-type-kind-without-subject", AdmitFacts(Value(definition => definition with { Type = new TypeReferenceDefinition { Name = "T", TargetArtifactKind = ArtifactKind.Concept } }))));
    }
}
