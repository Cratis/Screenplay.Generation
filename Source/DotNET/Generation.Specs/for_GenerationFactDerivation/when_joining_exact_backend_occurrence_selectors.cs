// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_joining_exact_backend_occurrence_selectors : given.an_artifact_usage
{
    static readonly BackendScopeKey First = new() { Store = new SubjectId { Value = "dotnet://Application/Stores.First" } };
    static readonly BackendScopeKey Second = new() { Store = new SubjectId { Value = "dotnet://Application/Stores.Second" } };
    GenerationDerivationSnapshot _result;
    GenerationDerivationSnapshot _unscoped;

    void Because()
    {
        GenerationFact[] declarations =
        [
            Scope(First, "first-scope"), Scope(Second, "second-scope"),
            Artifact(Query with { Scope = First }, "first-command", "FindCustomer"), Artifact(Query with { Scope = Second }, "second-command", "FindCustomer"),
            Artifact(Document with { Scope = First }, "first-model", "Customer"), Artifact(Document with { Scope = Second }, "second-model", "Customer"),
            Entry("first-entry", artifact: Query with { Scope = First }), Entry("second-entry", artifact: Query with { Scope = Second }),
            UsageDeclaration("first-declaration", Document with { Scope = First }), UsageDeclaration("second-declaration", Document with { Scope = Second })
        ];
        var forward = Usage("forward");
        var reverse = Usage("reverse");
        _result = Derive([.. declarations, forward with { Definition = forward.Definition with { SourceScope = First, TargetScope = Second } }, reverse with { Definition = reverse.Definition with { SourceScope = Second, TargetScope = First } }]);
        _unscoped = Derive([.. declarations, Usage()]);
    }

    [Fact] void should_derive_only_the_two_explicit_cross_store_reads() => _result.Facts.Length.ShouldEqual(2);
    [Fact] void should_preserve_independent_endpoint_scopes() => _result.Facts.Select(record => ((RelationshipFact)record.Fact).Definition.Key).All(key => key.SourceScope != key.TargetScope).ShouldBeTrue();
    [Fact] void should_keep_scope_in_derived_identity() => _result.Facts.Select(record => record.Fact.Id).Distinct().Count().ShouldEqual(2);
    [Fact] void should_not_treat_null_as_a_scope_wildcard() => _unscoped.Facts.ShouldBeEmpty();
    [Fact] void should_include_both_scope_declarations_in_every_derived_lineage() => _result.Facts.All(record => record.Lineage.Inputs.Length == 7).ShouldBeTrue();
    [Fact] void should_keep_scope_selection_unambiguous() => _result.Diagnostics.ShouldBeEmpty();

    static BackendScopeFact Scope(BackendScopeKey key, string id) => new()
    {
        Id = new FactId { Value = $"application:{id}" },
        Subject = key.Store,
        Evidence = UsageEvidence(),
        Definition = new BackendScopeDefinition { Key = key }
    };
}
