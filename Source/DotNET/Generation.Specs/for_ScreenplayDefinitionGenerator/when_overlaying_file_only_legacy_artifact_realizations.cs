// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_overlaying_file_only_legacy_artifact_realizations : given.a_generator
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var eventSubject = new SubjectId { Value = "dotnet://Ordering/Events.CustomerRegistered" };
        var conceptSubject = new SubjectId { Value = "dotnet://Ordering/Concepts.CustomerCode" };
        var artifact = new ArtifactKey { Subject = eventSubject, Kind = ArtifactKind.Event };
        var member = new ArtifactMemberKey { Artifact = artifact, Name = "customerCode" };
        var evidence = new Evidence { Adapter = Adapter, Strength = EvidenceStrength.Exact };
        var first = Legacy("event:first", "Events/First.cs", artifact, eventSubject, evidence);
        var second = Legacy("event:second", "Events/Second.cs", artifact, eventSubject, evidence);
        var facts = new GenerationFact[]
        {
            first,
            second,
            new ArtifactMemberTypeUseFact
            {
                Id = new FactId { Value = "event:type-use" },
                Subject = eventSubject,
                Evidence = evidence,
                Definition = new ArtifactMemberTypeUseDefinition
                {
                    Member = member,
                    Type = new TypeUseDefinition { Name = "RawCustomerCode" }
                }
            },
            new TypeUseBindingFact
            {
                Id = new FactId { Value = "event:binding" },
                Subject = eventSubject,
                Evidence = evidence,
                Definition = new TypeUseBindingDefinition
                {
                    Member = member,
                    Target = new ArtifactKey { Subject = conceptSubject, Kind = ArtifactKind.Concept }
                }
            },
            new ArtifactPlacementFact
            {
                Id = new FactId { Value = "event:placement" },
                Subject = eventSubject,
                Evidence = evidence,
                Artifact = artifact,
                Placement = new ArtifactPlacement
                {
                    Module = "Customers",
                    Slice = "Register",
                    SliceKind = GenerationSliceKind.StateChange
                }
            },
            new ArtifactFact
            {
                Id = new FactId { Value = "concept:customer-code" },
                Subject = conceptSubject,
                Evidence = evidence,
                Definition = new ArtifactDefinition
                {
                    Key = new ArtifactKey { Subject = conceptSubject, Kind = ArtifactKind.Concept },
                    Name = "CustomerCode"
                }
            },
            new ConceptRepresentationFact
            {
                Id = new FactId { Value = "concept:customer-code:representation" },
                Subject = conceptSubject,
                Evidence = evidence,
                Definition = new ConceptRepresentationDefinition
                {
                    Concept = conceptSubject,
                    Kind = ConceptRepresentationKind.Primitive,
                    Primitive = GenerationPrimitiveKind.Text
                }
            }
        };

        _result = Generator.Generate(
            Snapshot(Completed(Adapter, facts)),
            new ScreenplayGenerationOptions { Domain = "Ordering" });
    }

    [Fact] void should_retain_one_effective_semantic_variant() => Event().Variants.Count.ShouldEqual(1);
    [Fact] void should_retain_both_file_realizations() => Event().Variants.Single().Files.ShouldEqual("Events/First.cs", "Events/Second.cs");
    [Fact] void should_bind_the_effective_semantic_variant() => Event().Variants.Single().Definition.Properties.Single().Type.Subject!.Value.ShouldEqual("dotnet://Ordering/Concepts.CustomerCode");
    [Fact] void should_retain_both_complete_legacy_support_facts_as_provenance() => Dispositions("event:first", "event:second").ShouldContainOnly(GenerationFactDisposition.ProvenanceOnly, GenerationFactDisposition.ProvenanceOnly);
    [Fact] void should_lower_the_binding_supporting_the_effective_variant() => Dispositions("event:binding").ShouldContainOnly(GenerationFactDisposition.Lowered);

    ResolvedArtifact Event() => _result.Graph.Artifacts.Single(artifact => artifact.Key.Kind == ArtifactKind.Event);

    GenerationFactDisposition[] Dispositions(params string[] ids) =>
    [
        .. ids.Select(id => _result.AdapterRun!.Facts.Single(record => record.Fact.Id.Value == id).Disposition)
    ];

    static ArtifactFact Legacy(
        string id,
        string file,
        ArtifactKey artifact,
        SubjectId subject,
        Evidence evidence) => new()
    {
        Id = new FactId { Value = id },
        Subject = subject,
        Evidence = evidence,
        Definition = new ArtifactDefinition
        {
            Key = artifact,
            Name = "CustomerRegistered",
            File = file,
            Properties =
            [
                new PropertyDefinition
                {
                    Name = "customerCode",
                    Type = new TypeReferenceDefinition { Name = "RawCustomerCode" }
                }
            ]
        }
    };
}
