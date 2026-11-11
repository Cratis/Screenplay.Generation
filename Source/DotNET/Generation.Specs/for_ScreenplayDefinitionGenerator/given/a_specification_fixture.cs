// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator.given;

public class a_specification_fixture : a_lowering_fixture
{
    protected readonly ArtifactKey _command = new() { Subject = new SubjectId { Value = "dotnet://Banking/RegisterAccount" }, Kind = ArtifactKind.Command };
    protected readonly ArtifactKey _event = new() { Subject = new SubjectId { Value = "dotnet://Banking/AccountRegistered" }, Kind = ArtifactKind.Event };
    protected readonly ArtifactKey _readModel = new() { Subject = new SubjectId { Value = "dotnet://Banking/AccountView" }, Kind = ArtifactKind.ReadModel };
    protected readonly ArtifactKey _query = new() { Subject = new SubjectId { Value = "dotnet://Banking/GetAccount" }, Kind = ArtifactKind.Query };

    protected GenerationFact[] BankingArtifacts() =>
    [
        .. Artifact(ArtifactKind.Command, "RegisterAccount", "Register", GenerationSliceKind.StateChange, 1, properties: [Field("name", "String"), Field("count", "Int"), Field("flag", "Bool")]),
        .. Artifact(ArtifactKind.Event, "AccountRegistered", "Register", GenerationSliceKind.StateChange, 2, properties: [Field("name", "String")]),
        .. Artifact(ArtifactKind.ReadModel, "AccountView", "View", GenerationSliceKind.StateView, 3, properties: [Field("name", "String")]),
        .. Artifact(ArtifactKind.Query, "GetAccount", "View", GenerationSliceKind.StateView, 4),
        Relation(RelationshipKind.Returns, "GetAccount", "AccountView")
    ];

    static PropertyDefinition Field(string name, string type) => new() { Name = name, Type = new TypeReferenceDefinition { Name = type } };

    protected static IEnumerable<GenerationFact> Tweak<T>(IEnumerable<GenerationFact> facts, Func<T, T> change)
        where T : GenerationFact
    {
        var changed = false;
        foreach (var fact in facts)
        {
            if (!changed && fact is T target)
            {
                changed = true;
                yield return change(target);
            }
            else
            {
                yield return fact;
            }
        }
    }

    protected GenerationFact[] Scenario(string id, ArtifactKey target, StepSpec[] steps)
    {
        var subject = new SubjectId { Value = $"dotnet://Banking/Specs.{id}" };
        var scenarioKey = new SpecificationScenarioKey { Scenario = subject };
        var stepKeys = steps.Select((_, index) => new SpecificationStepKey { Scenario = scenarioKey, Index = index }).ToArray();
        var facts = new List<GenerationFact>
        {
            new SpecificationScenarioFact
            {
                Id = new FactId { Value = $"spec:{id}" },
                Subject = subject,
                Evidence = EvidenceAt($"Specs/{id}.cs", 7),
                Definition = new SpecificationScenarioDefinition
                {
                    Key = scenarioKey,
                    Name = $"Scenario{id}",
                    TargetArtifact = target,
                    Steps = stepKeys
                }
            }
        };
        for (var index = 0; index < steps.Length; index++)
        {
            facts.AddRange(StepFacts(id, subject, stepKeys, index, steps[index]));
        }

        return [.. facts];
    }

    List<GenerationFact> StepFacts(string id, SubjectId scenario, SpecificationStepKey[] stepKeys, int index, StepSpec step)
    {
        var key = stepKeys[index];
        var values = step.Values ?? [];
        var valueKeys = values.Select(_ => ValueKey(stepKeys, index, _)).ToArray();
        var facts = new List<GenerationFact>
        {
            new SpecificationStepFact
            {
                Id = new FactId { Value = $"spec:{id}:step:{index}" },
                Subject = new SubjectId { Value = $"{scenario.Value}/step/{index}" },
                Evidence = EvidenceAt($"Specs/{id}.cs", 8 + index),
                Definition = new SpecificationStepDefinition
                {
                    Key = key,
                    Phase = step.Phase,
                    Kind = step.Kind,
                    Artifact = step.Artifact,
                    ErrorCode = step.ErrorCode,
                    ErrorMessage = step.ErrorMessage,
                    Values = valueKeys
                }
            }
        };
        for (var position = 0; position < values.Length; position++)
        {
            facts.AddRange(ValueFacts(id, scenario, stepKeys, index, values[position], valueKeys[position]));
        }

        return facts;
    }

    static SpecificationValueKey ValueKey(SpecificationStepKey[] stepKeys, int index, ValueSpec value) => new()
    {
        Step = value.OtherStep is { } other ? stepKeys[other] : stepKeys[index],
        Path = value.PathDepth == 2 ? ["nested", "name"] : [value.Name]
    };

    List<GenerationFact> ValueFacts(string id, SubjectId scenario, SpecificationStepKey[] stepKeys, int index, ValueSpec value, SpecificationValueKey key)
    {
        var childSpecs = value.Children ?? [];
        var children = childSpecs
            .Select(_ => new SpecificationValueKey
            {
                Step = stepKeys[index],
                Path = _.SelfPath ? key.Path : [.. key.Path, _.Name]
            })
            .ToArray();
        var facts = new List<GenerationFact>
        {
            new SpecificationValueFact
            {
                Id = new FactId { Value = $"spec:{id}:value:{index}:{string.Join('.', key.Path)}" },
                Subject = new SubjectId { Value = $"{scenario.Value}/value/{index}/{string.Join('.', key.Path)}" },
                Evidence = EvidenceAt($"Specs/{id}.cs", 20 + index),
                Definition = new SpecificationValueDefinition
                {
                    Key = key,
                    Kind = value.Kind,
                    Scalar = value.Scalar,
                    Children = children
                }
            }
        };
        for (var position = 0; position < childSpecs.Length; position++)
        {
            if (!childSpecs[position].SelfPath)
            {
                facts.AddRange(ValueFacts(id, scenario, stepKeys, index, childSpecs[position], children[position]));
            }
        }

        return facts;
    }

    protected sealed record StepSpec(
        SpecificationStepPhase Phase,
        SpecificationStepKind Kind,
        ArtifactKey? Artifact,
        string? ErrorCode = null,
        string? ErrorMessage = null,
        ValueSpec[]? Values = null);

    protected sealed record ValueSpec(
        string Name,
        SpecificationValueKind Kind,
        string? Scalar = null,
        ValueSpec[]? Children = null,
        int PathDepth = 1,
        bool SelfPath = false,
        int? OtherStep = null);
}
