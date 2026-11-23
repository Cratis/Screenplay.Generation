// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_a_diagnostic_has_no_entry_point : given.diagnostic_projection
{
    GenerationDiagnostic _diagnostic;
    void Because() => _diagnostic = Projected(Derive(Observation(), Intent(), Support(), Classification()));
    [Fact] void should_report_a_located_unknown_without_leaking_the_payload() => (_diagnostic.Code, _diagnostic.Outcome, _diagnostic.Source).ShouldEqual((GenerationDiagnosticCodes.MissingDiagnosticEntryPoint, (GenerationDiagnosticOutcome?)GenerationDiagnosticOutcome.Unknown, Invocation));
}
