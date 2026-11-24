// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ArtifactMemberRoleSelection;

public class when_raw_diagnostic_references_are_malformed : given.role_candidates
{
    Dictionary<string, ResolvedApplicationGraph[]> _graphs = null!;

    void Because()
    {
        Dictionary<string, GenerationDiagnostic> diagnostics = new()
        {
            ["default-facts"] = Warning() with { Facts = default },
            ["null-fact"] = Warning() with { Facts = [null!] },
            ["null-producer"] = Warning() with { Lineage = new() { Producer = null! } }
        };
        _graphs = diagnostics.ToDictionary(entry => entry.Key, entry => Resolve(entry.Value), StringComparer.Ordinal);
    }

    [Fact] void should_reject_default_fact_arrays_through_every_entry_point() => Invalid("default-facts");
    [Fact] void should_reject_null_fact_references_through_every_entry_point() => Invalid("null-fact");
    [Fact] void should_reject_null_lineage_producers_through_every_entry_point() => Invalid("null-producer");
    [Fact] void should_not_select_a_member_for_the_malformed_group() => _graphs.Values.SelectMany(graphs => graphs).All(graph => !Properties(graph, Artifact).Any(property => property.IsIdentifier)).ShouldBeTrue();
    [Fact] void should_still_resolve_the_unrelated_valid_group() => _graphs.Values.SelectMany(graphs => graphs).All(graph => Properties(graph, EventArtifact).Single().IsIdentifier).ShouldBeTrue();

    static IReadOnlyList<PropertyDefinition> Properties(ResolvedApplicationGraph graph, ArtifactKey artifact) => graph.Artifacts.Single(resolved => resolved.Key == artifact).Variants.Single().Definition.Properties;

    ResolvedApplicationGraph[] Resolve(GenerationDiagnostic diagnostic)
    {
        var unrelated = Candidate("first:unrelated", "firstId", 1);
        unrelated = unrelated with { Subject = EventSubject, Definition = unrelated.Definition with { Artifact = EventArtifact, Member = new() { Artifact = EventArtifact, Name = "firstId" } } };
        var contributions = Inputs(Candidate("first:malformed", "secondId", 2, diagnostics: [diagnostic]), unrelated);
        return
        [
            Generator.Generate(Capture(contributions), new() { Domain = "Ordering" }).Graph,
            Generator.Generate(contributions, new() { Domain = "Ordering" }).Graph,
            new GenerationResolver().Resolve(contributions)
        ];
    }

    void Invalid(string name) => _graphs[name].All(graph => graph.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.InvalidArtifactMemberRoleSelection)).ShouldBeTrue();
}
