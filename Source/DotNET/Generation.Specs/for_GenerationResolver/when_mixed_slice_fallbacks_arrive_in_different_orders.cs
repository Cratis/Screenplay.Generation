// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_mixed_slice_fallbacks_arrive_in_different_orders : given.mixed_slice_placements
{
    ResolvedApplicationGraph _reversed = null!;
    void Because()
    {
        var first = Member("Write", GenerationSliceKind.StateChange, "Common");
        var second = Member("Read", GenerationSliceKind.StateView, "Common");
        graph = Resolve(first, second);
        _reversed = Resolve([.. second.Reverse()], [.. first.Reverse()]);
    }

    [Fact] void should_keep_identical_effective_names() => Names(graph).ShouldEqual(Names(_reversed));
    [Fact] void should_keep_identical_normalization_and_diagnostics() => JsonSerializer.Serialize(graph).ShouldEqual(JsonSerializer.Serialize(_reversed));
}
