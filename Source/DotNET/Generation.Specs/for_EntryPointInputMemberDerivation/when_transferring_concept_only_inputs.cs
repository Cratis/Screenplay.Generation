// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_EntryPointInputMemberDerivation;

public class when_transferring_concept_only_inputs : given.omitted_input_members
{
    GeneratedScreenplayDefinition _absent;
    GeneratedScreenplayDefinition _present;
    GeneratedScreenplayDefinition _nonConcept;
    GeneratedScreenplayDefinition _unsupported;
    GeneratedScreenplayDefinition _reverse;
    GeneratedScreenplayDefinition _yielded;
    GeneratedScreenplayDefinition _yieldReverse;

    void Because()
    {
        var facts = PolicyFacts();
        _absent = Generate(facts);
        _present = Generate([.. facts, Target(), Representation()]);
        _nonConcept = Generate([.. facts, Target(ArtifactKind.Event)]);
        _unsupported = Generate([.. facts, Target()]);
        _reverse = Generate(facts.Reverse());
        var yielded = YieldedFacts();
        _yielded = Generate(yielded);
        _yieldReverse = Generate(yielded.Reverse());
    }

    [Fact] void should_keep_all_query_endpoints() => new[] { _absent, _present, _nonConcept, _unsupported, _yielded }.All(result => result.IsSuccess).ShouldBeTrue();
    [Fact] void should_preserve_the_plain_type_and_wrappers() => _absent.Source.ShouldContain("filter Int[] optional");
    [Fact] void should_bind_a_supported_concept() => _present.Source.ShouldContain("filter Permissions[] optional");
    [Fact] void should_not_bind_a_non_concept() => _nonConcept.Source.ShouldContain("filter Int[] optional");
    [Fact] void should_fall_back_after_concept_lowering() => _unsupported.Source.ShouldContain("filter Int[] optional");
    [Fact] void should_keep_the_lowering_warning() => _unsupported.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.MissingConceptRepresentation).ShouldBeTrue();
    [Fact] void should_preserve_the_exact_observed_subject() => _absent.AdapterRun.Derivation.Facts.Select(record => record.Fact).OfType<ArtifactMemberTypeUseFact>().Single().Definition.Type.ObservedTypeSubject.ShouldEqual(FlagSubject);
    [Fact] void should_synthesize_an_endpoint_owned_policy() => OutputPolicy().Subject.ShouldEqual(Transfer().Definition.Artifact.Subject);
    [Fact] void should_link_the_synthesized_policy_to_the_output_observation() => OutputPolicy().Definition.TypeUse.ShouldEqual(_absent.AdapterRun.Derivation.Facts.Select(record => record.Fact).OfType<ArtifactMemberTypeUseFact>().Single().Id);
    [Fact] void should_include_the_original_policy_in_transferred_lineage() => _absent.AdapterRun.Derivation.Facts.Single(record => record.Fact is TypeUseBindingPolicyFact).Lineage.Inputs.ShouldContain(Policy().Id);
    [Fact] void should_not_report_missing_target_errors() => new[] { _absent, _present, _nonConcept, _unsupported, _yielded }.Any(result => result.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.MissingTypeUseTarget || diagnostic.Code == GenerationDiagnosticCodes.IncompleteArtifactMember || diagnostic.Code == GenerationDiagnosticCodes.MissingConceptReference)).ShouldBeFalse();
    [Fact] void should_keep_order_independent_source() => _reverse.Source.ShouldEqual(_absent.Source);
    [Fact] void should_keep_order_independent_lineage() => Projection(_reverse.AdapterRun.Derivation).ShouldEqual(Projection(_absent.AdapterRun.Derivation));
    [Fact] void should_keep_order_independent_diagnostics() => Projection(_reverse.Diagnostics).ShouldEqual(Projection(_absent.Diagnostics));
    [Fact] void should_fall_back_after_a_concept_yields() => _yielded.Source.ShouldContain("filter Int[] optional");
    [Fact] void should_keep_the_yield_warning_once() => _yielded.Diagnostics.Count(diagnostic => diagnostic.Code == "library.ConceptYield").ShouldEqual(1);
    [Fact] void should_keep_the_yield_warning_source() => _yielded.Diagnostics.Single(diagnostic => diagnostic.Code == "library.ConceptYield").Source.ShouldEqual(FlagType().Evidence.Source);
    [Fact] void should_keep_yielded_order_independent_source() => _yieldReverse.Source.ShouldEqual(_yielded.Source);
    [Fact] void should_keep_yielded_order_independent_diagnostics() => Projection(_yieldReverse.Diagnostics).ShouldEqual(Projection(_yielded.Diagnostics));
    [Fact] void should_keep_yielded_order_independent_lineage() => Projection(_yieldReverse.AdapterRun.Derivation).ShouldEqual(Projection(_yielded.AdapterRun.Derivation));

    TypeUseBindingPolicyFact OutputPolicy() => _absent.AdapterRun.Derivation.Facts.Select(record => record.Fact).OfType<TypeUseBindingPolicyFact>().Single();
    static GenerationFact[] PolicyFacts() => [.. OmittedFacts(TypeUseShapeKind.Optional, TypeUseShapeKind.Collection, TypeUseShapeKind.Named).Where(fact => fact is not DeclarationInputMemberTargetOmittedFact), Policy()];
    static TypeUseBindingPolicyFact Policy() => new()
    {
        Id = new() { Value = "library:input-concept-policy" }, Subject = Declaration, Evidence = FlagType().Evidence,
        Definition = new() { TypeUse = FlagType().Id, Policy = TypeUseBindingPolicyKind.ConceptOnlyWithUnboundFallback }
    };

    static ArtifactDeclarationFact Target(ArtifactKind kind = ArtifactKind.Concept) => new()
    {
        Id = new() { Value = "library:permissions" }, Subject = FlagSubject, Evidence = FlagType().Evidence,
        Definition = new() { Artifact = new() { Subject = FlagSubject, Kind = kind }, Name = "Permissions" }
    };

    static GenerationFact[] YieldedFacts()
    {
        var subject = new SubjectId { Value = "library://OtherPermissions" };
        var winner = Target() with
        {
            Id = new() { Value = "library:winning-permissions" }, Subject = subject,
            Definition = Target().Definition with { Artifact = Target().Definition.Artifact with { Subject = subject } }
        };
        var yielding = Target() with
        {
            Definition = Target().Definition with
            {
                NameConflictYield = new()
                {
                    Diagnostic = new()
                    {
                        Code = "library.ConceptYield", Severity = GenerationDiagnosticSeverity.Warning, Outcome = GenerationDiagnosticOutcome.Unsupported,
                        Message = "The enum yields to the authored concept", Subject = FlagSubject, Source = Target().Evidence.Source, Facts = [Target().Id]
                    }
                }
            }
        };
        return [.. PolicyFacts(), yielding, Representation(), winner,
            Representation() with { Id = new() { Value = "library:winning-representation" }, Subject = subject, Definition = Representation().Definition with { Concept = subject } }];
    }

    static ConceptRepresentationFact Representation() => new()
    {
        Id = new() { Value = "library:permissions-representation" }, Subject = FlagSubject, Evidence = FlagType().Evidence,
        Definition = new() { Concept = FlagSubject, Kind = ConceptRepresentationKind.Primitive, Primitive = GenerationPrimitiveKind.WholeNumber }
    };
}
