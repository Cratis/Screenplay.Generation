// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver.given;

public class mixed_slice_placements : facts
{
    protected ResolvedApplicationGraph graph = null!;

    protected static GenerationFact[] Member(string name, GenerationSliceKind kind, string? fallback, string slice = "Shared", string module = "Accounts", string[]? features = null, bool splitWithoutName = false)
    {
        var key = new ArtifactKey { Subject = new() { Value = $"dotnet://Application/{name}" }, Kind = kind == GenerationSliceKind.StateView ? ArtifactKind.Query : ArtifactKind.Command };
        var evidence = new Evidence { Adapter = FirstAdapter, Strength = EvidenceStrength.Exact, Source = new() { Path = $"Accounts/{name}.cs", StartLine = 1, StartColumn = 1, EndLine = 1, EndColumn = 2 } };
        return
        [
            new ArtifactFact { Id = new() { Value = $"first:{name}:artifact" }, Subject = key.Subject, Evidence = evidence, Definition = new() { Key = key, Name = name, File = evidence.Source.Path } },
            new ArtifactPlacementFact
            {
                Id = new() { Value = $"first:{name}:placement" }, Subject = key.Subject, Artifact = key, Evidence = evidence,
                Placement = new() { Module = module, Features = features ?? ["Banking"], Slice = slice, SliceKind = kind },
                MixedSliceFallback = fallback is null && !splitWithoutName ? null : new() { Slice = fallback, Evidence = evidence }
            }
        ];
    }

    protected static ResolvedApplicationGraph Resolve(params GenerationFact[][] members) => new GenerationResolver().Resolve([Contribution(FirstAdapter, [.. members.SelectMany(member => member)])]);

    protected static string[] Names(ResolvedApplicationGraph resolved) => [.. resolved.Placements.Select(placement => placement.Variants.Single().Placement.Slice).Order(StringComparer.Ordinal)];
}
