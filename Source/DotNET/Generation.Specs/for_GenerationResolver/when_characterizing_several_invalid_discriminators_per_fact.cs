// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_characterizing_several_invalid_discriminators_per_fact : given.facts
{
    const int Undefined = 99;
    ResolvedApplicationGraph _result = null!;

    void Because() => _result = new GenerationResolver().Resolve(
    [
        Contribution(
            FirstAdapter,
            ArtifactWithUnknownStrengthKindAndTargetKind(),
            PlacementWithUndefinedArtifactKindAndUnknownSliceKind(),
            TypeUseWithUnknownArtifactKindAndUnknownShapeNode(),
            TypeUseWithRepeatedNamedShape(),
            TypeUseWithEmptyShape(),
            TypeUseWithUndefinedShapeNode(),
            BindingWithInvalidMemberAndTargetKinds(),
            RoleWithInvalidArtifactKindAndRole(),
            StepWithInvalidPhaseKindAndArtifactKind(),
            ValueWithUnknownKindAndUndefinedTargetKind(),
            ScenarioWithUndefinedTargetKind(),
            RepresentationWithUndefinedKindAndPrimitive())
    ]);

    [Fact] void should_reject_every_invalid_fact_and_report_nothing_else() => _result.Diagnostics.Count.ShouldEqual(Expected.Length);

    [Fact] void should_report_every_failure_with_its_literal_code_severity_outcome_subject_line_and_message() => Lines().ShouldEqual(Expected);

    static string[] Expected =>
    [
        "Warning|GEN0022|Unknown|dotnet://Invalid/artifact|1|Fact 'artifact' from adapter 'first' uses unknown ArtifactKind value '-1'; the affected fact was omitted",
        "Warning|GEN0022|Unsupported|dotnet://Invalid/artifact|1|Fact 'artifact' from adapter 'first' uses undefined ArtifactKind value '99'; the affected fact was omitted",
        "Warning|GEN0022|Unsupported|dotnet://Invalid/placement|2|Fact 'placement' from adapter 'first' uses undefined ArtifactKind value '99'; the affected fact was omitted",
        "Warning|GEN0022|Unknown|dotnet://Invalid/unknown-shape-node|3|Fact 'unknown-shape-node' from adapter 'first' uses unknown ArtifactKind value '-1'; the affected fact was omitted",
        "Warning|GEN0022|Unknown|dotnet://Invalid/binding|7|Fact 'binding' from adapter 'first' uses unknown ArtifactKind value '-1'; the affected fact was omitted",
        "Warning|GEN0022|Unsupported|dotnet://Invalid/binding|7|Fact 'binding' from adapter 'first' uses undefined ArtifactKind value '55'; the affected fact was omitted",
        "Warning|GEN0022|Unsupported|dotnet://Invalid/role|8|Fact 'role' from adapter 'first' uses undefined ArtifactKind value '66'; the affected fact was omitted",
        "Warning|GEN0022|Unsupported|dotnet://Invalid/step|9|Fact 'step' from adapter 'first' uses undefined ArtifactKind value '44'; the affected fact was omitted",
        "Warning|GEN0022|Unsupported|dotnet://Invalid/value|10|Fact 'value' from adapter 'first' uses undefined ArtifactKind value '33'; the affected fact was omitted",
        "Warning|GEN0022|Unsupported|dotnet://Invalid/scenario|11|Fact 'scenario' from adapter 'first' uses undefined ArtifactKind value '22'; the affected fact was omitted",
        "Warning|GEN0023|Unknown|dotnet://Invalid/placement|2|Fact 'placement' from adapter 'first' uses unknown GenerationSliceKind value '-1'; the affected fact was omitted",
        "Warning|GEN0025|Unsupported|dotnet://Invalid/representation|12|Fact 'representation' from adapter 'first' uses undefined ConceptRepresentationKind value '12'; the affected fact was omitted",
        "Warning|GEN0026|Unsupported|dotnet://Invalid/representation|12|Fact 'representation' from adapter 'first' uses undefined GenerationPrimitiveKind value '13'; the affected fact was omitted",
        "Warning|GEN0029|Unknown|dotnet://Invalid/artifact|1|Fact 'artifact' from adapter 'first' uses unknown EvidenceStrength value '-1'; the affected fact was omitted",
        "Warning|GEN0030|Unknown|dotnet://Invalid/step|9|Fact 'step' from adapter 'first' uses unknown SpecificationStepPhase value '-1'; the affected fact was omitted",
        "Warning|GEN0031|Unsupported|dotnet://Invalid/step|9|Fact 'step' from adapter 'first' uses undefined SpecificationStepKind value '99'; the affected fact was omitted",
        "Warning|GEN0032|Unknown|dotnet://Invalid/value|10|Fact 'value' from adapter 'first' uses unknown SpecificationValueKind value '-1'; the affected fact was omitted",
        "Warning|GEN0052|Unknown|dotnet://Invalid/unknown-shape-node|3|Fact 'unknown-shape-node' from adapter 'first' uses unknown TypeUseShapeKind value '-1'; the affected fact was omitted",
        "Warning|GEN0052|Unsupported|dotnet://Invalid/undefined-shape-node|6|Fact 'undefined-shape-node' from adapter 'first' uses undefined TypeUseShapeKind value '77'; the affected fact was omitted",
        "Warning|GEN0053|Unknown|dotnet://Invalid/role|8|Fact 'role' from adapter 'first' uses unknown ArtifactMemberRoleKind value '-1'; the affected fact was omitted",
        "Error|GEN0049|Unsupported|dotnet://Invalid/repeated-named|4|Fact 'repeated-named' uses malformed exact type-use shape 'Named -> Named'; the affected fact was omitted",
        "Error|GEN0049|Unknown|dotnet://Invalid/empty-shape|5|Fact 'empty-shape' uses malformed exact type-use shape '<empty>'; the affected fact was omitted"
    ];

    static ArtifactKey Artifact(string name, ArtifactKind kind = ArtifactKind.Command) =>
        new() { Subject = Subject(name), Kind = kind };

    static SubjectId Subject(string name) => new() { Value = $"dotnet://Invalid/{name}" };

    static ArtifactMemberKey Member(ArtifactKind kind) => new() { Artifact = Artifact("owner", kind), Name = "member" };

    static Evidence Evidence(int line, EvidenceStrength strength = EvidenceStrength.Exact) => new()
    {
        Adapter = FirstAdapter,
        Strength = strength,
        Source = new SourceRange
        {
            Path = "Invalid/Facts.cs",
            StartLine = line,
            StartColumn = 2,
            EndLine = line,
            EndColumn = 9
        }
    };

    static ArtifactFact ArtifactWithUnknownStrengthKindAndTargetKind() => new()
    {
        Id = new FactId { Value = "artifact" },
        Subject = Subject("artifact"),
        Evidence = Evidence(1, EvidenceStrength.Unknown),
        Definition = new ArtifactDefinition
        {
            Key = Artifact("artifact", ArtifactKind.Unknown),
            Name = "Artifact",
            Properties =
            [
                new PropertyDefinition
                {
                    Name = "target",
                    Type = new TypeReferenceDefinition { Name = "Target", TargetArtifactKind = (ArtifactKind)Undefined }
                }
            ]
        }
    };

    static ArtifactPlacementFact PlacementWithUndefinedArtifactKindAndUnknownSliceKind() => new()
    {
        Id = new FactId { Value = "placement" },
        Subject = Subject("placement"),
        Evidence = Evidence(2),
        Artifact = Artifact("placement", (ArtifactKind)Undefined),
        Placement = new ArtifactPlacement
        {
            Module = "Invalid",
            Features = ["Invalid"],
            Slice = "Invalid",
            SliceKind = GenerationSliceKind.Unknown
        }
    };

    static ArtifactMemberTypeUseFact TypeUseWithUnknownArtifactKindAndUnknownShapeNode() =>
        TypeUse("unknown-shape-node", 3, ArtifactKind.Unknown, TypeUseShapeKind.Unknown, TypeUseShapeKind.Named);

    static ArtifactMemberTypeUseFact TypeUseWithRepeatedNamedShape() =>
        TypeUse("repeated-named", 4, ArtifactKind.Command, TypeUseShapeKind.Named, TypeUseShapeKind.Named);

    static ArtifactMemberTypeUseFact TypeUseWithEmptyShape() =>
        TypeUse("empty-shape", 5, ArtifactKind.Command);

    static ArtifactMemberTypeUseFact TypeUseWithUndefinedShapeNode() =>
        TypeUse("undefined-shape-node", 6, ArtifactKind.Command, TypeUseShapeKind.Named, (TypeUseShapeKind)77);

    static ArtifactMemberTypeUseFact TypeUse(string id, int line, ArtifactKind kind, params TypeUseShapeKind[] shape) => new()
    {
        Id = new FactId { Value = id },
        Subject = Subject(id),
        Evidence = Evidence(line),
        Definition = new ArtifactMemberTypeUseDefinition
        {
            Member = Member(kind),
            Type = new TypeUseDefinition { Name = "Type", Shape = shape }
        }
    };

    static TypeUseBindingFact BindingWithInvalidMemberAndTargetKinds() => new()
    {
        Id = new FactId { Value = "binding" },
        Subject = Subject("binding"),
        Evidence = Evidence(7),
        Definition = new TypeUseBindingDefinition
        {
            Member = Member(ArtifactKind.Unknown),
            Target = Artifact("target", (ArtifactKind)55)
        }
    };

    static ArtifactMemberRoleFact RoleWithInvalidArtifactKindAndRole() => new()
    {
        Id = new FactId { Value = "role" },
        Subject = Subject("role"),
        Evidence = Evidence(8),
        Definition = new ArtifactMemberRoleDefinition
        {
            Member = Member((ArtifactKind)66),
            Role = ArtifactMemberRoleKind.Unknown
        }
    };

    static SpecificationStepFact StepWithInvalidPhaseKindAndArtifactKind() => new()
    {
        Id = new FactId { Value = "step" },
        Subject = Subject("step"),
        Evidence = Evidence(9),
        Definition = new SpecificationStepDefinition
        {
            Key = new SpecificationStepKey { Scenario = new SpecificationScenarioKey { Scenario = Subject("scenario") }, Index = 0 },
            Phase = SpecificationStepPhase.Unknown,
            Kind = (SpecificationStepKind)Undefined,
            Artifact = Artifact("step-artifact", (ArtifactKind)44),
            Values = []
        }
    };

    static SpecificationValueFact ValueWithUnknownKindAndUndefinedTargetKind() => new()
    {
        Id = new FactId { Value = "value" },
        Subject = Subject("value"),
        Evidence = Evidence(10),
        Definition = new SpecificationValueDefinition
        {
            Key = new SpecificationValueKey
            {
                Step = new SpecificationStepKey { Scenario = new SpecificationScenarioKey { Scenario = Subject("scenario") }, Index = 0 },
                Path = ["name"]
            },
            Kind = SpecificationValueKind.Unknown,
            Type = new TypeReferenceDefinition { Name = "Value", TargetArtifactKind = (ArtifactKind)33 },
            Scalar = "x"
        }
    };

    static SpecificationScenarioFact ScenarioWithUndefinedTargetKind() => new()
    {
        Id = new FactId { Value = "scenario" },
        Subject = Subject("scenario"),
        Evidence = Evidence(11),
        Definition = new SpecificationScenarioDefinition
        {
            Key = new SpecificationScenarioKey { Scenario = Subject("scenario") },
            Name = "Scenario",
            TargetArtifact = Artifact("scenario-target", (ArtifactKind)22),
            Steps = []
        }
    };

    static ConceptRepresentationFact RepresentationWithUndefinedKindAndPrimitive() => new()
    {
        Id = new FactId { Value = "representation" },
        Subject = Subject("representation"),
        Evidence = Evidence(12),
        Definition = new ConceptRepresentationDefinition
        {
            Concept = Subject("representation"),
            Kind = (ConceptRepresentationKind)12,
            Primitive = (GenerationPrimitiveKind)13
        }
    };

    string[] Lines() =>
    [
        .. _result.Diagnostics.Select(diagnostic =>
            $"{diagnostic.Severity}|{diagnostic.Code}|{diagnostic.Outcome}|{diagnostic.Subject?.Value}|{diagnostic.Source?.StartLine}|{diagnostic.Message}")
    ];
}
