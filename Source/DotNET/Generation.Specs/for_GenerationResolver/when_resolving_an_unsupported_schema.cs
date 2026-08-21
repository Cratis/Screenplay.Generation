// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_resolving_an_unsupported_schema : given.facts
{
    ResolvedApplicationGraph _result = null!;

    void Because() => _result = new GenerationResolver().Resolve(
    [
        new AdapterContribution
        {
            Adapter = FirstAdapter,
            SchemaVersion = "2.0",
            Facts = [Fact("event", FirstAdapter)]
        }
    ]);

    [Fact] void should_report_the_schema() => _result.Diagnostics.Single().Code.ShouldEqual(GenerationDiagnosticCodes.UnsupportedSchema);
    [Fact] void should_fail_resolution() => _result.Diagnostics.Single().Severity.ShouldEqual(GenerationDiagnosticSeverity.Error);
}
