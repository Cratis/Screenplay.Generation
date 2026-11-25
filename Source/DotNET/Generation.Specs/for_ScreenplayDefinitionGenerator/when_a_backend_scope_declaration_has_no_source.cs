// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_a_backend_scope_declaration_has_no_source : given.backend_scope_diagnostic_sources
{
    GeneratedScreenplayDefinition[] _results;

    void Because()
    {
        var declaration = Declaration(First, "scope");
        declaration = declaration with { Evidence = declaration.Evidence with { Source = null } };
        _results = GenerateOrders([declaration], Members(First));
    }

    [Fact] void should_keep_the_source_less_declaration_evidence() => _results.All(result => result.Graph.BackendScopes.Single().Evidence.Single().Source is null).ShouldBeTrue();
    [Fact] void should_fall_back_to_the_canonical_reference_source_in_both_orders() => _results.All(result => ScopeWarnings(result).Single().Source == At(2).Source).ShouldBeTrue();
    [Fact] void should_keep_diagnostics_identical_in_both_orders() => Describe(_results[1].Diagnostics).ShouldEqual(Describe(_results[0].Diagnostics));
}
