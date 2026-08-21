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
- Dedicated specs for deterministic generation, conflicts, schema mismatch, source cataloging, generated source, type shapes, and provenance.

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

## Contract issues to resolve before first public release

1. Include project/target identity in .NET subject IDs, not assembly name alone.
2. Either implement or remove the currently declared `Equivalent` relationship before freezing contracts.
3. Decide what in-process schema/version promise `AdapterContribution` makes; do not accidentally promise a future wire protocol.
4. Add full print/compile/print verification rather than compile-only verification.
5. Define deliberate behavior for unplaced/non-lowerable facts.
6. Derive adapter identity/version from assembly package metadata in adapter repositories.
7. Add package-level compatibility and scratch-consumer tests.

## Immediate next actions

1. Finish repository scaffolding: package metadata, central versions, solution, CI, publishing, README, and framework instructions.
2. Change `Generation` to consume the published `Cratis.Screenplay` package instead of a cross-repository project reference.
3. Restore, build, and run all SDK specs in this independent repository.
4. Fix the pre-release contract issues above and add regression specs.
5. Pack all public packages and inspect nuspec dependency direction.
6. Open and merge the SDK PR with a `minor` release label.
7. Use the published SDK versions in `Screenplay.CritterStack`.

## Safety

- Do not add Marten, Wolverine, Arc, MSBuildWorkspace, or CLI dependencies here.
- `Generation.Contracts` remains framework/compiler independent.
- `Generation` depends only on Contracts and `Cratis.Screenplay`.
- `Generation.DotNet` depends only on Contracts and Roslyn, never MSBuildWorkspace.
- Never commit local source-reference roots, credentials, caches, `.pi`, `bin`, or `obj`.
