// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_inputs_are_permuted : given.a_derivation
{
    GenerationDerivationSnapshot _forward = null!;
    GenerationDerivationSnapshot _reverse = null!;

    void Because()
    {
        var facts = new GenerationFact[]
        {
            TypeUse("customerCode", ConceptSubject, "type-use"),
            Concept(ConceptSubject, "customer-code"),
            MemberDeclaration("customerCode", 0, "member"),
            CommandDeclaration()
        };
        _forward = Derive(facts);
        _reverse = Derive([.. facts.AsEnumerable().Reverse()]);
    }

    [Fact] void should_produce_recursively_identical_facts_lineage_and_diagnostics() => Projection(_reverse).ShouldEqual(Projection(_forward));
}
