// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.Vogen.for_VogenConceptScreenplayAdapter;

public class when_analyzing_non_idiomatic_equivalent_source : given.a_non_idiomatic_vogen_source
{
    AdapterContribution _contribution = null!;
    ArtifactFact _concept = null!;
    ConceptRepresentationFact _representation = null!;
    ConceptValidationRuleFact _validation = null!;

    void Because()
    {
        _contribution = Analyze(SourceProject);
        _concept = ConceptNamed(_contribution, "CustomerCode");
        _representation = RepresentationFor(_contribution, _concept);
        _validation = _contribution.Facts.OfType<ConceptValidationRuleFact>().Single();
    }

    [Fact] void should_discover_the_concept_by_symbol_identity() => _concept.Definition.Key.Kind.ShouldEqual(ArtifactKind.Concept);
    [Fact] void should_preserve_the_fully_qualified_backing_type() => _representation.Definition.Primitive.ShouldEqual(GenerationPrimitiveKind.Text);
    [Fact] void should_discover_the_exact_validation_hook() => _validation.Definition.Predicate.ShouldEqual("Validate");
    [Fact] void should_preserve_the_constant_message_through_redundant_casts() => _validation.Definition.Message.ShouldEqual("Customer code is required");
    [Fact] void should_report_no_semantic_loss() => _contribution.Diagnostics.ShouldBeEmpty();
}
