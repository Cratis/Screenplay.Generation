// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver.given;

public class specification_facts : facts
{
    protected static readonly SubjectId ScenarioSubject = new() { Value = "dotnet://Banking.Specs/RegisteringAccount" };
    protected static readonly SubjectId ReadModelSubject = new() { Value = "dotnet://Banking.ReadModels.AccountOverview" };
    protected static readonly SubjectId QuerySubject = new() { Value = "dotnet://Banking.Queries.AccountById" };

    protected static SpecificationScenarioKey ScenarioKey() => new() { Scenario = ScenarioSubject };

    protected static SpecificationStepKey StepKey(int index) => new()
    {
        Scenario = ScenarioKey(),
        Index = index
    };

    protected static SpecificationValueKey ValueKey(int step, params string[] path) => new()
    {
        Step = StepKey(step),
        Path = path
    };

    protected static SpecificationScenarioFact Scenario(params SpecificationStepKey[] steps) =>
        ScenarioFor(CommandKey(), steps);

    protected static SpecificationScenarioFact ScenarioFor(ArtifactKey target, params SpecificationStepKey[] steps) => new()
    {
        Id = new FactId { Value = "scenario" },
        Subject = ScenarioSubject,
        Definition = new SpecificationScenarioDefinition
        {
            Key = ScenarioKey(),
            Name = "RegisteringAccount",
            TargetArtifact = target,
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
        SpecificationValueKind kind = SpecificationValueKind.Text) =>
        ValueAt(step, [path], scalar, kind);

    protected static SpecificationValueFact ValueAt(
        int step,
        string[] path,
        string scalar,
        SpecificationValueKind kind = SpecificationValueKind.Text) => new()
        {
            Id = new FactId { Value = $"value-{step}-{string.Join('-', path)}" },
            Subject = new SubjectId { Value = $"{ScenarioSubject.Value}/step/{step}/{string.Join('/', path)}" },
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

    protected static ArtifactFact ReadModelArtifact() => new()
    {
        Id = new FactId { Value = "read-model" },
        Subject = ReadModelSubject,
        Definition = new ArtifactDefinition
        {
            Key = ReadModelKey(),
            Name = "AccountOverview",
            Properties =
            [
                new PropertyDefinition
                {
                    Name = "name",
                    Type = new TypeReferenceDefinition { Name = "String" }
                }
            ]
        },
        Evidence = Exact(6)
    };

    protected static ArtifactFact QueryArtifact() => new()
    {
        Id = new FactId { Value = "query" },
        Subject = QuerySubject,
        Definition = new ArtifactDefinition
        {
            Key = QueryKey(),
            Name = "AccountById",
            Properties =
            [
                new PropertyDefinition
                {
                    Name = "accountId",
                    Type = new TypeReferenceDefinition { Name = "String" },
                    IsIdentifier = true
                }
            ]
        },
        Evidence = Exact(7)
    };

    protected static ArtifactPlacementFact CommandPlacement() => Placement(
        "command-placement",
        CommandSubject,
        CommandKey());

    protected static ArtifactPlacementFact EventPlacement(string slice = "Register") => Placement(
        "event-placement",
        EventSubject,
        EventKey(),
        GenerationSliceKind.StateChange,
        slice);

    protected static ArtifactPlacementFact ReadModelPlacement() => Placement(
        "read-model-placement",
        ReadModelSubject,
        ReadModelKey(),
        GenerationSliceKind.StateView,
        "Overview");

    protected static ArtifactPlacementFact QueryPlacement() => Placement(
        "query-placement",
        QuerySubject,
        QueryKey(),
        GenerationSliceKind.StateView,
        "Overview");

    static ArtifactPlacementFact Placement(
        string id,
        SubjectId subject,
        ArtifactKey artifact,
        GenerationSliceKind sliceKind = GenerationSliceKind.StateChange,
        string slice = "Register") => new()
    {
        Id = new FactId { Value = id },
        Subject = subject,
        Artifact = artifact,
        Placement = new ArtifactPlacement
        {
            Module = "Accounts",
            Features = ["Registration"],
            Slice = slice,
            SliceKind = sliceKind
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

    protected static ArtifactKey ReadModelKey() => new()
    {
        Subject = ReadModelSubject,
        Kind = ArtifactKind.ReadModel
    };

    protected static ArtifactKey QueryKey() => new()
    {
        Subject = QuerySubject,
        Kind = ArtifactKind.Query
    };

    protected static RelationshipFact QueryReturnsReadModel() => new()
    {
        Id = new FactId { Value = "query-returns-read-model" },
        Subject = QuerySubject,
        Definition = new RelationshipDefinition
        {
            Key = new RelationshipKey
            {
                Kind = RelationshipKind.Returns,
                Source = QuerySubject,
                Target = ReadModelSubject
            },
            IsOptional = true
        },
        Evidence = Exact(8)
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
