// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_source_mapping_returns_another_artifact : given.source_placement_candidates
{
    void Because() => result = GenerationFactDerivation.Derive(new AdapterRunSnapshot { Facts = [new() { Fact = CommandDeclaration() }, new() { Fact = Candidate(Command, Command, "self") }] }, new WrongIdentity());

    [Fact] void should_block_the_response() => result.Facts.ShouldBeEmpty();
    [Fact] void should_explain_the_invalid_response() => result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.InvalidSourceOwnerCandidate);

    sealed class WrongIdentity : IArtifactSourcePlacementResolver
    {
        public SourcePlacementResolutionResult Resolve(SourcePlacementResolutionRequest request) => new()
        {
            Artifact = Event,
            Placement = new() { Module = "Customers", Slice = "Register", SliceKind = request.SliceKind }
        };
    }
}
