// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_calculating_specification_fact_dispositions : given.a_generator
{
    readonly SubjectId _commandSubject = new() { Value = "dotnet://Banking/Commands.RegisterAccount" };
    readonly SubjectId _eventSubject = new() { Value = "dotnet://Banking/Events.AccountRegistered" };
    readonly SubjectId _scenarioSubject = new() { Value = "dotnet://Banking/Specs.RegisteringAccount" };
    GeneratedScreenplayDefinition _accepted = null!;
    GeneratedScreenplayDefinition _conflicted = null!;
    GeneratedScreenplayDefinition _rejected = null!;

    void Because()
    {
        var common = CommonFacts();
        var specification = SpecificationFacts();
        var options = new ScreenplayGenerationOptions { Domain = "Banking" };
        _accepted = Generator.Generate(
            Snapshot(Completed(Adapter, [.. common, .. specification])),
            options);
        _rejected = Generator.Generate(
            Snapshot(Completed(Adapter, [.. common, .. specification.Where(fact => fact.Id.Value != "spec:value:then")])),
            options);
        var scenario = (SpecificationScenarioFact)specification.Single(fact => fact.Id.Value == "spec:scenario");
        var value = (SpecificationValueFact)specification.Single(fact => fact.Id.Value == "spec:value:then");
        _conflicted = Generator.Generate(
            Snapshot(Completed(
                Adapter,
                [
                    .. common,
                    .. specification,
                    scenario with
                    {
                        Id = new FactId { Value = "spec:scenario:conflict" },
                        Definition = scenario.Definition with { Name = "Registering another account" }
                    },
                    value with
                    {
                        Id = new FactId { Value = "spec:value:then:conflict" },
                        Definition = value.Definition with { Scalar = "Other" }
                    }
                ])),
            options);
    }

    [Fact] void should_lower_the_accepted_scenario() => Disposition(_accepted, "spec:scenario").ShouldEqual(GenerationFactDisposition.Lowered);
    [Fact] void should_lower_every_accepted_step() => Dispositions(_accepted, "spec:step:when", "spec:step:then").ShouldContainOnly(GenerationFactDisposition.Lowered, GenerationFactDisposition.Lowered);
    [Fact] void should_lower_every_accepted_value() => Dispositions(_accepted, "spec:value:when", "spec:value:then").ShouldContainOnly(GenerationFactDisposition.Lowered, GenerationFactDisposition.Lowered);
    [Fact] void should_omit_the_rejected_scenario() => Disposition(_rejected, "spec:scenario").ShouldEqual(GenerationFactDisposition.OmittedWithDiagnostic);
    [Fact] void should_omit_every_step_of_the_rejected_scenario() => Dispositions(_rejected, "spec:step:when", "spec:step:then").ShouldContainOnly(GenerationFactDisposition.OmittedWithDiagnostic, GenerationFactDisposition.OmittedWithDiagnostic);
    [Fact] void should_omit_the_unconsumed_value_of_the_rejected_scenario() => Disposition(_rejected, "spec:value:when").ShouldEqual(GenerationFactDisposition.OmittedWithDiagnostic);
    [Fact] void should_associate_the_incomplete_scenario_diagnostic_with_rejected_steps_and_values() => DiagnosticCodes(_rejected, "spec:scenario", "spec:step:when", "spec:step:then", "spec:value:when").All(code => code == GenerationDiagnosticCodes.IncompleteSpecificationScenario).ShouldBeTrue();
    [Fact] void should_classify_both_scenario_conflict_variants_independently() => Dispositions(_conflicted, "spec:scenario", "spec:scenario:conflict").ShouldContainOnly(GenerationFactDisposition.Conflicted, GenerationFactDisposition.Conflicted);
    [Fact] void should_classify_both_value_conflict_variants_independently() => Dispositions(_conflicted, "spec:value:then", "spec:value:then:conflict").ShouldContainOnly(GenerationFactDisposition.Conflicted, GenerationFactDisposition.Conflicted);
    [Fact] void should_not_leave_accepted_rejected_or_conflicted_specification_facts_unknown() => _accepted.AdapterRun!.Facts.Concat(_rejected.AdapterRun!.Facts).Concat(_conflicted.AdapterRun!.Facts).Any(record => record.Disposition == GenerationFactDisposition.Unknown).ShouldBeFalse();

    GenerationFact[] CommonFacts()
    {
        var commandKey = new ArtifactKey { Subject = _commandSubject, Kind = ArtifactKind.Command };
        var eventKey = new ArtifactKey { Subject = _eventSubject, Kind = ArtifactKind.Event };
        return
        [
            Artifact("artifact:command", commandKey, "RegisterAccount"),
            Placement("placement:command", commandKey),
            Artifact("artifact:event", eventKey, "AccountRegistered"),
            Placement("placement:event", eventKey)
        ];
    }

    GenerationFact[] SpecificationFacts()
    {
        var scenario = new SpecificationScenarioKey { Scenario = _scenarioSubject };
        var whenKey = new SpecificationStepKey { Scenario = scenario, Index = 0 };
        var thenKey = new SpecificationStepKey { Scenario = scenario, Index = 1 };
        var whenValue = new SpecificationValueKey { Step = whenKey, Path = ["name"] };
        var thenValue = new SpecificationValueKey { Step = thenKey, Path = ["name"] };
        return
        [
            new SpecificationScenarioFact
            {
                Id = new FactId { Value = "spec:scenario" },
                Subject = _scenarioSubject,
                Evidence = Exact(),
                Definition = new SpecificationScenarioDefinition
                {
                    Key = scenario,
                    Name = "Registering account",
                    TargetArtifact = new ArtifactKey { Subject = _commandSubject, Kind = ArtifactKind.Command },
                    Steps = [whenKey, thenKey]
                }
            },
            new SpecificationStepFact
            {
                Id = new FactId { Value = "spec:step:when" },
                Subject = new SubjectId { Value = $"{_scenarioSubject.Value}/step/0" },
                Evidence = Exact(),
                Definition = new SpecificationStepDefinition
                {
                    Key = whenKey,
                    Phase = SpecificationStepPhase.When,
                    Kind = SpecificationStepKind.Command,
                    Artifact = new ArtifactKey { Subject = _commandSubject, Kind = ArtifactKind.Command },
                    Values = [whenValue]
                }
            },
            new SpecificationStepFact
            {
                Id = new FactId { Value = "spec:step:then" },
                Subject = new SubjectId { Value = $"{_scenarioSubject.Value}/step/1" },
                Evidence = Exact(),
                Definition = new SpecificationStepDefinition
                {
                    Key = thenKey,
                    Phase = SpecificationStepPhase.Then,
                    Kind = SpecificationStepKind.Event,
                    Artifact = new ArtifactKey { Subject = _eventSubject, Kind = ArtifactKind.Event },
                    Values = [thenValue]
                }
            },
            Value("spec:value:when", whenValue),
            Value("spec:value:then", thenValue)
        ];
    }

    ArtifactFact Artifact(string id, ArtifactKey key, string name) => new()
    {
        Id = new FactId { Value = id },
        Subject = key.Subject,
        Evidence = Exact(),
        Definition = new ArtifactDefinition { Key = key, Name = name }
    };

    ArtifactPlacementFact Placement(string id, ArtifactKey key) => new()
    {
        Id = new FactId { Value = id },
        Subject = key.Subject,
        Evidence = Exact(),
        Artifact = key,
        Placement = new ArtifactPlacement
        {
            Module = "Accounts",
            Features = ["Registration"],
            Slice = "Register",
            SliceKind = GenerationSliceKind.StateChange
        }
    };

    SpecificationValueFact Value(string id, SpecificationValueKey key) => new()
    {
        Id = new FactId { Value = id },
        Subject = new SubjectId { Value = $"{_scenarioSubject.Value}/value/{id}" },
        Evidence = Exact(),
        Definition = new SpecificationValueDefinition
        {
            Key = key,
            Kind = SpecificationValueKind.Text,
            Scalar = "Cratis"
        }
    };

    Evidence Exact() => new() { Adapter = Adapter, Strength = EvidenceStrength.Exact };

    static GenerationFactDisposition Disposition(GeneratedScreenplayDefinition result, string id) =>
        result.AdapterRun!.Facts.Single(record => record.Fact.Id.Value == id).Disposition;

    static GenerationFactDisposition[] Dispositions(GeneratedScreenplayDefinition result, params string[] ids) =>
        [.. ids.Select(id => Disposition(result, id))];

    static string[] DiagnosticCodes(GeneratedScreenplayDefinition result, params string[] ids) =>
        [.. ids.Select(id => result.AdapterRun!.Facts.Single(record => record.Fact.Id.Value == id).Diagnostics[0].Code)];
}
