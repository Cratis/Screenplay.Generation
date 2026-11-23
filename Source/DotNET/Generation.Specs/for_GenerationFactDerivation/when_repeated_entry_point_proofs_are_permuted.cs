// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_repeated_entry_point_proofs_are_permuted : given.diagnostic_projection
{
    GenerationDiagnostic _forward;
    GenerationDiagnostic _reverse;
    void Because()
    {
        GenerationFact[] facts = [.. Inputs(), Entry("duplicate")];
        _forward = Projected(Derive(facts));
        _reverse = Projected(Derive([.. facts.Reverse()]));
    }
    [Fact] void should_not_count_repeated_proofs_as_ambiguity() => _forward.Code.ShouldEqual(Payload().Code);
    [Fact] void should_preserve_all_proofs_canonically() => JsonSerializer.Serialize(_forward).ShouldEqual(JsonSerializer.Serialize(_reverse));
}
