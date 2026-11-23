// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator.when_binding_concept_only_type_uses;

public class with_an_explicit_concept_binding_to_another_subject : given.concept_only_type_uses
{
    GenerationFact[] _facts;
    TypeUseBindingFact _binding;
    ArtifactMemberTypeUseFact _observation;
    GeneratedScreenplayDefinition _result;

    void Establish()
    {
        var own = OptedCommand();
        _observation = own.OfType<ArtifactMemberTypeUseFact>().Single();
        var otherConcept = Candidate("other", false, true);
        _binding = new()
        {
            Id = Id("explicit-binding"), Subject = _observation.Subject, Evidence = _observation.Evidence,
            Definition = new()
            {
                Member = _observation.Definition.Member,
                Target = otherConcept.OfType<ArtifactDeclarationFact>().Single().Definition.Artifact
            }
        };
        _facts = [.. own, .. otherConcept, _binding];
    }

    void Because() => _result = Generate(_facts);

    [Fact] void should_fail_generation() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_a_conflicting_member() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.ConflictingArtifactMember).Outcome.ShouldEqual(GenerationDiagnosticOutcome.Conflict);
    [Fact] void should_link_the_explicit_binding() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.ConflictingArtifactMember).Facts.ShouldContain(_binding.Id);
    [Fact] void should_link_the_observed_subject() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.ConflictingArtifactMember).Facts.ShouldContain(_observation.Id);
    [Fact] void should_locate_the_conflict() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.ConflictingArtifactMember).Source.ShouldEqual(_observation.Evidence.Source);
    [Fact] void should_not_emit_the_contradicted_owner() => _result.Source.ShouldNotContain("command SetPermissions");
}
