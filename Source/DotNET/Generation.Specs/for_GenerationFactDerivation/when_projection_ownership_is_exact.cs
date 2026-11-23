// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_projection_ownership_is_exact : given.diagnostic_projection
{
    GenerationDiagnostic _diagnostic;
    void Because()
    {
        var scope = new BackendScopeKey { Store = new() { Value = "symbol://stores/main" }, EventModelName = "Orders" };
        var intent = Intent();
        var support = Support();
        var classification = Classification();
        var scopeFact = new BackendScopeFact { Id = new() { Value = "observations:scope" }, Subject = scope.Store, Evidence = ObservationEvidence(), Definition = new() { Key = scope } };
        _diagnostic = Projected(Derive(
            Observation(),
            intent with { Definition = intent.Definition with { Scope = scope } },
            support with { Definition = support.Definition with { Alternatives = [new() { Method = Method, Scope = scope }] } },
            classification with { Definition = classification.Definition with { Scope = scope } },
            scopeFact,
            CommandDeclaration() with { Definition = CommandDeclaration().Definition with { Artifact = Command with { Scope = scope } } },
            Entry("unscoped"),
            Entry("wrong-qualifier", Command with { Scope = scope with { EventModelName = "orders" } }),
            Entry("correct", Command with { Scope = scope })));
    }
    [Fact] void should_ignore_unscoped_and_differently_qualified_entries() => _diagnostic.Code.ShouldEqual(Payload().Code);
    [Fact] void should_include_scope_evidence() => _diagnostic.Lineage!.Inputs.Select(id => id.Value).ShouldContain("observations:scope");
    [Fact] void should_not_use_the_unscoped_mapping() => _diagnostic.Lineage!.Inputs.Any(id => id.Value == "application:unscoped").ShouldBeFalse();
}
