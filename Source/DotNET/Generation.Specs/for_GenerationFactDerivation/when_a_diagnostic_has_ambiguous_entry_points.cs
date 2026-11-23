// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_a_diagnostic_has_ambiguous_entry_points : given.diagnostic_projection
{
    GenerationDiagnostic _diagnostic;
    void Because() => _diagnostic = Projected(Derive([.. Inputs(), Entry("other", Command with { Subject = ConceptSubject })]));
    [Fact] void should_report_one_located_conflict() => (_diagnostic.Code, _diagnostic.Outcome, _diagnostic.Source).ShouldEqual((GenerationDiagnosticCodes.AmbiguousDiagnosticEntryPoint, (GenerationDiagnosticOutcome?)GenerationDiagnosticOutcome.Conflict, Invocation));
    [Fact] void should_retain_both_mappings() => _diagnostic.Lineage!.Inputs.Select(id => id.Value).ShouldContain("application:other");
}
