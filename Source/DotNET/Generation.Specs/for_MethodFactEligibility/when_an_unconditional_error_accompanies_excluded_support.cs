// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_MethodFactEligibility;

public class when_an_unconditional_error_accompanies_excluded_support : given.method_facts
{
    GeneratedScreenplayDefinition _result;

    void Because()
    {
        var contributions = Contributions(Support(), Classification(MethodExecutionKind.FailureOnly));
        contributions[0] = contributions[0] with { Diagnostics = [Reason() with { Code = "HOST_ERROR", Severity = GenerationDiagnosticSeverity.Error }] };
        _result = new ScreenplayDefinitionGenerator().Generate(contributions, new ScreenplayGenerationOptions { Domain = "Application" });
    }

    [Fact] void should_not_suppress_the_host_error() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == "HOST_ERROR" && diagnostic.Severity == GenerationDiagnosticSeverity.Error).ShouldBeTrue();
}
