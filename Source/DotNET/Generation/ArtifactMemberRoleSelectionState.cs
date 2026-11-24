// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation;

/// <summary>The role-selection evidence, selected roles and lineage rules carried by <see cref="EffectiveGenerationInputs"/>.</summary>
sealed record ArtifactMemberRoleSelectionState
{
    public ImmutableDictionary<GenerationFactInstance, ImmutableArray<GenerationDiagnostic>> Diagnostics { get; init; } = [];

    public ImmutableDictionary<GenerationFactInstance, ImmutableArray<GenerationFact>> Inputs { get; init; } = [];

    public ImmutableArray<GenerationFactRecord> Roles { get; init; } = [];

    public ImmutableArray<GenerationDerivationRuleRecord> Rules { get; init; } = [];
}
