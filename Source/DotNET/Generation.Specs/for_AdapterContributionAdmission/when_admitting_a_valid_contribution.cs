// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_admitting_a_valid_contribution : given.a_contribution
{
    AdapterContributionAdmissionResult _result = null!;

    void Because() => _result = Admit();

    [Fact] void should_admit_the_complete_contribution() => _result.IsAdmitted.ShouldBeTrue();
    [Fact] void should_not_report_admission_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_freeze_every_current_fact_family() => _result.Snapshot!.Facts.Select(fact => fact.GetType()).ShouldContainOnly(
        typeof(ArtifactFact),
        typeof(ArtifactPlacementFact),
        typeof(RelationshipFact),
        typeof(ConceptRepresentationFact),
        typeof(ConceptAttributeFact),
        typeof(ConceptValidationRuleFact),
        typeof(SpecificationScenarioFact),
        typeof(SpecificationStepFact),
        typeof(SpecificationValueFact));
    [Fact] void should_canonicalize_and_deduplicate_required_host_capabilities() => string.Join('|', _result.Snapshot!.Descriptor.RequiredHostCapabilities).ShouldEqual("AuthoredSource|SemanticAnalysis");
    [Fact] void should_canonicalize_and_deduplicate_emitted_fact_capabilities() => _result.Snapshot!.Descriptor.EmittedFactCapabilities.ShouldEqual(Enum.GetValues<GenerationFactCapability>().Where(value => value != GenerationFactCapability.Unknown));
}
