// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver.given;

public class specification_facts : facts
{
    protected static readonly SubjectId ScenarioSubject = new() { Value = "dotnet://Banking.Specs/RegisteringAccount" };

    protected static SpecificationScenarioKey ScenarioKey() => new() { Scenario = ScenarioSubject };

    protected static SpecificationStepKey StepKey(int index) => new()
    {
        Scenario = ScenarioKey(),
        Index = index
    };

    protected static SpecificationValueKey ValueKey(int step, string path) => new()
    {
        Step = StepKey(step),
        Path = [path]
    };

    protected static SpecificationScenarioFact Scenario(params SpecificationStepKey[] steps) => new()
    {
        Id = new FactId { Value = "scenario" },
        Subject = ScenarioSubject,
        Definition = new SpecificationScenarioDefinition
        {
            Key = ScenarioKey(),
            Name = "RegisteringAccount",
            TargetArtifact = CommandKey(),
            Steps = steps
        },
        Evidence = Exact(10)
    };

    protected static SpecificationStepFact Step(
        int index,
        SpecificationStepPhase phase,
        SpecificationStepKind kind,
        ArtifactKey? artifact,
        IReadOnlyList<SpecificationValueKey>? values = null,
        string? errorMessage = null) => new()
        {
            Id = new FactId { Value = $"step-{index}" },
            Subject = new SubjectId { Value = $"{ScenarioSubject.Value}/step/{index}" },
            Definition = new SpecificationStepDefinition
            {
                Key = StepKey(index),
                Phase = phase,
                Kind = kind,
                Artifact = artifact,
                ErrorMessage = errorMessage,
                Values = values ?? []
            },
            Evidence = Exact(11 + index)
        };

    protected static SpecificationValueFact Value(
        int step,
        string path,
        string scalar,
        SpecificationValueKind kind = SpecificationValueKind.Text) => new()
        {
            Id = new FactId { Value = $"value-{step}-{path}" },
            Subject = new SubjectId { Value = $"{ScenarioSubject.Value}/step/{step}/{path}" },
            Definition = new SpecificationValueDefinition
            {
                Key = ValueKey(step, path),
                Kind = kind,
                Type = new TypeReferenceDefinition { Name = "String" },
                Scalar = scalar
            },
            Evidence = Exact(20 + step)
        };

    protected static ArtifactFact CommandArtifact() => new()
    {
        Id = new FactId { Value = "command" },
        Subject = CommandSubject,
        Definition = new ArtifactDefinition
        {
            Key = CommandKey(),
            Name = "RegisterAccount"
        },
        Evidence = Exact(3)
    };

    protected static ArtifactFact EventArtifact() => new()
    {
        Id = new FactId { Value = "event" },
        Subject = EventSubject,
        Definition = EventDefinition(),
        Evidence = Exact(4)
    };

    protected static ArtifactPlacementFact CommandPlacement() => new()
    {
        Id = new FactId { Value = "command-placement" },
        Subject = CommandSubject,
        Artifact = CommandKey(),
        Placement = new ArtifactPlacement
        {
            Module = "Accounts",
            Features = ["Registration"],
            Slice = "Register",
            SliceKind = GenerationSliceKind.StateChange
        },
        Evidence = Exact(5)
    };

    protected static ArtifactKey CommandKey() => new()
    {
        Subject = CommandSubject,
        Kind = ArtifactKind.Command
    };

    protected static ArtifactKey EventKey() => new()
    {
        Subject = EventSubject,
        Kind = ArtifactKind.Event
    };

    protected static Evidence Exact(int line) => new()
    {
        Adapter = FirstAdapter,
        Strength = EvidenceStrength.Exact,
        Source = new SourceRange
        {
            Path = "Accounts/RegisteringAccount.cs",
            StartLine = line,
            StartColumn = 1,
            EndLine = line,
            EndColumn = 20
        }
    };
}
