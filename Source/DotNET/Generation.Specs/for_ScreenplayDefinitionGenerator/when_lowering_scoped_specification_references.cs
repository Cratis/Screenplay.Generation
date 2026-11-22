// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_lowering_scoped_specification_references : given.a_specification_fixture
{
    GeneratedScreenplayDefinition _result;

    void Because()
    {
        var scope = new BackendScopeKey { Store = new SubjectId { Value = "dotnet://Banking/Stores.Accounts" } };
        var command = _command with { Scope = scope };
        GenerationFact[] facts =
        [
            new BackendScopeFact { Id = new FactId { Value = "scope:accounts" }, Subject = scope.Store, Evidence = EvidenceAt("Stores/Accounts.cs", 3), Definition = new BackendScopeDefinition { Key = scope } },
            .. BankingArtifacts().Select(fact => fact switch
            {
                ArtifactFact artifact when artifact.Definition.Key == _command => artifact with { Definition = artifact.Definition with { Key = command } },
                ArtifactPlacementFact placement when placement.Artifact == _command => placement with { Artifact = command },
                _ => fact
            }),
            .. Scenario(
                "Scoped",
                command,
                [
                    new(SpecificationStepPhase.When, SpecificationStepKind.Command, command),
                    new(SpecificationStepPhase.Then, SpecificationStepKind.Event, _event)
                ])
        ];
        _result = Generator.Generate(Snapshot(Completed(Adapter, facts)), new ScreenplayGenerationOptions { Domain = "Banking" });
    }

    [Fact] void should_resolve_the_exact_scoped_command_in_the_scenario_and_step() => _result.Source.ShouldContain("specification ScenarioScoped");
    [Fact] void should_verify_the_structural_specification() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_mark_the_scoped_scenario_as_partial() => _result.AdapterRun.Facts.Single(record => record.Fact is SpecificationScenarioFact).Disposition.ShouldEqual(GenerationFactDisposition.PartiallyLowered);
    [Fact] void should_mark_the_scoped_command_step_as_partial() => _result.AdapterRun.Facts.Single(record => record.Fact is SpecificationStepFact step && step.Definition.Kind == SpecificationStepKind.Command).Disposition.ShouldEqual(GenerationFactDisposition.PartiallyLowered);
    [Fact] void should_keep_the_unscoped_event_step_fully_lowered() => _result.AdapterRun.Facts.Single(record => record.Fact is SpecificationStepFact step && step.Definition.Kind == SpecificationStepKind.Event).Disposition.ShouldEqual(GenerationFactDisposition.Lowered);
}
