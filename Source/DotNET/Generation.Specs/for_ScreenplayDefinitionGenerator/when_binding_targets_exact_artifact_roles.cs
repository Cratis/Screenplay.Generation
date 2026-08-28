// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_binding_targets_exact_artifact_roles : given.a_generator
{
    readonly SubjectId _targetSubject = new() { Value = "dotnet://Ordering/Types.CustomerCode" };
    GeneratedScreenplayDefinition _exact = null!;
    GeneratedScreenplayDefinition _missingRole = null!;

    void Because()
    {
        _missingRole = Generate(includeTargetEvent: false);
        _exact = Generate(includeTargetEvent: true);
    }

    [Fact] void should_reject_an_undeclared_exact_target_role() => _missingRole.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(GenerationDiagnosticCodes.MissingTypeUseBindingTarget);
    [Fact] void should_omit_the_owner_when_the_exact_target_role_is_missing() => _missingRole.Graph.Artifacts.Any(artifact => artifact.Key.Subject.Value == "dotnet://Ordering/Events.RegistrationAttempted").ShouldBeFalse();
    [Fact] void should_omit_the_invalid_binding_with_its_diagnostic() => Binding(_missingRole).Disposition.ShouldEqual(GenerationFactDisposition.OmittedWithDiagnostic);
    [Fact] void should_retain_the_complete_exact_target_key() => Owner(_exact).Definition.Properties.Single().Type.TargetArtifactKind.ShouldEqual(ArtifactKind.Event);
    [Fact] void should_not_resolve_an_event_binding_as_the_same_subject_concept() => _exact.Source.ShouldContain("customerCode CustomerCodeEvent");
    [Fact] void should_not_substitute_the_same_subject_concept_name() => _exact.Source.ShouldNotContain("customerCode CustomerCode\n");
    [Fact] void should_lower_the_exact_declared_event_binding() => Binding(_exact).Disposition.ShouldEqual(GenerationFactDisposition.Lowered);

    GeneratedScreenplayDefinition Generate(bool includeTargetEvent)
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
        var facts = new List<GenerationFact>
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
                            Type = new TypeReferenceDefinition { Name = "CustomerCodeEvent" }
                        }
                    ]
                }
            },
            new TypeUseBindingFact
            {
                Id = new FactId { Value = "owner:binding" },
                Subject = ownerSubject,
                Evidence = evidence,
                Definition = new TypeUseBindingDefinition
                {
                    Member = member,
                    Target = targetEvent
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
        if (includeTargetEvent)
        {
            facts.Add(new ArtifactFact
            {
                Id = new FactId { Value = "target:event" },
                Subject = _targetSubject,
                Evidence = evidence,
                Definition = new ArtifactDefinition { Key = targetEvent, Name = "CustomerCodeEvent" }
            });
            facts.Add(new ArtifactPlacementFact
            {
                Id = new FactId { Value = "target:placement" },
                Subject = _targetSubject,
                Evidence = evidence,
                Artifact = targetEvent,
                Placement = placement
            });
        }

        return Generator.Generate(
            Snapshot(Completed(Adapter, facts)),
            new ScreenplayGenerationOptions { Domain = "Ordering" });
    }

    ResolvedArtifactVariant Owner(GeneratedScreenplayDefinition result) => result.Graph.Artifacts
        .Single(artifact => artifact.Key.Subject.Value == "dotnet://Ordering/Events.RegistrationAttempted")
        .Variants.Single();

    static GenerationFactRecord Binding(GeneratedScreenplayDefinition result) => result.AdapterRun!.Facts
        .Single(record => record.Fact.Id.Value == "owner:binding");
}
