// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_a_projection_method_is_failure_only : given.diagnostic_projection
{
    GenerationDerivationSnapshot _result;
    void Because()
    {
        var classification = Classification(MethodExecutionKind.FailureOnly);
        classification = classification with { Definition = classification.Definition with { OmissionReason = Payload() with { Code = "FAILURE", Facts = [] } } };
        _result = Derive(Observation(), Intent(), Support(), classification, Entry());
    }
    [Fact] void should_not_project_the_ineligible_payload() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == Payload().Code).ShouldBeFalse();
    [Fact] void should_preserve_existing_eligibility_diagnostics() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == "FAILURE").ShouldBeTrue();
    [Fact] void should_not_register_an_empty_projection_rule() => _result.Rules.Any(rule => rule.Rule.Id == "cratis.screenplay.method-diagnostic-projection").ShouldBeFalse();
}
