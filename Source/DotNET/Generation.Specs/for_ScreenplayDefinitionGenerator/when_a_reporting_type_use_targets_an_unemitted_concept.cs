// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_a_reporting_type_use_targets_an_unemitted_concept : given.concept_only_type_uses
{
    GeneratedScreenplayDefinition _legacy;
    GeneratedScreenplayDefinition _reporting;
    GeneratedScreenplayDefinition _silent;
    GeneratedScreenplayDefinition _emitted;
    GeneratedScreenplayDefinition _absent;
    GeneratedScreenplayDefinition _nonConcept;
    GeneratedScreenplayDefinition _yielded;
    GeneratedScreenplayDefinition _mixed;
    GeneratedScreenplayDefinition _mixedReverse;

    void Because()
    {
        var unsupported = Candidate("enum", false).Where(fact => fact is not ConceptRepresentationFact);
        var reporting = ReportingCommand(TypeUseShapeKind.Optional, TypeUseShapeKind.Collection, TypeUseShapeKind.Named);
        _legacy = Generate([.. Command(TypeUseShapeKind.Optional, TypeUseShapeKind.Collection, TypeUseShapeKind.Named), .. unsupported]);
        _reporting = Generate([.. reporting, .. unsupported]);
        _silent = Generate([.. OptedCommand(TypeUseShapeKind.Optional, TypeUseShapeKind.Collection, TypeUseShapeKind.Named), .. unsupported]);
        _emitted = Generate([.. reporting, .. Candidate("enum", false)]);
        _absent = Generate(reporting);
        _nonConcept = Generate([.. reporting, NonConcept(), NonConcept(ArtifactKind.Event)]);
        _yielded = Generate([.. reporting, .. Candidate("enum", true), .. Candidate("winner", false, true)]);
        var command = Command(TypeUseShapeKind.Optional, TypeUseShapeKind.Collection, TypeUseShapeKind.Named);
        var typeUse = command.OfType<ArtifactMemberTypeUseFact>().Single();
        var other = Adapter with { Id = $"{Adapter.Id}-other" };
        var otherUse = typeUse with { Id = new() { Value = $"{other.Id}:type-use" }, Evidence = typeUse.Evidence with { Adapter = other } };
        var mixed = new[] { Completed(Adapter, [.. command, Policy(typeUse, TypeUseBindingPolicyKind.ConceptOnlyWithUnboundFallbackAndMissingConceptDiagnostic), .. unsupported]), Completed(other, [otherUse, Policy(otherUse)]) };
        _mixed = Generator.Generate(Snapshot(mixed), new() { Domain = "Ordering" });
        _mixedReverse = Generator.Generate(Snapshot([.. mixed.Reverse()]), new() { Domain = "Ordering" });
    }

    [Fact] void should_report_exactly_the_legacy_diagnostic() => Projection(MissingConcept(_reporting)).ShouldEqual(Projection(MissingConcept(_legacy)));
    [Fact] void should_report_once() => MissingConcept(_reporting).Length.ShouldEqual(1);
    [Fact] void should_use_the_legacy_message() => MissingConcept(_reporting)[0].Message.ShouldEqual("Type reference 'Int' targets concept 'dotnet://Ordering/Permissions', which could not be emitted");
    [Fact] void should_not_attach_a_source() => MissingConcept(_reporting)[0].Source.ShouldBeNull();
    [Fact] void should_warn() => MissingConcept(_reporting)[0].Severity.ShouldEqual(GenerationDiagnosticSeverity.Warning);
    [Fact] void should_keep_the_unsupported_concept_warning() => _reporting.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.MissingConceptRepresentation).ShouldBeTrue();
    [Fact] void should_keep_the_owner_with_its_printable_fallback() => _reporting.Source.ShouldContain("permissions Int[] optional");
    [Fact] void should_omit_the_owner_under_the_legacy_policy() => _legacy.Source.ShouldNotContain("permissions");
    [Fact] void should_stay_silent_for_the_existing_policy() => MissingConcept(_silent).Length.ShouldEqual(0);
    [Fact] void should_not_report_an_emitted_concept() => MissingConcept(_emitted).Length.ShouldEqual(0);
    [Fact] void should_not_report_an_undeclared_type() => MissingConcept(_absent).Length.ShouldEqual(0);
    [Fact] void should_not_report_a_non_concept() => MissingConcept(_nonConcept).Length.ShouldEqual(0);
    [Fact] void should_not_report_a_yielded_concept() => MissingConcept(_yielded).Length.ShouldEqual(0);
    [Fact] void should_keep_the_yield_warning_once() => _yielded.Diagnostics.Count(IsWarning).ShouldEqual(1);
    [Fact] void should_not_report_a_mixed_policy_member() => MissingConcept(_mixed).Length.ShouldEqual(0);
    [Fact] void should_keep_mixed_policy_results_order_independent() => Projection(_mixedReverse.Diagnostics).ShouldEqual(Projection(_mixed.Diagnostics));
}
