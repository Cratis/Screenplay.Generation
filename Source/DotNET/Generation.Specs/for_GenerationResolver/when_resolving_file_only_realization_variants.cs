// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_resolving_file_only_realization_variants : Specification
{
    readonly AdapterIdentity _adapter = new() { Id = "files", Version = "1.0.0" };
    ResolvedApplicationGraph _forward = null!;
    ResolvedApplicationGraph _reverse = null!;

    void Because()
    {
        var conceptSubject = new SubjectId { Value = "dotnet://Banking/Concepts.AccountNumber" };
        var eventSubject = new SubjectId { Value = "dotnet://Banking/Events.AccountOpened" };
        var eventKey = new ArtifactKey { Subject = eventSubject, Kind = ArtifactKind.Event };
        var facts = new GenerationFact[]
        {
            Artifact("files:event:a", eventKey, "Events/A.cs"),
            Artifact("files:event:b", eventKey, "Events/B.cs"),
            Validation("files:validation:a", conceptSubject, "Validation/A.cs"),
            Validation("files:validation:b", conceptSubject, "Validation/B.cs")
        };
        var resolver = new GenerationResolver();
        _forward = resolver.Resolve([new AdapterContribution { Adapter = _adapter, Facts = facts }]);
        _reverse = resolver.Resolve([new AdapterContribution { Adapter = _adapter, Facts = [.. facts.AsEnumerable().Reverse()] }]);
    }

    [Fact] void should_resolve_one_semantic_artifact_variant() => Artifact().Variants.Count.ShouldEqual(1);
    [Fact] void should_not_report_a_semantic_artifact_conflict() => Artifact().IsConflicted.ShouldBeFalse();
    [Fact] void should_retain_both_artifact_realization_files() => Artifact().Variants.Single().Files.ShouldEqual("Events/A.cs", "Events/B.cs");
    [Fact] void should_choose_the_canonical_compatibility_file() => Artifact().Variants.Single().Definition.File.ShouldEqual("Events/A.cs");
    [Fact] void should_resolve_one_semantic_validation_variant() => Validation().Variants.Count.ShouldEqual(1);
    [Fact] void should_retain_both_validation_implementation_files() => Validation().Variants.Single().ImplementationFiles.ShouldEqual("Validation/A.cs", "Validation/B.cs");
    [Fact] void should_choose_the_canonical_implementation_file() => Validation().Variants.Single().Definition.ImplementationFile.ShouldEqual("Validation/A.cs");
    [Fact] void should_be_independent_of_fact_order() => Projection(_reverse).ShouldEqual(Projection(_forward));
    [Fact] void should_not_report_file_only_conflicts() => _forward.Diagnostics.ShouldBeEmpty();

    ResolvedArtifact Artifact() => _forward.Artifacts.Single();

    ResolvedConceptValidationRule Validation() => _forward.ConceptValidationRules.Single();

    ArtifactFact Artifact(string id, ArtifactKey key, string file) => new()
    {
        Id = new FactId { Value = id },
        Subject = key.Subject,
        Evidence = Evidence(),
        Definition = new ArtifactDefinition
        {
            Key = key,
            Name = "AccountOpened",
            File = file
        }
    };

    ConceptValidationRuleFact Validation(string id, SubjectId concept, string file) => new()
    {
        Id = new FactId { Value = id },
        Subject = concept,
        Evidence = Evidence(),
        Definition = new ConceptValidationRuleDefinition
        {
            Concept = concept,
            RuleIdentity = "format",
            Kind = ConceptValidationRuleKind.NamedPredicate,
            Predicate = "Validate",
            ImplementationFile = file
        }
    };

    Evidence Evidence() => new() { Adapter = _adapter, Strength = EvidenceStrength.Exact };

    static string Projection(ResolvedApplicationGraph graph) => string.Join('|',
        graph.Artifacts.Single().Variants.Single().Definition.File,
        string.Join(',', graph.Artifacts.Single().Variants.Single().Files),
        graph.ConceptValidationRules.Single().Variants.Single().Definition.ImplementationFile,
        string.Join(',', graph.ConceptValidationRules.Single().Variants.Single().ImplementationFiles));
}
