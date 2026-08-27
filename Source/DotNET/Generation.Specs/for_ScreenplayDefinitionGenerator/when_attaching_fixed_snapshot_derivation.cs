// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_attaching_fixed_snapshot_derivation : given.a_generator
{
    const string ExpectedBindingId = "generation:derive:type-use-binding:1:" +
                                     "0064006F0074006E00650074003A002F002F004F00720064006500720069006E0067002F0043006F006D006D0061006E00640073002E005200650067006900730074006500720043007500730074006F006D00650072" +
                                     ":3:" +
                                     "0063007500730074006F006D006500720043006F00640065" +
                                     ":" +
                                     "0064006F0074006E00650074003A002F002F004F00720064006500720069006E0067002F0043006F006E00630065007000740073002E0043007500730074006F006D006500720043006F00640065" +
                                     ":1";
    readonly AdapterIdentity _application = new() { Id = "application", Version = "1.0.0" };
    readonly AdapterIdentity _concepts = new() { Id = "concepts", Version = "2.0.0" };
    readonly SubjectId _commandSubject = new() { Value = "dotnet://Ordering/Commands.RegisterCustomer" };
    readonly SubjectId _conceptSubject = new() { Value = "dotnet://Ordering/Concepts.CustomerCode" };
    readonly List<TypeUseShapeKind> _shape = [TypeUseShapeKind.Optional, TypeUseShapeKind.Named];
    Evidence _admittedTypeUseEvidence = null!;
    GeneratedScreenplayDefinition _forward = null!;
    GeneratedScreenplayDefinition _reverse = null!;
    string _beforeMutation = string.Empty;

    void Because()
    {
        var command = new ArtifactKey { Subject = _commandSubject, Kind = ArtifactKind.Command };
        var missingSubject = new SubjectId { Value = "dotnet://Ordering/Concepts.MissingCode" };
        var applicationFacts = new GenerationFact[]
        {
            new ArtifactDeclarationFact
            {
                Id = Id(_application, "command"),
                Subject = _commandSubject,
                Evidence = Evidence(_application),
                Definition = new ArtifactDeclarationDefinition
                {
                    Artifact = command,
                    Name = "RegisterCustomer"
                }
            },
            Member(_application, command, _commandSubject, "customerCode", 0),
            TypeUse(_application, command, _commandSubject, "customerCode", _conceptSubject, "customer-code", _shape),
            Member(_application, command, _commandSubject, "missingCode", 1),
            TypeUse(_application, command, _commandSubject, "missingCode", missingSubject, "missing-code", [TypeUseShapeKind.Named])
        };
        var conceptFacts = new GenerationFact[]
        {
            new ArtifactFact
            {
                Id = Id(_concepts, "customer-code"),
                Subject = _conceptSubject,
                Evidence = Evidence(_concepts),
                Definition = new ArtifactDefinition
                {
                    Key = new ArtifactKey { Subject = _conceptSubject, Kind = ArtifactKind.Concept },
                    Name = "CustomerCode"
                }
            },
            new ConceptRepresentationFact
            {
                Id = Id(_concepts, "customer-code-representation"),
                Subject = _conceptSubject,
                Evidence = Evidence(_concepts),
                Definition = new ConceptRepresentationDefinition
                {
                    Concept = _conceptSubject,
                    Kind = ConceptRepresentationKind.Primitive,
                    Primitive = GenerationPrimitiveKind.Text
                }
            }
        };
        var applicationForward = Admit(_application, applicationFacts);
        var applicationReverse = Admit(_application, [.. applicationFacts.AsEnumerable().Reverse()]);
        var conceptsForward = Admit(_concepts, conceptFacts);
        var conceptsReverse = Admit(_concepts, [.. conceptFacts.AsEnumerable().Reverse()]);
        _admittedTypeUseEvidence = applicationForward.Facts
            .OfType<ArtifactMemberTypeUseFact>()
            .Single(fact => fact.Definition.Member.Name == "customerCode")
            .Evidence;
        var options = new ScreenplayGenerationOptions { Domain = "Ordering" };
        _forward = Generator.Generate(
            Snapshot(Completed(applicationForward), Completed(conceptsForward)),
            options);
        _reverse = Generator.Generate(
            Snapshot(Completed(conceptsReverse), Completed(applicationReverse)),
            options);
        _beforeMutation = AdapterRunProjection(_forward.AdapterRun);

        _shape.Clear();
    }

    [Fact] void should_attach_the_closed_derivation_rule() => Derivation().Rules.Single().Rule.ShouldEqual(new GenerationDerivationRuleIdentity { Id = "cratis.screenplay.type-use-binding", Version = "1.0.0" });
    [Fact] void should_attach_the_exact_derived_binding() => Binding().Definition.ShouldEqual(new TypeUseBindingDefinition { Member = new ArtifactMemberKey { Artifact = new ArtifactKey { Subject = _commandSubject, Kind = ArtifactKind.Command }, Name = "customerCode" }, Target = new ArtifactKey { Subject = _conceptSubject, Kind = ArtifactKind.Concept } });
    [Fact] void should_attach_the_exact_stable_binding_identity() => Binding().Id.Value.ShouldEqual(ExpectedBindingId);
    [Fact] void should_attach_the_exact_lineage_producer() => Record().Lineage!.Producer.ShouldEqual(new GenerationDerivationRuleIdentity { Id = "cratis.screenplay.type-use-binding", Version = "1.0.0" });
    [Fact] void should_attach_the_exact_canonical_lineage_inputs() => Record().Lineage!.Inputs.Select(input => input.Value).ShouldEqual("application:command", "application:member:customerCode", "application:type-use:customer-code", "concepts:customer-code");
    [Fact] void should_attach_evidence_corresponding_to_every_lineage_input() => Record().Lineage!.Evidence.ShouldEqual(Evidence(_application), Evidence(_application), Evidence(_application), Evidence(_concepts));
    [Fact] void should_propagate_derivation_diagnostics_to_the_generated_result() => _forward.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(GenerationDiagnosticCodes.MissingTypeUseTarget);
    [Fact] void should_retain_derivation_diagnostics_on_the_adapter_run() => Derivation().Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(GenerationDiagnosticCodes.MissingTypeUseTarget);
    [Fact] void should_canonicalize_adapter_and_fact_permutations() => AdapterRunProjection(_reverse.AdapterRun).ShouldEqual(AdapterRunProjection(_forward.AdapterRun));
    [Fact] void should_generate_identical_source_for_adapter_and_fact_permutations() => _reverse.Source.ShouldEqual(_forward.Source);
    [Fact] void should_remain_deeply_immutable_after_input_mutation() => AdapterRunProjection(_forward.AdapterRun).ShouldEqual(_beforeMutation);
    [Fact] void should_deep_copy_admitted_input_evidence_into_lineage() => ReferenceEquals(Record().Lineage!.Evidence[2], _admittedTypeUseEvidence).ShouldBeFalse();

    GenerationDerivationSnapshot Derivation() => _forward.AdapterRun!.Derivation!;

    GenerationFactRecord Record() => Derivation().Facts.Single();

    TypeUseBindingFact Binding() => (TypeUseBindingFact)Record().Fact;

    static AdapterContributionSnapshot Admit(AdapterIdentity adapter, IReadOnlyList<GenerationFact> facts)
    {
        var descriptor = new AdapterDescriptor
        {
            Identity = adapter,
            SourceLanguage = AdapterSourceLanguage.SourceIndependent,
            Category = AdapterCategory.ApplicationFramework,
            EmittedFactCapabilities =
            [
                GenerationFactCapability.Artifact,
                GenerationFactCapability.ConceptRepresentation,
                GenerationFactCapability.ArtifactDeclaration,
                GenerationFactCapability.ArtifactMemberDeclaration,
                GenerationFactCapability.ArtifactMemberTypeUse
            ]
        };
        return AdapterContributionAdmission.Admit(
            descriptor,
            new AdapterContribution { Adapter = adapter, Facts = facts }).Snapshot!;
    }

    static AdapterRunRecord Completed(AdapterContributionSnapshot contribution) => new()
    {
        Considered = true,
        Probed = true,
        Executed = true,
        Descriptor = contribution.Descriptor,
        Probe = new AdapterProbeApplicable(),
        Execution = new AdapterExecutionCompleted { Contribution = contribution },
        Disposition = AdapterRunDisposition.Admitted
    };

    static ArtifactMemberDeclarationFact Member(
        AdapterIdentity adapter,
        ArtifactKey artifact,
        SubjectId subject,
        string name,
        int order) => new()
    {
        Id = Id(adapter, $"member:{name}"),
        Subject = subject,
        Evidence = Evidence(adapter),
        Definition = new ArtifactMemberDeclarationDefinition
        {
            Member = new ArtifactMemberKey { Artifact = artifact, Name = name },
            DeclarationOrder = order
        }
    };

    static ArtifactMemberTypeUseFact TypeUse(
        AdapterIdentity adapter,
        ArtifactKey artifact,
        SubjectId subject,
        string name,
        SubjectId observedType,
        string suffix,
        IReadOnlyList<TypeUseShapeKind> shape) => new()
    {
        Id = Id(adapter, $"type-use:{suffix}"),
        Subject = subject,
        Evidence = Evidence(adapter),
        Definition = new ArtifactMemberTypeUseDefinition
        {
            Member = new ArtifactMemberKey { Artifact = artifact, Name = name },
            Type = new TypeUseDefinition
            {
                Name = observedType.Value.Split('.')[^1],
                ObservedTypeSubject = observedType,
                Shape = shape
            }
        }
    };

    static FactId Id(AdapterIdentity adapter, string suffix) => new() { Value = $"{adapter.Id}:{suffix}" };

    static Evidence Evidence(AdapterIdentity adapter) => new()
    {
        Adapter = adapter,
        Strength = EvidenceStrength.Exact
    };
}
