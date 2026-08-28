// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_overlaying_one_legacy_artifact_member_binding : given.a_generator
{
    readonly AdapterIdentity _application = new() { Id = "legacy-application", Version = "1.0.0" };
    readonly AdapterIdentity _concepts = new() { Id = "concepts", Version = "2.0.0" };
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var eventSubject = new SubjectId { Value = "dotnet://Ordering/Events.CustomerRegistered" };
        var conceptSubject = new SubjectId { Value = "dotnet://Ordering/Concepts.CustomerCode" };
        var artifact = new ArtifactKey { Subject = eventSubject, Kind = ArtifactKind.Event };
        var member = new ArtifactMemberKey { Artifact = artifact, Name = "customerCode" };
        var applicationEvidence = Evidence(_application);
        var applicationFacts = new GenerationFact[]
        {
            new ArtifactFact
            {
                Id = Id(_application, "event"),
                Subject = eventSubject,
                Evidence = applicationEvidence,
                Definition = new ArtifactDefinition
                {
                    Key = artifact,
                    Name = "CustomerRegistered",
                    Properties =
                    [
                        new PropertyDefinition
                        {
                            Name = "customerCode",
                            Type = new TypeReferenceDefinition { Name = "UnresolvedCustomerCode" }
                        }
                    ]
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
                        Name = "UnresolvedCustomerCode",
                        ObservedTypeSubject = conceptSubject
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
                    Slice = "Register",
                    SliceKind = GenerationSliceKind.StateChange
                }
            }
        };
        var conceptEvidence = Evidence(_concepts);
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
                    Name = "CustomerCode"
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

        _result = Generator.Generate(
            Snapshot(Completed(_application, applicationFacts), Completed(_concepts, conceptFacts)),
            new ScreenplayGenerationOptions { Domain = "Ordering" });
    }

    [Fact] void should_generate_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_bind_only_the_matching_legacy_property() => Event().Definition.Properties.Single().Type.Subject!.Value.ShouldEqual("dotnet://Ordering/Concepts.CustomerCode");
    [Fact] void should_lower_the_concept_name_instead_of_the_unresolved_source_name() => _result.Source.ShouldContain("customerCode CustomerCode");
    [Fact] void should_not_emit_the_unresolved_source_name() => _result.Source.ShouldNotContain("UnresolvedCustomerCode");
    [Fact] void should_retain_the_legacy_aggregate_as_overlay_provenance() => DirectDisposition("legacy-application:event").ShouldEqual(GenerationFactDisposition.ProvenanceOnly);
    [Fact] void should_retain_the_granular_type_use_as_provenance() => DirectDisposition("legacy-application:type-use").ShouldEqual(GenerationFactDisposition.ProvenanceOnly);
    [Fact] void should_lower_the_granular_derived_binding() => _result.AdapterRun!.Derivation!.Facts.Single().Disposition.ShouldEqual(GenerationFactDisposition.Lowered);

    ResolvedArtifactVariant Event() => _result.Graph.Artifacts.Single(artifact => artifact.Key.Kind == ArtifactKind.Event).Variants.Single();

    GenerationFactDisposition DirectDisposition(string id) => _result.AdapterRun!.Facts.Single(record => record.Fact.Id.Value == id).Disposition;

    static FactId Id(AdapterIdentity adapter, string suffix) => new() { Value = $"{adapter.Id}:{suffix}" };

    static Evidence Evidence(AdapterIdentity adapter) => new()
    {
        Adapter = adapter,
        Strength = EvidenceStrength.Exact
    };
}
