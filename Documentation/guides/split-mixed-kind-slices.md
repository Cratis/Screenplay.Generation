<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Split mixed-kind slices

When source placement puts commands and read models in the same slice, you can
opt into a deterministic split without changing their module or feature owners.
Generation keeps same-kind groups unchanged and never infers semantic slice kinds
from filenames or framework names.

## Opt in from an adapter

Declare `GenerationFactCapability.MixedSlicePlacementFallback` (27) alongside the
capability for your placement facts or source-owner candidates. Attach a
`MixedSlicePlacementFallbackDefinition` to `ArtifactPlacementFact.MixedSliceFallback`
or `ArtifactSourcePlacementCandidateDefinition.MixedSliceFallback`.

Use version `1` and mode `ArtifactThenKind`. Supply the policy's `Evidence`,
including an authoritative source range when available. Set `Slice` to an explicit
artifact-specific name such as `OpenAccount` or `AccountSummary`, not a module,
feature path, or filename. Omit `Slice` when no artifact-specific name is known;
the original slice name then remains the base. A missing metadata object does
**not** opt in. Admission rejects unsupported versions, modes, and path-shaped or
blank names, and checks nested evidence just like fact evidence.

## Understand the two stages

Generation groups effective, unconflicted placements by backend scope, module,
complete feature chain, and slice name. Only groups containing more than one
known slice kind are eligible:

1. Replace each opted-in member's name with its supplied artifact-specific name,
   retaining the original name when the name is absent.
2. Regroup once. Where kinds still collide, append `StateChange`, `StateView`,
   `Automation`, or `Translate` to each opted-in member's current name.

For example, two members named `Shared` with fallbacks `OpenAccount` and
`AccountSummary` stop after stage one. If both fallbacks are `Account`, they become
`AccountStateChange` and `AccountStateView`. Unknown kinds, unannotated placements,
and groups that already have one kind are not renamed. There is no third rename
or numeric suffix to hide a remaining collision.

## Inspect the result

`ResolvedApplicationGraph.PlacementNormalizations` records original, intermediate,
and final placements with their input fact IDs and evidence. Generation snapshots
also expose normalized placement facts under derivation rule
`cratis.screenplay.mixed-slice-placement`, version `1.0.0`. Their lineage points to
admitted inputs, including source-owner candidates when placement was derived.
Original facts remain unchanged as provenance; effective outputs carry the final
lowering disposition. Repaired inputs are not marked conflicted merely because
the split produced a warning.

`GEN0079` explains a split with the original located evidence. `GEN0078` reports
a mixed-kind collision that remains after both stages, including collisions with
existing sibling names. Correct the supplied fallback names or source placement
when this happens; Generation preserves the conflict instead of inventing a name.

See [Build a source adapter](build-source-adapter.md) for adapter admission,
source authority, and generation setup.
