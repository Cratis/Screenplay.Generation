// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_a_jagged_view_hides_an_observed_concept : given.jagged_array_members
{
    GenerationFact[] _facts = null!;
    ArtifactFact _concept = null!;
    GeneratedScreenplayDefinition[] _results = null!;

    void Establish()
    {
        var part = new SubjectId { Value = "dotnet://Ordering/Concepts.Part" };
        _facts = MemberFacts(true, ArrayOfArrays, name: "Part", observed: part);
        _concept = new ArtifactFact
        {
            Id = new FactId { Value = "critter-stack:part" },
            Subject = part,
            Evidence = _facts[0].Evidence,
            Definition = new() { Key = new() { Subject = part, Kind = ArtifactKind.Concept }, Name = "Part" }
        };
    }

    void Because() => _results = [.. GenerateBoth(_facts), .. GenerateBoth([.. _facts, _concept])];

    [Fact] void should_not_bind_the_hidden_element_even_when_the_concept_exists() => _results.Skip(2)
        .All(result => result.Graph.Artifacts.Any(artifact => artifact.Key == _concept.Definition.Key) && Type(result).Subject is null).ShouldBeTrue();

    [Fact] void should_not_derive_an_element_binding() => _results.Where(result => result.AdapterRun is not null)
        .SelectMany(result => result.AdapterRun!.Derivation.Facts).Any(record => record.Fact is TypeUseBindingFact).ShouldBeFalse();

    [Fact] void should_not_report_a_missing_concept_with_or_without_the_declaration() => _results
        .Any(result => HasCode(result, GenerationDiagnosticCodes.MissingConceptReference)).ShouldBeFalse();

    [Fact] void should_leave_the_element_unnamed_on_both_ingestion_paths() => _results.All(result => Type(result).Name.Length == 0).ShouldBeTrue();

    static TypeReferenceDefinition Type(GeneratedScreenplayDefinition result) => result.Graph.Artifacts
        .Single(artifact => artifact.Key.Kind == ArtifactKind.Event).Variants.Single().Definition.Properties.Single().Type;
}
