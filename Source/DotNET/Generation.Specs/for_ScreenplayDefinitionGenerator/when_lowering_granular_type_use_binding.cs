// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_lowering_granular_type_use_binding : given.a_generator
{
    readonly AdapterIdentity _application = new() { Id = "application", Version = "1.0.0" };
    readonly AdapterIdentity _concepts = new() { Id = "concepts", Version = "2.0.0" };
    GeneratedScreenplayDefinition _contribution = null!;
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var eventSubject = new SubjectId { Value = "dotnet://Ordering/Events.CustomerRegistered" };
        var conceptSubject = new SubjectId { Value = "dotnet://Ordering/Concepts.CustomerCode" };
        var artifact = new ArtifactKey { Subject = eventSubject, Kind = ArtifactKind.Event };
        var member = new ArtifactMemberKey { Artifact = artifact, Name = "customerCode" };
        var applicationEvidence = Evidence(_application, "Customers/Register/CustomerRegistered.cs");
        var conceptEvidence = Evidence(_concepts, "Concepts/CustomerCode.cs");
        var applicationFacts = new GenerationFact[]
        {
            new ArtifactDeclarationFact
            {
                Id = Id(_application, "event"),
                Subject = eventSubject,
                Evidence = applicationEvidence,
                Definition = new ArtifactDeclarationDefinition
                {
                    Artifact = artifact,
                    Name = "CustomerRegistered",
                    File = "Customers/Register/CustomerRegistered.cs"
                }
            },
            new ArtifactMemberDeclarationFact
            {
                Id = Id(_application, "member"),
                Subject = eventSubject,
                Evidence = applicationEvidence,
                Definition = new ArtifactMemberDeclarationDefinition
                {
                    Member = member,
                    DeclarationOrder = 0
                }
            },
            new ArtifactMemberTypeUseFact
            {
                Id = Id(_application, "type-use"),
                Subject = eventSubject,
                Evidence = applicationEvidence,
                Definition = new ArtifactMemberTypeUseDefinition
                {
                    Member = member,
                    Type = new TypeUseDefinition
                    {
                        Name = "CustomerCode",
                        ObservedTypeSubject = conceptSubject,
                        Shape = [TypeUseShapeKind.Optional, TypeUseShapeKind.Named]
                    }
                }
            },
            new ArtifactPlacementFact
            {
                Id = Id(_application, "placement"),
                Subject = eventSubject,
                Evidence = applicationEvidence,
                Artifact = artifact,
                Placement = new ArtifactPlacement
                {
                    Module = "Customers",
                    Features = ["Registration"],
                    Slice = "Register",
                    SliceKind = GenerationSliceKind.StateChange
                }
            }
        };
        var conceptFacts = new GenerationFact[]
        {
            new ArtifactFact
            {
                Id = Id(_concepts, "concept"),
                Subject = conceptSubject,
                Evidence = conceptEvidence,
                Definition = new ArtifactDefinition
                {
                    Key = new ArtifactKey { Subject = conceptSubject, Kind = ArtifactKind.Concept },
                    Name = "CustomerCode",
                    File = "Concepts/CustomerCode.cs"
                }
            },
            new ConceptRepresentationFact
            {
                Id = Id(_concepts, "representation"),
                Subject = conceptSubject,
                Evidence = conceptEvidence,
                Definition = new ConceptRepresentationDefinition
                {
                    Concept = conceptSubject,
                    Kind = ConceptRepresentationKind.Primitive,
                    Primitive = GenerationPrimitiveKind.Text
                }
            }
        };

        var options = new ScreenplayGenerationOptions { Domain = "Ordering" };
        _result = Generator.Generate(
            Snapshot(
                Completed(_application, applicationFacts),
                Completed(_concepts, conceptFacts)),
            options);
        _contribution = Generator.Generate(
            [
                new AdapterContribution { Adapter = _concepts, Facts = conceptFacts },
                new AdapterContribution { Adapter = _application, Facts = applicationFacts }
            ],
            options);
    }

    [Fact] void should_generate_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_preserve_contribution_overload_compatibility() => _contribution.Source.ShouldEqual(_result.Source);
    [Fact] void should_resolve_the_same_contribution_overload_binding() => ContributionEvent().Definition.Properties.Single().Type.Subject.ShouldEqual(Event().Definition.Properties.Single().Type.Subject);
    [Fact] void should_resolve_the_same_contribution_overload_diagnostics() => _contribution.Diagnostics.ShouldContainOnly(_result.Diagnostics);
    [Fact] void should_leave_the_contribution_overload_without_an_adapter_run() => _contribution.AdapterRun.ShouldBeNull();
    [Fact] void should_materialize_one_effective_event_property() => Event().Definition.Properties.Select(property => property.Name).ShouldEqual(["customerCode"]);
    [Fact] void should_bind_the_effective_property_to_the_exact_concept_subject() => Event().Definition.Properties.Single().Type.Subject!.Value.ShouldEqual("dotnet://Ordering/Concepts.CustomerCode");
    [Fact] void should_preserve_exact_optionality_in_screenplay() => _result.Source.ShouldContain("customerCode CustomerCode?");
    [Fact] void should_retain_the_declaration_as_provenance() => Disposition("application:event").ShouldEqual(GenerationFactDisposition.ProvenanceOnly);
    [Fact] void should_retain_the_member_declaration_as_provenance() => Disposition("application:member").ShouldEqual(GenerationFactDisposition.ProvenanceOnly);
    [Fact] void should_retain_the_type_use_as_provenance() => Disposition("application:type-use").ShouldEqual(GenerationFactDisposition.ProvenanceOnly);
    [Fact] void should_lower_the_derived_binding() => _result.AdapterRun!.Derivation!.Facts.Single().Disposition.ShouldEqual(GenerationFactDisposition.Lowered);
    [Fact] void should_not_leave_any_direct_or_derived_disposition_unknown() => _result.AdapterRun!.Facts.Concat(_result.AdapterRun.Derivation!.Facts).Any(record => record.Disposition == GenerationFactDisposition.Unknown).ShouldBeFalse();

    ResolvedArtifactVariant Event() => _result.Graph.Artifacts
        .Single(artifact => artifact.Key.Kind == ArtifactKind.Event)
        .Variants.Single();

    ResolvedArtifactVariant ContributionEvent() => _contribution.Graph.Artifacts
        .Single(artifact => artifact.Key.Kind == ArtifactKind.Event)
        .Variants.Single();

    GenerationFactDisposition Disposition(string id) => _result.AdapterRun!.Facts
        .Single(record => record.Fact.Id.Value == id)
        .Disposition;

    static FactId Id(AdapterIdentity adapter, string suffix) => new() { Value = $"{adapter.Id}:{suffix}" };

    static Evidence Evidence(AdapterIdentity adapter, string path) => new()
    {
        Adapter = adapter,
        Strength = EvidenceStrength.Exact,
        Source = new SourceRange
        {
            Path = path,
            StartLine = 1,
            StartColumn = 1,
            EndLine = 1,
            EndColumn = 20
        }
    };
}
