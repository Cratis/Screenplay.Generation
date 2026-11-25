// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_a_backend_scope_has_only_reference_evidence : given.backend_scope_diagnostic_sources
{
    GeneratedScreenplayDefinition[] _results;

    void Because()
    {
        var references = Members(First);
        _results = GenerateOrders([references[0]], references[1..]);
    }

    [Fact] void should_not_invent_declaration_evidence() => _results.All(result => result.Graph.BackendScopes.Single().Evidence.Count == 0).ShouldBeTrue();
    [Fact] void should_keep_the_warning_at_the_canonical_reference_source_in_both_orders() => _results.All(result => ScopeWarnings(result).Single().Source == At(2).Source).ShouldBeTrue();
    [Fact] void should_keep_the_missing_scope_diagnostic_at_the_same_fallback_source() => _results.All(result => result.Diagnostics.Single(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.UnknownBackendScope).Source == At(2).Source).ShouldBeTrue();
    [Fact] void should_keep_diagnostics_identical_in_both_orders() => Describe(_results[1].Diagnostics).ShouldEqual(Describe(_results[0].Diagnostics));
}
