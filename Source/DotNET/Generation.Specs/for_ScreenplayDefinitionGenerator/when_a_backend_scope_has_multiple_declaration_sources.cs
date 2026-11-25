// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_a_backend_scope_has_multiple_declaration_sources : given.backend_scope_diagnostic_sources
{
    GeneratedScreenplayDefinition[] _results;

    void Because() => _results = GenerateOrders([Declaration(First, "later-scope", 6), Declaration(First, "earlier-scope")], Members(First));

    [Fact] void should_keep_both_declaration_sources() => _results.All(result => result.Graph.BackendScopes.Single().Evidence.Count == 2).ShouldBeTrue();
    [Fact] void should_choose_the_canonical_declaration_source_in_both_orders() => _results.All(result => ScopeWarnings(result).Single().Source == At(5).Source).ShouldBeTrue();
    [Fact] void should_keep_diagnostics_identical_in_both_orders() => Describe(_results[1].Diagnostics).ShouldEqual(Describe(_results[0].Diagnostics));
}
