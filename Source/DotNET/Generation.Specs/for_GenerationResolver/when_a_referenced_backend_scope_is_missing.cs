// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_a_referenced_backend_scope_is_missing : given.backend_scopes
{
    ResolvedApplicationGraph _graph;

    void Because() => _graph = new GenerationResolver().Resolve([Contribution(Artifact(Key(EventSubject, ArtifactKind.Event, First), "event", "CustomerRegistered"))]);

    [Fact] void should_not_invent_a_scope_declaration() => _graph.BackendScopes.Single().Definitions.ShouldBeEmpty();
    [Fact] void should_report_the_exact_missing_scope() => _graph.Diagnostics.Single().Code.ShouldEqual(GenerationDiagnosticCodes.UnknownBackendScope);
    [Fact] void should_link_the_scoped_input() => _graph.Diagnostics.Single().Facts.Single().ShouldEqual(Id("event"));
}
