// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission.given;

public class a_contribution : Specification
{
    protected static readonly AdapterIdentity Adapter = new() { Id = "atomic", Version = "1.2.3" };
    protected static readonly SubjectId ArtifactSubject = Subject("artifact");
    protected static readonly SubjectId ScenarioSubject = Subject("scenario");
    protected static readonly SubjectId StepSubject = Subject("scenario/step/0");
    protected static readonly SubjectId ValueSubject = Subject("scenario/step/0/arguments/name");
    protected static readonly SubjectId ExternalSubject = new() { Value = "python://catalog/shared-type" };

    protected static AdapterDescriptor Descriptor(params GenerationFactCapability[] capabilities) => new()
    {
        Identity = Adapter,
        SourceLanguage = AdapterSourceLanguage.CSharp,
        Category = AdapterCategory.ApplicationFramework,
        CompatibleGenerationVersions = new GenerationVersionRange
        {
            MinimumInclusive = new Version(0, 14, 0),
            MaximumExclusive = new Version(2, 0, 0)
        },
        RequiredHostCapabilities =
        [
            AdapterHostCapability.SemanticAnalysis,
            AdapterHostCapability.AuthoredSource,
            AdapterHostCapability.SemanticAnalysis
        ],
        EmittedFactCapabilities = capabilities.Length == 0
            ? AllCapabilities()
            : [.. capabilities]
    };

    protected static ImmutableArray<GenerationFactCapability> AllCapabilities() =>
    [
        GenerationFactCapability.SpecificationValue,
        GenerationFactCapability.Relationship,
        GenerationFactCapability.Artifact,
        GenerationFactCapability.ConceptValidationRule,
        GenerationFactCapability.SpecificationScenario,
        GenerationFactCapability.ArtifactPlacement,
        GenerationFactCapability.ConceptAttribute,
        GenerationFactCapability.SpecificationStep,
        GenerationFactCapability.ConceptRepresentation,
        GenerationFactCapability.ArtifactDeclaration,
        GenerationFactCapability.ArtifactMemberDeclaration,
        GenerationFactCapability.ArtifactMemberTypeUse,
        GenerationFactCapability.TypeUseBinding,
        GenerationFactCapability.ArtifactMemberRole,
        GenerationFactCapability.Artifact
    ];

    protected static List<GenerationFact> EveryFact(
        IReadOnlyList<PropertyDefinition>? properties = null,
        IReadOnlyList<SpecificationStepKey>? scenarioSteps = null,
        IReadOnlyList<string>? valuePath = null,
        IReadOnlyList<TypeUseShapeKind>? typeUseShape = null)
    {
        var scenarioKey = ScenarioKey();
        var stepKey = StepKey();
        var valueKey = ValueKey(valuePath ?? ["arguments", "name"]);
        return
        [
            new ArtifactFact
            {
                Id = Id("artifact"),
                Subject = ArtifactSubject,
                Evidence = Evidence(),
                Definition = new ArtifactDefinition
                {
                    Key = ArtifactKey(ArtifactSubject, ArtifactKind.Command),
                    Name = "Register",
                    Properties = properties ??
                    [
                        new PropertyDefinition
                        {
                            Name = "second",
                            Type = new TypeReferenceDefinition
                            {
                                Name = "External",
                                Subject = ExternalSubject,
                                TargetArtifactKind = ArtifactKind.Concept
                            }
                        },
                        new PropertyDefinition
                        {
                            Name = "first",
                            Type = new TypeReferenceDefinition { Name = "String" }
                        }
                    ]
                }
            },
            new ArtifactPlacementFact
            {
                Id = Id("placement"),
                Subject = ArtifactSubject,
                Evidence = Evidence(),
                Artifact = ArtifactKey(ArtifactSubject, ArtifactKind.Command),
                Placement = new ArtifactPlacement
                {
                    Module = "Accounts",
                    Features = ["Registration", "Commands"],
                    Slice = "Register",
                    SliceKind = GenerationSliceKind.StateChange
                }
            },
            new ArtifactDeclarationFact
            {
                Id = Id("artifact-declaration"),
                Subject = ArtifactSubject,
                Evidence = Evidence(),
                Definition = new ArtifactDeclarationDefinition
                {
                    Artifact = ArtifactKey(ArtifactSubject, ArtifactKind.Command),
                    Name = "Register"
                }
            },
            new ArtifactMemberDeclarationFact
            {
                Id = Id("artifact-member"),
                Subject = ArtifactSubject,
                Evidence = Evidence(),
                Definition = new ArtifactMemberDeclarationDefinition
                {
                    Member = MemberKey("second"),
                    DeclarationOrder = 0
                }
            },
            new ArtifactMemberTypeUseFact
            {
                Id = Id("artifact-member-type-use"),
                Subject = ArtifactSubject,
                Evidence = Evidence(),
                Definition = new ArtifactMemberTypeUseDefinition
                {
                    Member = MemberKey("second"),
                    Type = new TypeUseDefinition
                    {
                        Name = "External",
                        ObservedTypeSubject = ExternalSubject,
                        Shape = typeUseShape ?? [TypeUseShapeKind.Optional, TypeUseShapeKind.Collection, TypeUseShapeKind.Named]
                    }
                }
            },
            new TypeUseBindingFact
            {
                Id = Id("type-use-binding"),
                Subject = ArtifactSubject,
                Evidence = Evidence(),
                Definition = new TypeUseBindingDefinition
                {
                    Member = MemberKey("second"),
                    Target = ArtifactKey(ExternalSubject, ArtifactKind.Concept)
                }
            },
            new ArtifactMemberRoleFact
            {
                Id = Id("artifact-member-role"),
                Subject = ArtifactSubject,
                Evidence = Evidence(),
                Definition = new ArtifactMemberRoleDefinition
                {
                    Member = MemberKey("second"),
                    Role = ArtifactMemberRoleKind.Identifier
                }
            },
            new RelationshipFact
            {
                Id = Id("relationship"),
                Subject = ArtifactSubject,
                Evidence = Evidence(),
                Definition = new RelationshipDefinition
                {
                    Key = new RelationshipKey
                    {
                        Kind = RelationshipKind.Produces,
                        Source = ArtifactSubject,
                        Target = ExternalSubject
                    }
                }
            },
            new ConceptRepresentationFact
            {
                Id = Id("concept-representation"),
                Subject = ArtifactSubject,
                Evidence = Evidence(),
                Definition = new ConceptRepresentationDefinition
                {
                    Concept = ArtifactSubject,
                    Kind = ConceptRepresentationKind.Primitive,
                    Primitive = GenerationPrimitiveKind.Text
                }
            },
            new ConceptAttributeFact
            {
                Id = Id("concept-attribute"),
                Subject = ArtifactSubject,
                Evidence = Evidence(),
                Definition = new ConceptAttributeDefinition
                {
                    Concept = ArtifactSubject,
                    Kind = ConceptAttributeKind.Named,
                    Name = "sensitive"
                }
            },
            new ConceptValidationRuleFact
            {
                Id = Id("concept-validation"),
                Subject = ArtifactSubject,
                Evidence = Evidence(),
                Definition = new ConceptValidationRuleDefinition
                {
                    Concept = ArtifactSubject,
                    RuleIdentity = "not-empty",
                    Kind = ConceptValidationRuleKind.NamedPredicate,
                    Predicate = "IsNotEmpty"
                }
            },
            new SpecificationScenarioFact
            {
                Id = Id("scenario"),
                Subject = ScenarioSubject,
                Evidence = Evidence(),
                Definition = new SpecificationScenarioDefinition
                {
                    Key = scenarioKey,
                    Name = "registering an account",
                    TargetArtifact = ArtifactKey(ExternalSubject, ArtifactKind.Command),
                    Steps = scenarioSteps ?? [stepKey]
                }
            },
            new SpecificationStepFact
            {
                Id = Id("step"),
                Subject = StepSubject,
                Evidence = Evidence(),
                Definition = new SpecificationStepDefinition
                {
                    Key = stepKey,
                    Phase = SpecificationStepPhase.When,
                    Kind = SpecificationStepKind.Command,
                    Artifact = ArtifactKey(ExternalSubject, ArtifactKind.Command),
                    Values = [valueKey]
                }
            },
            new SpecificationValueFact
            {
                Id = Id("value"),
                Subject = ValueSubject,
                Evidence = Evidence(),
                Definition = new SpecificationValueDefinition
                {
                    Key = valueKey,
                    Kind = SpecificationValueKind.Text,
                    Type = new TypeReferenceDefinition { Name = "External", Subject = ExternalSubject },
                    Scalar = "Alice"
                }
            }
        ];
    }

    protected static AdapterContribution Contribution(
        IReadOnlyList<GenerationFact>? facts = null,
        IReadOnlyList<GenerationDiagnostic>? diagnostics = null,
        AdapterIdentity? adapter = null) => new()
    {
        Adapter = adapter ?? Adapter,
        Facts = facts ?? EveryFact(),
        Diagnostics = diagnostics ?? []
    };

    protected static AdapterContributionAdmissionResult Admit(
        AdapterDescriptor? descriptor = null,
        AdapterContribution? contribution = null,
        ISourceAuthorityValidator? validator = null) =>
        AdapterContributionAdmission.Admit(
            descriptor ?? Descriptor(),
            contribution ?? Contribution(),
            validator ?? AcceptingSourceAuthorityValidator.Instance);

    protected static FactId Id(string value) => new() { Value = $"{Adapter.Id}:{value}" };

    sealed class AcceptingSourceAuthorityValidator : ISourceAuthorityValidator
    {
        public static AcceptingSourceAuthorityValidator Instance { get; } = new();

        public bool IsAuthoritative(SourceRange source) => true;
    }

    protected static Evidence Evidence(int line = 3) => new()
    {
        Adapter = Adapter,
        Strength = EvidenceStrength.Exact,
        Source = Source(line)
    };

    protected static SourceRange Source(int line = 3) => new()
    {
        Path = "Accounts/Register.cs",
        FileIdentity = new SourceFileIdentity
        {
            Project = "Accounts",
            Path = "Accounts/Register.cs"
        },
        StartLine = line,
        StartColumn = 1,
        EndLine = line,
        EndColumn = 20
    };

    protected static ArtifactKey ArtifactKey(SubjectId subject, ArtifactKind kind) => new()
    {
        Subject = subject,
        Kind = kind
    };

    protected static ArtifactMemberKey MemberKey(string name) => new()
    {
        Artifact = ArtifactKey(ArtifactSubject, ArtifactKind.Command),
        Name = name
    };

    protected static SpecificationScenarioKey ScenarioKey() => new() { Scenario = ScenarioSubject };

    protected static SpecificationStepKey StepKey() => new()
    {
        Scenario = ScenarioKey(),
        Index = 0
    };

    protected static SpecificationValueKey ValueKey(IReadOnlyList<string> path) => new()
    {
        Step = StepKey(),
        Path = path
    };

    protected static SubjectId Subject(string path) => new() { Value = $"dotnet://Accounts/{path}" };
}
