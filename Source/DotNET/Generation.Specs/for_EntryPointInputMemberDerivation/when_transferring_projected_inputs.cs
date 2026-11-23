// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_EntryPointInputMemberDerivation;

public class when_transferring_projected_inputs : given.omitted_input_members
{
    GeneratedScreenplayDefinition _absent = null!;
    GeneratedScreenplayDefinition _present = null!;
    GeneratedScreenplayDefinition _reverse = null!;
    GeneratedScreenplayDefinition _conflict = null!;
    GeneratedScreenplayDefinition _reverseConflict = null!;
    static readonly SubjectId Container = new() { Value = "library://Container" };

    void Because()
    {
        var facts = Facts();
        _absent = Generate(facts);
        _reverse = Generate(facts.Reverse());
        var conflicting = ConflictingFacts(facts);
        _conflict = Generate(conflicting);
        _reverseConflict = Generate(conflicting.Reverse());
        _present = Generate([.. facts,
            new ArtifactDeclarationFact
            {
                Id = new() { Value = "library:container-concept" }, Subject = Container, Evidence = View().Evidence,
                Definition = new() { Artifact = new() { Subject = Container, Kind = ArtifactKind.Concept }, Name = "ContainerConcept" }
            },
            new ConceptRepresentationFact
            {
                Id = new() { Value = "library:container-representation" }, Subject = Container, Evidence = View().Evidence,
                Definition = new() { Concept = Container, Kind = ConceptRepresentationKind.Primitive, Primitive = GenerationPrimitiveKind.Text }
            }]);
    }

    [Fact] void should_lower_the_projected_input() => _absent.IsSuccess.ShouldBeTrue();
    [Fact] void should_preserve_the_one_layer_flags() => _absent.Source.ShouldContain("filter Container[] optional");
    [Fact] void should_bind_the_selected_subject() => _present.Source.ShouldContain("filter ContainerConcept[] optional");
    [Fact] void should_keep_the_exact_declaration_input() => _absent.AdapterRun!.Facts.Select(record => record.Fact).OfType<DeclarationInputMemberTypeUseFact>().Single().Definition.Type.Shape.Count.ShouldEqual(4);
    [Fact] void should_keep_the_exact_terminal_subject() => _absent.AdapterRun!.Facts.Select(record => record.Fact).OfType<DeclarationInputMemberTypeUseFact>().Single().Definition.Type.ObservedTypeSubject.ShouldEqual(FlagSubject);
    [Fact] void should_transfer_the_exact_terminal_subject() => _absent.AdapterRun!.Derivation.Facts.Select(record => record.Fact).OfType<ArtifactMemberTypeUseFact>().Single().Definition.Type.ObservedTypeSubject.ShouldEqual(FlagSubject);
    [Fact] void should_transfer_the_exact_wrapper_sequence() => _absent.AdapterRun!.Derivation.Facts.Select(record => record.Fact).OfType<ArtifactMemberTypeUseFact>().Single().Definition.Type.Shape.Count.ShouldEqual(4);
    [Fact] void should_transfer_complete_node_evidence() => _absent.AdapterRun!.Derivation.Facts.Select(record => record.Fact).OfType<ArtifactMemberTypeUseFact>().Single().Definition.Type.Nodes![2].Subject.ShouldEqual(Container);
    [Fact] void should_transfer_a_separate_printable_view() => _absent.AdapterRun!.Derivation.Facts.Select(record => record.Fact).OfType<TypeUseCompatibilityProjectionFact>().Single().Definition.Type.ObservedTypeSubject.ShouldEqual(Container);
    [Fact] void should_link_the_transferred_view_to_the_exact_output() => _absent.AdapterRun!.Derivation.Facts.Select(record => record.Fact).OfType<TypeUseCompatibilityProjectionFact>().Single().Definition.TypeUse.ShouldEqual(_absent.AdapterRun!.Derivation.Facts.Select(record => record.Fact).OfType<ArtifactMemberTypeUseFact>().Single().Id);
    [Fact] void should_include_the_projection_in_transferred_lineage() => _absent.AdapterRun!.Derivation.Facts.Single(record => record.Fact is ArtifactMemberTypeUseFact).Lineage.Inputs.ShouldContain(View().Id);
    [Fact] void should_include_the_projection_in_binding_lineage() => _present.AdapterRun!.Derivation.Facts.Single(record => record.Fact is TypeUseBindingFact).Lineage.Inputs.ShouldContain(View().Id);
    [Fact] void should_keep_order_independent_source() => _reverse.Source.ShouldEqual(_absent.Source);
    [Fact] void should_keep_order_independent_diagnostics() => Projection(_reverse.Diagnostics).ShouldEqual(Projection(_absent.Diagnostics));
    [Fact] void should_keep_order_independent_lineage() => Projection(_reverse.AdapterRun!.Derivation).ShouldEqual(Projection(_absent.AdapterRun!.Derivation));

    [Fact] void should_reject_conflicting_exact_inputs_despite_equal_views() => _conflict.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_transfer_a_winning_projection_for_conflicting_exact_inputs() => _conflict.AdapterRun!.Derivation.Facts.Any(record => record.Fact is TypeUseCompatibilityProjectionFact).ShouldBeFalse();
    [Fact] void should_keep_conflict_diagnostics_order_independent() => Projection(_reverseConflict.Diagnostics).ShouldEqual(Projection(_conflict.Diagnostics));

    static GenerationFact[] ConflictingFacts(GenerationFact[] facts)
    {
        var original = facts.OfType<DeclarationInputMemberTypeUseFact>().Single();
        var nodes = original.Definition.Type.Nodes!;
        var alternate = original with
        {
            Id = new() { Value = "library:alternate-input" },
            Definition = original.Definition with
            {
                Type = original.Definition.Type with
                {
                    Shape = [TypeUseShapeKind.Optional, TypeUseShapeKind.Collection, TypeUseShapeKind.Optional, TypeUseShapeKind.Collection, TypeUseShapeKind.Named],
                    Nodes = [nodes[0], nodes[1], nodes[2], nodes[2], nodes[3]]
                }
            }
        };
        return [.. facts, alternate, View() with { Id = new() { Value = "library:alternate-view" }, Definition = View().Definition with { TypeUse = alternate.Id } }];
    }

    static string Projection(object value) => System.Text.Json.JsonSerializer.Serialize(value);

    static GenerationFact[] Facts()
    {
        var original = FlagType(TypeUseShapeKind.Optional, TypeUseShapeKind.Collection, TypeUseShapeKind.Collection, TypeUseShapeKind.Named);
        var observed = original with
        {
            Definition = original.Definition with
            {
                Type = original.Definition.Type with
                {
                    Nodes = [new() { Name = "List" }, new() { Name = "List" }, new() { Name = "Container", Subject = Container }, new() { Name = "Int", Subject = FlagSubject }]
                }
            }
        };
        return [.. OmittedFacts().Where(fact => fact is not DeclarationInputMemberTypeUseFact and not DeclarationInputMemberTargetOmittedFact), observed, View(), new TypeUseBindingPolicyFact
        {
            Id = new() { Value = "library:projected-input-policy" }, Subject = observed.Subject, Evidence = observed.Evidence,
            Definition = new() { TypeUse = observed.Id, Policy = TypeUseBindingPolicyKind.ConceptOnlyWithUnboundFallback }
        }];
    }

    static TypeUseCompatibilityProjectionFact View() => new()
    {
        Id = new() { Value = "library:input-view" }, Subject = Declaration, Evidence = FlagType().Evidence,
        Definition = new()
        {
            TypeUse = FlagType().Id, Profile = TypeUseCompatibilityProfile.OneLayerCollection, Version = 1, SourceNodeIndex = 2,
            Type = new() { Name = "Container", ObservedTypeSubject = Container, Shape = [TypeUseShapeKind.Optional, TypeUseShapeKind.Collection, TypeUseShapeKind.Named] }
        }
    };
}
