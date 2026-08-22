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
- Current public release: [`v0.7.0`](https://github.com/Cratis/Screenplay.Generation/releases/tag/v0.7.0) at `d9ff40966b637b25126160629d567719ce218a01`.
- The four packages release in lockstep: Contracts, Generation, DotNet, and DotNet.Vogen.
- `0.7.0` is the public compatibility floor protected by package validation. Compatible additions remain allowed; removals and incompatible signature changes fail validation.
- Unchanged legacy binaries compiled against the `0.1.0` core packages and the first correctly sourced `0.5.0` Vogen package remain the compatibility-ancestry smoke.
- Historical `Cratis.Screenplay.Generation.DotNet.Vogen` versions `0.2.0` through `0.4.0` were incorrectly sourced. Manual unlisting remains tracked in [issue #13](https://github.com/Cratis/Screenplay.Generation/issues/13); OIDC publication credentials cannot delete or unlist them.
- The normative lifecycle and compatibility contract is in [`COMPATIBILITY.md`](COMPATIBILITY.md).

## Delivered public capability

- Typed adapter, subject, and fact identities; evidence, provenance, source ranges, and stable diagnostics.
- Artifact, placement, relationship, concept-representation, concept-attribute, and named concept-validation facts.
- Exact `TypeReferenceDefinition.Subject` binding across projects and namespaces without display-name fallback.
- Deterministic duplicate collapse, conflict diagnostics, and public visibility of every incompatible resolved variant.
- Evidence-strength-aware placement resolution that retains weaker provenance while stronger exact/configured evidence determines the effective placement.
- Lowering for concepts, events, read models, reducers, commands, queries, attributes, and named external validation predicates.
- Canonical printing, Screenplay compiler verification, and print/compile/print stability checking.
- Required authoritative `AuthoredSyntaxTrees` on every `DotNetProjectCompilation`, plus reusable authored declaration, attribute, partial, evidence, symbol, catalog, and type-shape APIs.
- Exact authored Vogen value-object, supported primitive representation, and `Validate(TBacking) -> Vogen.Validation` discovery without a production Vogen dependency.
- Stable Vogen loss codes: `VOG0001` unsupported backing, `VOG0002` unrepresented normalization, and `VOG0003` unrepresented named instance.
- Independent Vogen and external-adapter contribution composition through one neutral resolver/generator pipeline.

## Current verification evidence

- Generation specs: 93 passing.
- Generation.DotNet specs: 28 passing.
- Generation.DotNet.Vogen specs: 84 passing.
- Total Debug specs: **205 passing**.
- Debug `net10.0` and Release `net8.0`, `net9.0`, and `net10.0`: zero warnings and zero errors.
- All four sentinel `9999.0.0` packages pack with package validation against published `0.7.0`.
- The isolated package cache starts empty for consumer verification.
- Existing binaries compiled against core `0.1.0` and Vogen `0.5.0` run unchanged against the sentinel packages.
- A separate current-source consumer compiles only against the sentinel package set. With fake exact Vogen metadata authored directly in Roslyn source and no Vogen runtime/source-generator package, it verifies authoritative authored trees, exact subject references, neutral representation and named validation facts, conflict variants, stable Vogen diagnostic-code APIs, independent adapter composition and provenance, and compiler-verified deterministic source without verification loss.

## Coordinated repository state

- Critter Stack is released as [`v0.15.0`](https://github.com/Cratis/Screenplay.CritterStack/releases/tag/v0.15.0) at `a0df0c22f112b98b353ca6a84072761de119c7ba`. Its facade composes independent Vogen and Marten/Wolverine contributions through Generation `0.7.0`; the low-level Critter Stack adapter remains Vogen-independent. The release gate passed 352 specs and seven canonical fixtures while preserving the six pre-existing outputs.
- CLI `v2.14.0` is the coordinated CLI checkpoint. It owns workspace evaluation, explicit multi-target-framework selection, provider/host selection, package and assembly provenance, compatibility admission, and output. It consumes Generation `0.6.1` plus Critter Stack `0.13.1`; updating to Generation `0.7.0` plus Critter Stack `0.15.0` remains active CLI work.

## Immediate next actions

1. Continue [Generation issue #5](https://github.com/Cratis/Screenplay.Generation/issues/5) after this compatibility-floor increment. Typed `Unknown` representation and disposition hardening remain outstanding; **issue #5 is not closed by this increment**.
2. Continue [CLI issue #87](https://github.com/Cratis/cli/issues/87), including the Generation `0.7.0` / Critter Stack `0.15.0` update and the remaining fail-closed workspace behavior.
3. Leave [Generation issue #13](https://github.com/Cratis/Screenplay.Generation/issues/13) open until a NuGet owner manually unlists the identified incorrectly sourced Vogen packages.

## Safety boundaries

- Do not add Marten, Wolverine, Arc, MSBuildWorkspace, CLI, or Vogen runtime/source-generator dependencies here.
- `Generation.Contracts` remains framework/compiler independent.
- `Generation` depends only on Contracts and `Cratis.Screenplay`.
- `Generation.DotNet` depends only on Contracts and Roslyn, never MSBuildWorkspace.
- `Generation.DotNet.Vogen` depends only on `Generation.DotNet`; Vogen is used only in semantic specs.
- Never commit local source-reference roots, credentials, caches, `.pi`, `bin`, or `obj`.
