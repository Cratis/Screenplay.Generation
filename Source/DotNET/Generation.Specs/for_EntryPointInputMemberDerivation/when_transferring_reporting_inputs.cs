// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_EntryPointInputMemberDerivation;

public class when_transferring_reporting_inputs : given.omitted_input_members
{
    GeneratedScreenplayDefinition _reporting;
    GeneratedScreenplayDefinition _reverse;
    GeneratedScreenplayDefinition _silent;
    GeneratedScreenplayDefinition _absent;

    void Because()
    {
        var reporting = Facts(TypeUseBindingPolicyKind.ConceptOnlyWithUnboundFallbackAndMissingConceptDiagnostic);
        _reporting = Generate([.. reporting, Target()]);
        _reverse = Generate(reporting.Append(Target()).Reverse());
        _silent = Generate([.. Facts(TypeUseBindingPolicyKind.ConceptOnlyWithUnboundFallback), Target()]);
        _absent = Generate(reporting);
    }

    [Fact] void should_keep_the_endpoint() => _reporting.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_the_printable_fallback() => _reporting.Source.ShouldContain("filter Int[] optional");
    [Fact] void should_report_the_missing_concept_once() => MissingConcept(_reporting).Length.ShouldEqual(1);
    [Fact] void should_use_the_legacy_message() => MissingConcept(_reporting)[0].Message.ShouldEqual("Type reference 'Int' targets concept 'dotnet://Library/Permissions', which could not be emitted");
    [Fact] void should_not_attach_a_source() => MissingConcept(_reporting)[0].Source.ShouldBeNull();
    [Fact] void should_preserve_the_reporting_policy() => OutputPolicy(_reporting).Definition.Policy.ShouldEqual(TypeUseBindingPolicyKind.ConceptOnlyWithUnboundFallbackAndMissingConceptDiagnostic);
    [Fact] void should_stay_silent_for_the_existing_policy() => MissingConcept(_silent).Length.ShouldEqual(0);
    [Fact] void should_keep_the_existing_policy_kind() => OutputPolicy(_silent).Definition.Policy.ShouldEqual(TypeUseBindingPolicyKind.ConceptOnlyWithUnboundFallback);
    [Fact] void should_not_report_an_undeclared_type() => MissingConcept(_absent).Length.ShouldEqual(0);
    [Fact] void should_be_deterministic() => Projection(_reverse.Diagnostics).ShouldEqual(Projection(_reporting.Diagnostics));

    static GenerationDiagnostic[] MissingConcept(GeneratedScreenplayDefinition result) =>
        [.. result.Diagnostics.Where(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.MissingConceptReference)];

    static TypeUseBindingPolicyFact OutputPolicy(GeneratedScreenplayDefinition result) =>
        result.AdapterRun.Derivation.Facts.Select(record => record.Fact).OfType<TypeUseBindingPolicyFact>().Single();

    static GenerationFact[] Facts(TypeUseBindingPolicyKind kind) =>
    [
        .. OmittedFacts(TypeUseShapeKind.Optional, TypeUseShapeKind.Collection, TypeUseShapeKind.Named).Where(fact => fact is not DeclarationInputMemberTargetOmittedFact),
        new TypeUseBindingPolicyFact
        {
            Id = new() { Value = "library:input-concept-policy" }, Subject = Declaration, Evidence = FlagType().Evidence,
            Definition = new() { TypeUse = FlagType().Id, Policy = kind }
        }
    ];

    static ArtifactDeclarationFact Target() => new()
    {
        Id = new() { Value = "library:permissions" }, Subject = FlagSubject, Evidence = FlagType().Evidence,
        Definition = new() { Artifact = new() { Subject = FlagSubject, Kind = ArtifactKind.Concept }, Name = "Permissions" }
    };
}
