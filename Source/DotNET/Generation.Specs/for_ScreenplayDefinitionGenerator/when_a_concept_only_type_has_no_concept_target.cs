// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_a_concept_only_type_has_no_concept_target : given.concept_only_type_uses
{
    GeneratedScreenplayDefinition _absent;
    GeneratedScreenplayDefinition _nonConcept;
    GeneratedScreenplayDefinition _unsupported;
    GeneratedScreenplayDefinition _yielded;

    void Because()
    {
        var command = OptedCommand(TypeUseShapeKind.Optional, TypeUseShapeKind.Collection, TypeUseShapeKind.Named);
        _absent = Generate(command);
        _nonConcept = Generate([.. command, NonConcept(), NonConcept(ArtifactKind.Event)]);
        _unsupported = Generate([.. command, .. Candidate("enum", false).Where(fact => fact is not ConceptRepresentationFact)]);
        _yielded = Generate([.. command, .. Candidate("enum", true), .. Candidate("winner", false, true)]);
    }

    [Fact] void should_keep_the_absent_target_owner() => _absent.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_bind_a_record_or_event() => Member(_nonConcept).Type.Subject.ShouldBeNull();
    [Fact] void should_keep_the_non_concept_target_owner() => _nonConcept.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_the_unsupported_target_owner() => _unsupported.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_the_yielded_target_owner() => _yielded.IsSuccess.ShouldBeTrue();
    [Fact] void should_preserve_the_printable_name_and_wrappers() => new[] { _absent, _nonConcept, _unsupported, _yielded }.All(result => result.Source.Contains("permissions Int[] optional", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_report_missing_target_errors() => new[] { _absent, _nonConcept, _unsupported, _yielded }.Any(result => result.Diagnostics.Any(MissingTarget)).ShouldBeFalse();
    [Fact] void should_not_invent_a_fallback_binding() => _nonConcept.AdapterRun.Derivation.Facts.Any(record => record.Fact is TypeUseBindingFact).ShouldBeFalse();
    [Fact] void should_preserve_the_observed_subject() => _absent.AdapterRun.Facts.Select(record => record.Fact).OfType<ArtifactMemberTypeUseFact>().Single().Definition.Type.ObservedTypeSubject.ShouldEqual(Target);
    [Fact] void should_preserve_the_unsupported_concept_warning() => _unsupported.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.MissingConceptRepresentation).ShouldBeTrue();
    [Fact] void should_preserve_the_yield_warning_once() => _yielded.Diagnostics.Count(IsWarning).ShouldEqual(1);
    [Fact] void should_keep_the_yield_warning_source() => _yielded.Diagnostics.Single(IsWarning).Source.Path.ShouldEqual("enum.cs");
    [Fact] void should_keep_the_yield_warning_outcome() => _yielded.Diagnostics.Single(IsWarning).Outcome.ShouldEqual(GenerationDiagnosticOutcome.Conflict);
    [Fact] void should_keep_the_yield_warning_links() => _yielded.Diagnostics.Single(IsWarning).Facts.ShouldContain(Candidate("enum", true).OfType<ArtifactDeclarationFact>().Single().Id);
    [Fact] void should_not_bind_to_the_winning_same_name() => Member(_yielded).Type.Subject.ShouldBeNull();
}
