// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_member_sources_precede_a_backend_scope_declaration : given.backend_scope_diagnostic_sources
{
    BackendScopeFact _declaration;
    GenerationFact[] _members;
    GeneratedScreenplayDefinition[] _results;

    void Establish()
    {
        _declaration = Declaration(First, "scope");
        _members = Members(First);
    }

    void Because() => _results = GenerateOrders([_declaration], _members);

    [Fact] void should_locate_the_warning_at_the_scope_declaration_in_both_orders() => _results.All(result => ScopeWarnings(result).Single().Source == _declaration.Evidence.Source).ShouldBeTrue();
    [Fact] void should_keep_the_warning_severity_and_outcome() => _results.All(result => ScopeWarnings(result).Single() is { Severity: GenerationDiagnosticSeverity.Warning, Outcome: GenerationDiagnosticOutcome.Unsupported }).ShouldBeTrue();
    [Fact] void should_keep_the_warning_message() => ScopeWarnings(_results[0]).Single().Message.ShouldEqual("Backend store/event-model scope is not supported in .play; only structural declarations and relationships can be emitted");
    [Fact] void should_keep_the_scope_subject() => _results.All(result => ScopeWarnings(result).Single().Subject == First.Store).ShouldBeTrue();
    [Fact] void should_keep_links_to_every_scoped_fact() => _results.All(result => ScopeWarnings(result).Single().Facts.ToHashSet().SetEquals(_members.Select(member => member.Id).Append(_declaration.Id))).ShouldBeTrue();
    [Fact] void should_keep_member_and_type_evidence_in_the_graph() => _results.All(result => _members.All(member => result.Graph.Artifacts.Single().Variants.Single().Evidence.Contains(member.Evidence))).ShouldBeTrue();
    [Fact] void should_keep_diagnostics_identical_in_both_orders() => Describe(_results[1].Diagnostics).ShouldEqual(Describe(_results[0].Diagnostics));
    [Fact] void should_keep_canonical_output_identical_in_both_orders() => _results[1].Source.ShouldEqual(_results[0].Source);
}
