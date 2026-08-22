<!--
Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information.
-->

# Screenplay generation SDK implementation status

## Architecture decision

The source-generation work uses separate repositories and release cadences:

```text
Cratis/Screenplay
  Language, compiler, AST, printer, editor tooling only

Cratis/Screenplay.Generation
  Cratis.Screenplay.Generation.Contracts
  Cratis.Screenplay.Generation
  Cratis.Screenplay.Generation.DotNet

Cratis/Screenplay.CritterStack
  Cratis.CritterStack.Screenplay
  complete Marten/Wolverine generator façade

Cratis/Arc
  existing Cratis.Arc.Screenplay complete generator

Cratis/cli
  MSBuildWorkspace loading and generator package selection
```

This mirrors the current Arc architecture: Arc publishes `Cratis.Arc.Screenplay`, whose generator receives Roslyn compilations and returns verified `.play` source; Cratis CLI consumes that package and owns workspace loading. The Critter Stack package will expose the same compilation-in/source-out experience while using this shared SDK internally.

## Repository state

- GitHub repository created: <https://github.com/Cratis/Screenplay.Generation>
- Local repository: `/Volumes/sourcecode/repos/cratis/Screenplay.Generation`
- `main` is synchronized with `origin/main` through the initial `v0.1.0` SDK release and repository maintenance.
- Current feature branch: `feat/neutral-concept-representations`.
- Latest GitHub release: [`v0.1.0`](https://github.com/Cratis/Screenplay.Generation/releases/tag/v0.1.0).
- NuGet publication remains blocked by [issue #2](https://github.com/Cratis/Screenplay.Generation/issues/2); verified packages are attached to the release and available through the local feed.

## Projects transferred

- `Source/DotNET/Generation.Contracts`
- `Source/DotNET/Generation`
- `Source/DotNET/Generation.Specs`
- `Source/DotNET/Generation.DotNet`
- `Source/DotNET/Generation.DotNet.Specs`

## Functionality already implemented locally

- Typed adapter identity, subject identity, fact identity, evidence, source range, and diagnostics.
- Artifact, placement, relationship, concept-representation, and concept-attribute facts.
- Subject-aware type references for exact concept binding across projects and namespaces.
- Deterministic fact resolution with duplicate collapse and conflict diagnostics.
- Evidence-strength-aware placement resolution.
- Lowering for concepts, events, read models, reducers, commands, and queries.
- Primitive/enumeration concept and named attribute lowering without module placement, with explicit missing/conflicting/unsupported diagnostics and no `String` fallback.
- Canonical Screenplay printing and compiler verification.
- Reusable Roslyn compilation context, artifact catalog, symbol IDs, generated-source recognition, source ranges, type-shape conversion, and adapter interface.
- Dedicated specs for deterministic generation, conflicts, evidence-strength placement, unplaced artifacts, source cataloging, generated source, type shapes, and provenance.

## Verified before repository split

The code was built and tested while still in the Screenplay working tree:

- Generation Contracts Debug and Release: zero warnings/errors.
- Generation Debug and Release: zero warnings/errors.
- Generation specs: 20 passing.
- Generation.DotNet Debug: zero warnings/errors.
- Generation.DotNet specs: 19 passing.
- Critter Stack synthetic specs: 18 passing.
- Real `BankAccountES` project built cleanly.
- A temporary MSBuildWorkspace runner loaded real BankAccountES source and generated a compiling `.play` document with no diagnostics.
- Canonical Wolverine IncidentService built cleanly.

The Critter Stack source and fixture specs now live in the separate `Screenplay.CritterStack` repository.

## Pre-release contract hardening completed

- .NET adapters now have a required project-qualified subject-ID API; assembly-only IDs were removed.
- The unimplemented `Equivalent` relationship was removed before publication.
- The premature CLR `SchemaVersion` promise was removed; a wire schema will be designed only for a real external adapter protocol.
- Generation now verifies print/compile/print stability.
- Lowerable artifacts without placement produce explicit diagnostics.
- Placement resolution retains all provenance while allowing stronger exact/configured evidence to supersede weaker heuristics.
- Packable projects no longer run as empty test assemblies.
- Local packages were packed, nuspec dependency direction was inspected, and a scratch consumer restored and executed solely from the local NuGet feed.

## Current verification

- Generation specs: 64 passing.
- Generation.DotNet specs: 22 passing.
- Debug and Release net8/net9/net10 builds: zero warnings/errors.
- Concept output compiles and remains stable through print/compile/print.
- Primitive, enum, missing representation, conflicting representation, same-name subject, exact reference, and shuffled-order cases are covered.
- All three packages pack with sentinel version 9999.0.0.
- A scratch consumer restored the sentinel Generation package, emitted a concept through the public API, and compiled the resulting Screenplay.

## Immediate next actions

1. Complete and release neutral concept representation/resolution/lowering in [issue #6](https://github.com/Cratis/Screenplay.Generation/issues/6).
2. Add concept validation facts/resolution/lowering without conflating validation with representation, attributes, or identity.
3. Add authored-source helpers and the Vogen interpreter package in [issue #7](https://github.com/Cratis/Screenplay.Generation/issues/7).
4. Compose Vogen contributions with Critter Stack through its pinned canonical fixture in [`Cratis/Screenplay.CritterStack#25`](https://github.com/Cratis/Screenplay.CritterStack/issues/25).
5. Enable package validation after trusted publication through issue #3.

## Safety

- Do not add Marten, Wolverine, Arc, MSBuildWorkspace, or CLI dependencies here.
- `Generation.Contracts` remains framework/compiler independent.
- `Generation` depends only on Contracts and `Cratis.Screenplay`.
- `Generation.DotNet` depends only on Contracts and Roslyn, never MSBuildWorkspace.
- Never commit local source-reference roots, credentials, caches, `.pi`, `bin`, or `obj`.
