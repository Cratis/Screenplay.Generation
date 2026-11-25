// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_backend_scopes_have_distinct_registration_sources : given.backend_scope_diagnostic_sources
{
    BackendScopeFact _first;
    BackendScopeFact _second;
    GeneratedScreenplayDefinition[] _results;

    void Establish()
    {
        _first = Declaration(First, "first-scope");
        _second = Declaration(Second, "second-scope", 6);
    }

    void Because() => _results = GenerateOrders([_first, _second], [.. Members(First, "first-event"), .. Members(Second, "second-event")]);

    [Fact] void should_report_one_warning_per_scope() => _results.All(result => ScopeWarnings(result).Length == 2).ShouldBeTrue();
    [Fact] void should_locate_the_first_scope_at_its_own_registration() => _results.All(result => ScopeWarnings(result).Single(diagnostic => diagnostic.Subject == First.Store).Source == _first.Evidence.Source).ShouldBeTrue();
    [Fact] void should_locate_the_second_scope_at_its_own_registration() => _results.All(result => ScopeWarnings(result).Single(diagnostic => diagnostic.Subject == Second.Store).Source == _second.Evidence.Source).ShouldBeTrue();
    [Fact] void should_keep_diagnostics_identical_in_both_orders() => Describe(_results[1].Diagnostics).ShouldEqual(Describe(_results[0].Diagnostics));
}
