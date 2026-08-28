// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_type_use_bindings_conflict : given.a_generator
{
    readonly AdapterIdentity _application = new() { Id = "application", Version = "1.0.0" };
    readonly AdapterIdentity _concepts = new() { Id = "concepts", Version = "2.0.0" };
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var eventSubject = new SubjectId { Value = "dotnet://Ordering/Events.CustomerRegistered" };
        var firstConcept = new SubjectId { Value = "dotnet://Ordering/Concepts.CustomerCode" };
        var secondConcept = new SubjectId { Value = "dotnet://Ordering/Concepts.LegacyCustomerCode" };
        var artifact = new ArtifactKey { Subject = eventSubject, Kind = ArtifactKind.Event };
        var member = new ArtifactMemberKey { Artifact = artifact, Name = "customerCode" };
        var evidence = new Evidence { Adapter = _application, Strength = EvidenceStrength.Exact };
        var applicationFacts = new GenerationFact[]
        {
            new ArtifactFact
            {
                Id = Id(_application, "event"),
                Subject = eventSubject,
                Evidence = evidence,
                Definition = new ArtifactDefinition
                {
                    Key = artifact,
                    Name = "CustomerRegistered",
                    Properties =
                    [
                        new PropertyDefinition
                        {
                            Name = "customerCode",
                            Type = new TypeReferenceDefinition { Name = "RawCustomerCode" }
                        }
                    ]
                }
            },
            new ArtifactMemberTypeUseFact
            {
                Id = Id(_application, "type-use"),
                Subject = eventSubject,
                Evidence = evidence,
                Definition = new ArtifactMemberTypeUseDefinition
                {
                    Member = member,
                    Type = new TypeUseDefinition
                    {
                        Name = "RawCustomerCode",
                        ObservedTypeSubject = firstConcept
                    }
                }
            },
            new TypeUseBindingFact
            {
                Id = Id(_application, "legacy-binding"),
                Subject = eventSubject,
                Evidence = evidence,
                Definition = new TypeUseBindingDefinition
                {
                    Member = member,
                    Target = new ArtifactKey { Subject = secondConcept, Kind = ArtifactKind.Concept }
                }
            },
            new ArtifactPlacementFact
            {
                Id = Id(_application, "placement"),
                Subject = eventSubject,
                Evidence = evidence,
                Artifact = artifact,
                Placement = new ArtifactPlacement
                {
                    Module = "Customers",
                    Slice = "Register",
                    SliceKind = GenerationSliceKind.StateChange
                }
            }
        };
        var conceptEvidence = new Evidence { Adapter = _concepts, Strength = EvidenceStrength.Exact };
        var conceptFacts = ConceptFacts(firstConcept, "CustomerCode", "customer-code", conceptEvidence)
            .Concat(ConceptFacts(secondConcept, "LegacyCustomerCode", "legacy-customer-code", conceptEvidence))
            .ToArray();

        _result = Generator.Generate(
            Snapshot(Completed(_application, applicationFacts), Completed(_concepts, conceptFacts)),
            new ScreenplayGenerationOptions { Domain = "Ordering" });
    }

    [Fact] void should_fail_closed() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_binding_conflict() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(GenerationDiagnosticCodes.ConflictingArtifactMember);
    [Fact] void should_not_choose_either_target_subject() => Event().Definition.Properties.Single().Type.Subject.ShouldBeNull();
    [Fact] void should_conflict_the_direct_binding() => DirectBinding().Disposition.ShouldEqual(GenerationFactDisposition.Conflicted);
    [Fact] void should_conflict_the_derived_binding() => DerivedBinding().Disposition.ShouldEqual(GenerationFactDisposition.Conflicted);
    [Fact] void should_associate_the_same_conflict_with_both_bindings() => DirectBinding().Diagnostics.Select(diagnostic => diagnostic.Code).Concat(DerivedBinding().Diagnostics.Select(diagnostic => diagnostic.Code)).ShouldContainOnly(GenerationDiagnosticCodes.ConflictingArtifactMember, GenerationDiagnosticCodes.ConflictingArtifactMember);

    ResolvedArtifactVariant Event() => _result.Graph.Artifacts.Single(artifact => artifact.Key.Kind == ArtifactKind.Event).Variants.Single();

    GenerationFactRecord DirectBinding() => _result.AdapterRun!.Facts.Single(record => record.Fact.Id.Value == "application:legacy-binding");

    GenerationFactRecord DerivedBinding() => _result.AdapterRun!.Derivation!.Facts.Single();

    static GenerationFact[] ConceptFacts(SubjectId subject, string name, string suffix, Evidence evidence) =>
    [
        new ArtifactFact
        {
            Id = Id(evidence.Adapter, suffix),
            Subject = subject,
            Evidence = evidence,
            Definition = new ArtifactDefinition
            {
                Key = new ArtifactKey { Subject = subject, Kind = ArtifactKind.Concept },
                Name = name
            }
        },
        new ConceptRepresentationFact
        {
            Id = Id(evidence.Adapter, $"{suffix}:representation"),
            Subject = subject,
            Evidence = evidence,
            Definition = new ConceptRepresentationDefinition
            {
                Concept = subject,
                Kind = ConceptRepresentationKind.Primitive,
                Primitive = GenerationPrimitiveKind.Text
            }
        }
    ];

    static FactId Id(AdapterIdentity adapter, string suffix) => new() { Value = $"{adapter.Id}:{suffix}" };
}
