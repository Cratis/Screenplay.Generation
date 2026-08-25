<!--
Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information.
-->

# Screenplay generation SDK implementation status

## Architecture and ownership

The source-generation work is split by responsibility and release cadence:

```text
Cratis/Screenplay
  Language, compiler, AST, printer, and editor tooling

Cratis/Screenplay.Generation
  Neutral contracts, deterministic resolution/lowering/verification,
  reusable Roslyn source mechanics, and the separate Vogen adapter

Cratis/Screenplay.CritterStack
  Marten/Wolverine semantics and the composed generator facade

Cratis/Arc
  Arc semantics and its complete generator facade

Cratis/cli
  Workspace/project loading, authored-tree establishment,
  package/provider discovery and admission, and output orchestration
```

Hosts own loading and orchestration, adapters own source-framework semantics, and Screenplay Generation owns neutral composition plus deterministic AST/text generation and compiler verification. Protocol, schema, adapter, Screenplay language, Generation package, and CLI versions remain independent compatibility dimensions.

## Current Generation release

- Repository: <https://github.com/Cratis/Screenplay.Generation>
- Current public release: [`v0.12.0`](https://github.com/Cratis/Screenplay.Generation/releases/tag/v0.12.0) at `65eb13329f158ea1d18a1eb34b86386ad87a8003`.
- The four packages release in lockstep: Contracts, Generation, DotNet, and DotNet.Vogen.
- `0.7.0` is the minimum public compatibility floor; package validation compares the complete public surface against the latest published release, `0.12.0`. Compatible additions remain allowed; removals and incompatible signature changes fail validation.
- `0.10.0` introduced neutral executable-specification facts, admission, and lowering; `0.10.1` corrected bare rejection-step admission; `0.12.0` is the released shared adapter and strict source-placement baseline.
- Unchanged legacy binaries compiled against the `0.1.0` core packages and the first correctly sourced `0.5.0` Vogen package remain the compatibility-ancestry smoke.
- Historical `Cratis.Screenplay.Generation.DotNet.Vogen` versions `0.2.0` through `0.4.0` were incorrectly sourced. Manual unlisting remains tracked in [issue #13](https://github.com/Cratis/Screenplay.Generation/issues/13); OIDC publication credentials cannot delete or unlist them.
- The normative lifecycle and compatibility contract is in [`COMPATIBILITY.md`](COMPATIBILITY.md).

## Delivered public capability

- Typed adapter, subject, and fact identities; evidence, provenance, source ranges, and stable diagnostics.
- Artifact, placement, relationship, concept-representation, concept-attribute, and named concept-validation facts.
- Exact `TypeReferenceDefinition.Subject` binding across projects and namespaces without display-name fallback.
- Deterministic duplicate collapse, conflict diagnostics, and public visibility of every incompatible resolved variant.
- Additive typed `Unknown`/`Conflict`/`Unsupported` diagnostic outcomes and `Unknown = -1` public fact discriminators. Malformed or future undefined values fail closed before resolution, omit only the affected semantic unit, preserve source/subject evidence, and never map to another role.
- Evidence-strength-aware placement resolution that retains weaker provenance while stronger exact/configured evidence determines the effective placement.
- Lowering for concepts, events, read models, reducers, commands, queries, attributes, and named external validation predicates.
- Canonical printing, Screenplay compiler verification, and print/compile/print stability checking.
- Required authoritative `AuthoredSyntaxTrees` on every `DotNetProjectCompilation`, plus reusable authored declaration, attribute, partial, evidence, symbol, catalog, and type-shape APIs.
- Additive project-qualified source-file identity and explicit workspace/project display policies, with legacy `SourceRoot` behavior preserved for existing hosts.
- Exact authored Vogen value-object, supported primitive representation, and `Validate(TBacking) -> Vogen.Validation` discovery without a production Vogen dependency.
- Stable Vogen loss codes: `VOG0001` unsupported backing, `VOG0002` unrepresented normalization, and `VOG0003` unrepresented named instance.
- Independent Vogen and external-adapter contribution composition through one neutral resolver/generator pipeline.
- Neutral executable-specification scenarios with ordered Given/When/Then facts, typed values, deterministic admission, and Screenplay lowering.
- Shared .NET adapter helpers for collection element types, named attribute arguments, companion method families, resilient type shapes, and declared concept nomination through `DotNetConceptFacts.Emit(...)`.
- Fixed, project-qualified .NET source-structure snapshots and deterministic source placement derivation with host-owned policy, application/specification project roles, and fail-closed `DOTNETSP####` diagnostics.
- Additive exact `SourceOwner` requests for method-backed and synthetic artifacts without rewriting fixed snapshots.
- Explicit versioned flat-source compatibility placement admitted only after a sole `DOTNETSP0004`, with strict/compatibility policy, usage, trigger, and source-owner provenance on each result.

## Current verification evidence

- Full Debug suite: **508 specs passed** — Generation 175, Generation.DotNet 231, and Generation.DotNet.Vogen 102.
- Release targets `net8.0`, `net9.0`, and `net10.0` built with zero warnings and zero errors.
- All four sentinel `9999.0.0` packages validate against published `0.12.0`.
- Legacy binaries compiled against core `0.1.0` and Vogen `0.5.0` run unchanged against the candidate packages.
- The clean current-source consumer verifies shared symbol/method identities, source snapshots and placement, concept nomination, executable specification facts and bare rejection admission, adapter composition, conflicts, provenance, deterministic source, compiler verification, and round-trip stability.
- The isolated package cache starts empty for consumer verification.
- Existing binaries compiled against core `0.1.0` and Vogen `0.5.0` run unchanged against the sentinel packages.
- A separate current-source consumer compiles only against the sentinel package set. With fake exact Vogen metadata authored directly in Roslyn source and no Vogen runtime/source-generator package, it verifies legacy positional-null source calls, explicit stable source identity/display mapping, authoritative authored trees, exact subject references, neutral representation and named validation facts, conflict variants, stable Vogen diagnostic-code APIs, independent adapter composition and provenance, and compiler-verified deterministic source without verification loss.

## Coordinated repository state

- Critter Stack public `main` is released as `v0.21.0` and consumes all four Generation packages at `0.9.0`.
- Cratis CLI public `main` is released as `v2.17.0` and consumes Generation `0.9.0`; it owns workspace loading, package provenance, provider admission, and distribution.
- Arc public `main` owns its adapter independently and does not currently consume these Generation packages; neutral specification adoption is tracked separately in Arc.

## Unreleased `main` capability

- Exact alternate source owners and explicit fail-closed flat-source compatibility placement are implemented after `v0.12.0` for the preserved Critter Stack adoption blocker.
- The next release should include those additive APIs and be followed by resumed Critter Stack shared-placement adoption.

## Immediate next actions

1. Publish the additive source-owner and explicit flat-source compatibility contract as the next lockstep minor release after fresh package/consumer verification.
2. Resume Critter Stack shared source-placement adoption against that public release, preserving canonical output and strict project-aware diagnostics.
3. Continue atomic adapter orchestration under [issue #17](https://github.com/Cratis/Screenplay.Generation/issues/17), with granular derivation in #19, validation contracts in #20, and descriptor/probe admission in #23.
4. Reconcile issue #18 and #25 with capability already merged or released.
5. Leave [Generation issue #13](https://github.com/Cratis/Screenplay.Generation/issues/13) open until a NuGet owner manually unlists the identified incorrectly sourced Vogen packages.

## Safety boundaries

- Do not add Marten, Wolverine, Arc, MSBuildWorkspace, CLI, or Vogen runtime/source-generator dependencies here.
- `Generation.Contracts` remains framework/compiler independent.
- `Generation` depends only on Contracts and `Cratis.Screenplay`.
- `Generation.DotNet` depends only on Contracts and Roslyn, never MSBuildWorkspace.
- `Generation.DotNet.Vogen` depends only on `Generation.DotNet`; Vogen is used only in semantic specs.
- Never commit local source-reference roots, credentials, caches, `.pi`, `bin`, or `obj`.
