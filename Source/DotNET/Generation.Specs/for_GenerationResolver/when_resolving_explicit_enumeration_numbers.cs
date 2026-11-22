// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_resolving_explicit_enumeration_numbers : given.facts
{
    ResolvedApplicationGraph _result = null!;
    ResolvedApplicationGraph _reversed = null!;

    void Because()
    {
        var first = Contribution(FirstAdapter, Representation("first", FirstAdapter, [0, 1]));
        var same = Contribution(SecondAdapter, Representation("same", SecondAdapter, [0, 1]));
        var different = Contribution(SecondAdapter, Representation("different", SecondAdapter, [0, 7]));
        var legacy = Contribution(SecondAdapter, Representation("legacy", SecondAdapter, null));
        _result = new GenerationResolver().Resolve([first, same, different, legacy]);
        _reversed = new GenerationResolver().Resolve([legacy, different, same, first]);
    }

    [Fact] void should_merge_equal_numbers_but_distinguish_different_and_implicit_numbers() => _result.ConceptRepresentations.Single().Variants.Count.ShouldEqual(3);
    [Fact] void should_retain_both_adapters_for_equal_numbers() => _result.ConceptRepresentations.Single().Variants.Single(variant => variant.Definition.EnumerationNumbers?[^1] == 1).Evidence.Count.ShouldEqual(2);
    [Fact] void should_resolve_in_the_same_order() => _reversed.ConceptRepresentations.Single().Variants.Select(variant => string.Join(',', variant.Definition.EnumerationNumbers ?? [])).ShouldEqual(_result.ConceptRepresentations.Single().Variants.Select(variant => string.Join(',', variant.Definition.EnumerationNumbers ?? [])));

    static ConceptRepresentationFact Representation(string id, AdapterIdentity adapter, IReadOnlyList<int>? numbers)
    {
        var subject = new SubjectId { Value = "dotnet://Banking/Concepts.Status" };
        return new ConceptRepresentationFact
        {
            Id = new FactId { Value = id },
            Subject = subject,
            Evidence = new Evidence { Adapter = adapter, Strength = EvidenceStrength.Exact },
            Definition = new ConceptRepresentationDefinition
            {
                Concept = subject,
                Kind = ConceptRepresentationKind.Enumeration,
                EnumerationValues = ["Open", "Closed"],
                EnumerationNumbers = numbers
            }
        };
    }
}
