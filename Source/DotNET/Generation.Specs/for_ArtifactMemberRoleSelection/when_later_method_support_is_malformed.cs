// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ArtifactMemberRoleSelection;

public class when_later_method_support_is_malformed : given.role_candidates
{
    Dictionary<string, ResolvedApplicationGraph[]> _graphs = null!;

    void Because()
    {
        var later = Candidate("second:later", "secondId", 2, producer: Second);
        var support = Support(later);
        Dictionary<string, GenerationFact[]> cases = new()
        {
            ["undeclared-scope"] = [later, support with { Definition = support.Definition with { Alternatives = [new() { Method = Method, Scope = new() { Store = new() { Value = "source://missing/store" } } }] } }],
            ["empty-alternatives"] = [later, support with { Definition = support.Definition with { Alternatives = [] } }],
            ["null-alternatives"] = [later, support with { Definition = support.Definition with { Alternatives = null! } }],
            ["null-alternative"] = [later, support with { Definition = support.Definition with { Alternatives = [null!] } }],
            ["duplicate-candidate"] = [later, later with { Definition = later.Definition with { Member = new() { Artifact = Artifact, Name = "thirdId" } } }, support]
        };
        _graphs = cases.ToDictionary(entry => entry.Key, entry => Resolve(entry.Value), StringComparer.Ordinal);
    }

    [Fact] void should_reject_an_undeclared_support_scope() => Invalid("undeclared-scope");
    [Fact] void should_reject_an_empty_alternatives_list() => Invalid("empty-alternatives");
    [Fact] void should_reject_a_null_alternatives_list() => Invalid("null-alternatives");
    [Fact] void should_reject_a_null_alternative() => Invalid("null-alternative");
    [Fact] void should_reject_duplicate_same_producer_candidate_ids() => Invalid("duplicate-candidate");
    [Fact] void should_never_fall_back_to_the_earlier_member() => _graphs.Values.SelectMany(graphs => graphs).All(graph => graph.Artifacts.Single(artifact => artifact.Key == Artifact).Variants.Single().Definition.Properties.All(property => !property.IsIdentifier)).ShouldBeTrue();
    [Fact] void should_keep_the_unrelated_valid_group() => _graphs.Values.SelectMany(graphs => graphs).All(graph => graph.Artifacts.Single(artifact => artifact.Key == EventArtifact).Variants.Single().Definition.Properties.Single().IsIdentifier).ShouldBeTrue();

    ResolvedApplicationGraph[] Resolve(GenerationFact[] malformed)
    {
        var unrelated = Candidate("first:unrelated", "firstId", 1);
        unrelated = unrelated with { Subject = EventSubject, Definition = unrelated.Definition with { Artifact = EventArtifact, Member = new() { Artifact = EventArtifact, Name = "firstId" } } };
        var contributions = Inputs([Candidate("first:earlier", "firstId", 1), unrelated, .. malformed]);
        return
        [
            Generator.Generate(Capture(contributions), new() { Domain = "Ordering" }).Graph,
            Generator.Generate(contributions, new() { Domain = "Ordering" }).Graph,
            new GenerationResolver().Resolve(contributions)
        ];
    }

    void Invalid(string name) => _graphs[name].All(graph => graph.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.InvalidArtifactMemberRoleSelection)).ShouldBeTrue();
}
