// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_granular_type_use_overlays_a_legacy_non_concept_binding : given.a_generator
{
    readonly SubjectId _targetSubject = new() { Value = "dotnet://Ordering/Types.CustomerCode" };
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var ownerSubject = new SubjectId { Value = "dotnet://Ordering/Events.RegistrationAttempted" };
        var owner = new ArtifactKey { Subject = ownerSubject, Kind = ArtifactKind.Event };
        var targetEvent = new ArtifactKey { Subject = _targetSubject, Kind = ArtifactKind.Event };
        var concept = new ArtifactKey { Subject = _targetSubject, Kind = ArtifactKind.Concept };
        var member = new ArtifactMemberKey { Artifact = owner, Name = "customerCode" };
        var evidence = new Evidence { Adapter = Adapter, Strength = EvidenceStrength.Exact };
        var placement = new ArtifactPlacement
        {
            Module = "Customers",
            Slice = "Register",
            SliceKind = GenerationSliceKind.StateChange
        };
        var facts = new GenerationFact[]
        {
            new ArtifactFact
            {
                Id = new FactId { Value = "owner:event" },
                Subject = ownerSubject,
                Evidence = evidence,
                Definition = new ArtifactDefinition
                {
                    Key = owner,
                    Name = "RegistrationAttempted",
                    Properties =
                    [
                        new PropertyDefinition
                        {
                            Name = "customerCode",
                            Type = new TypeReferenceDefinition
                            {
                                Name = "CustomerCodeEvent",
                                Subject = _targetSubject,
                                TargetArtifactKind = ArtifactKind.Event
                            }
                        }
                    ]
                }
            },
            new ArtifactMemberTypeUseFact
            {
                Id = new FactId { Value = "owner:type-use" },
                Subject = ownerSubject,
                Evidence = evidence,
                Definition = new ArtifactMemberTypeUseDefinition
                {
                    Member = member,
                    Type = new TypeUseDefinition
                    {
                        Name = "CustomerCodeEvent",
                        ObservedTypeSubject = _targetSubject
                    }
                }
            },
            new ArtifactPlacementFact
            {
                Id = new FactId { Value = "owner:placement" },
                Subject = ownerSubject,
                Evidence = evidence,
                Artifact = owner,
                Placement = placement
            },
            new ArtifactFact
            {
                Id = new FactId { Value = "target:event" },
                Subject = _targetSubject,
                Evidence = evidence,
                Definition = new ArtifactDefinition { Key = targetEvent, Name = "CustomerCodeEvent" }
            },
            new ArtifactPlacementFact
            {
                Id = new FactId { Value = "target:placement" },
                Subject = _targetSubject,
                Evidence = evidence,
                Artifact = targetEvent,
                Placement = placement
            },
            new ArtifactFact
            {
                Id = new FactId { Value = "target:concept" },
                Subject = _targetSubject,
                Evidence = evidence,
                Definition = new ArtifactDefinition { Key = concept, Name = "CustomerCode" }
            },
            new ConceptRepresentationFact
            {
                Id = new FactId { Value = "target:representation" },
                Subject = _targetSubject,
                Evidence = evidence,
                Definition = new ConceptRepresentationDefinition
                {
                    Concept = _targetSubject,
                    Kind = ConceptRepresentationKind.Primitive,
                    Primitive = GenerationPrimitiveKind.Text
                }
            }
        };

        _result = Generator.Generate(
            Snapshot(Completed(Adapter, facts)),
            new ScreenplayGenerationOptions { Domain = "Ordering" });
    }

    [Fact] void should_generate_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_preserve_the_legacy_exact_target_role() => Owner().Definition.Properties.Single().Type.TargetArtifactKind.ShouldEqual(ArtifactKind.Event);
    [Fact] void should_derive_the_existing_exact_event_target() => ((TypeUseBindingFact)_result.AdapterRun!.Derivation!.Facts.Single().Fact).Definition.Target.ShouldEqual(new ArtifactKey { Subject = _targetSubject, Kind = ArtifactKind.Event });
    [Fact] void should_not_substitute_the_same_subject_concept() => _result.Source.ShouldContain("customerCode CustomerCodeEvent");

    ResolvedArtifactVariant Owner() => _result.Graph.Artifacts
        .Single(artifact => artifact.Key.Subject.Value == "dotnet://Ordering/Events.RegistrationAttempted")
        .Variants.Single();
}
