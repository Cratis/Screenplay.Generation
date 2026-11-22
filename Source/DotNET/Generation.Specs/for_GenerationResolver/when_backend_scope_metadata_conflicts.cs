// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_backend_scope_metadata_conflicts : given.backend_scopes
{
    ResolvedApplicationGraph _graph;

    void Because()
    {
        var original = Scope(First, "original");
        var conflicting = original with { Id = Id("conflict"), Definition = original.Definition with { Name = "Other display name" } };
        _graph = new GenerationResolver().Resolve([Contribution(original, conflicting)]);
    }

    [Fact] void should_keep_one_exact_scope() => _graph.BackendScopes.Count.ShouldEqual(1);
    [Fact] void should_retain_both_metadata_variants() => _graph.BackendScopes.Single().Definitions.Count.ShouldEqual(2);
    [Fact] void should_diagnose_the_same_key_conflict() => _graph.Diagnostics.Single().Code.ShouldEqual(GenerationDiagnosticCodes.ConflictingBackendScope);
    [Fact] void should_retain_both_input_links() => _graph.Diagnostics.Single().Facts.Length.ShouldEqual(2);
}
