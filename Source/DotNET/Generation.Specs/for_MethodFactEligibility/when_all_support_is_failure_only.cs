// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_MethodFactEligibility;

public class when_all_support_is_failure_only : given.method_facts
{
    GenerationDerivationSnapshot _result;
    GeneratedScreenplayDefinition _generated;

    void Because()
    {
        _result = Derive([.. CompleteFacts(), Support(), Classification(MethodExecutionKind.FailureOnly)]);
        _generated = new ScreenplayDefinitionGenerator().Generate(Contributions(Support(), Classification(MethodExecutionKind.FailureOnly)), new ScreenplayGenerationOptions { Domain = "Application" });
    }

    [Fact] void should_omit_the_behavior_before_joining() => _result.Facts.ShouldBeEmpty();
    [Fact] void should_preserve_the_frontend_warning_exactly() => _result.Diagnostics.Single().ShouldEqual(Reason());
    [Fact] void should_keep_the_structural_artifacts() => _generated.Graph.Artifacts.Count.ShouldEqual(2);
    [Fact] void should_not_resolve_the_excluded_relationship() => _generated.Graph.Relationships.ShouldBeEmpty();
    [Fact] void should_record_the_eligibility_rule() => _result.Rules.Any(rule => rule.Rule.Id == "cratis.screenplay.method-fact-eligibility").ShouldBeTrue();
}
