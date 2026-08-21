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
- Working branch: `feat/generation-sdk`
- Initial GitHub commit: `7552d89`
- The uncommitted SDK project trees were moved here before their first source commit.
- No secrets, local `.pi` data, `bin`, or `obj` artifacts were transferred.

## Projects transferred

- `Source/DotNET/Generation.Contracts`
- `Source/DotNET/Generation`
- `Source/DotNET/Generation.Specs`
- `Source/DotNET/Generation.DotNet`
- `Source/DotNET/Generation.DotNet.Specs`

## Functionality already implemented locally

- Typed adapter identity, subject identity, fact identity, evidence, source range, and diagnostics.
- Artifact, placement, and relationship facts.
- Deterministic fact resolution with duplicate collapse and conflict diagnostics.
- Evidence-strength-aware placement resolution.
- Lowering for events, read models, reducers, commands, and queries.
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

- Generation specs: 24 passing.
- Generation.DotNet specs: 19 passing.
- Debug tests: green.
- Release net8/net9/net10 build: zero warnings/errors.
- Three NuGet packages created successfully.
- Scratch package consumer generated and printed a valid Screenplay document.

## Immediate next actions

1. Commit this status update and open the SDK pull request.
2. Add the `minor` release label and monitor CI to green.
3. Merge and verify the three 1.0 packages are published.
4. Use the published SDK versions in `Screenplay.CritterStack`.
5. Derive Critter Stack adapter identity/version from assembly informational version.

## Safety

- Do not add Marten, Wolverine, Arc, MSBuildWorkspace, or CLI dependencies here.
- `Generation.Contracts` remains framework/compiler independent.
- `Generation` depends only on Contracts and `Cratis.Screenplay`.
- `Generation.DotNet` depends only on Contracts and Roslyn, never MSBuildWorkspace.
- Never commit local source-reference roots, credentials, caches, `.pi`, `bin`, or `obj`.
