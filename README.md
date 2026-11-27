# Screenplay.Generation

Framework-neutral source adapter SDK for generating verified [Cratis Screenplay](https://github.com/Cratis/Screenplay) definitions.

Screenplay.Generation is part of the Cratis model-first layer — [Studio](https://github.com/Cratis/Studio), [Screenplay](https://github.com/Cratis/Screenplay), [Stage](https://github.com/Cratis/Stage), [Scene](https://github.com/Cratis/Scene), and [Prologue](https://github.com/Cratis/Prologue) — which is experimental and in early development. Within that layer, this SDK owns source-to-Screenplay recovery: adapters contribute typed semantic facts from existing codebases, and Generation resolves them into one deterministic, compiler-verified Screenplay model of an event-sourced, CQRS system.

## Packages

| Package | Responsibility |
| --- | --- |
| `Cratis.Screenplay.Generation.Contracts` | Typed semantic facts, evidence, provenance, and diagnostics contributed by source adapters |
| `Cratis.Screenplay.Generation` | Deterministic fact resolution, Screenplay lowering, canonical printing, and compiler verification |
| `Cratis.Screenplay.Generation.DotNet` | Reusable Roslyn compilation, symbol, authored-source, and type-shape APIs for .NET adapter authors |
| `Cratis.Screenplay.Generation.DotNet.Vogen` | Authored-source Vogen value-object discovery as neutral concept, primitive-representation, and named validation-rule facts |

Framework adapters remain owned by their source ecosystems:

- Arc: [`Cratis.Arc.Screenplay`](https://github.com/Cratis/Arc)
- Critter Stack: [`Cratis.CritterStack.Screenplay`](https://github.com/Cratis/Screenplay.CritterStack)

See [Build a .NET source adapter](Documentation/guides/build-source-adapter.md) for the canonical adapter contract, source-authority rules, fact and evidence patterns, composition flow, and verification checklist.

## Public baseline and current main

`0.16.0` is the current public lockstep release and package-validation baseline. Granular type-use derivation is additive on `main`.

| Capability | Released `0.16.0` | Current `main` |
| --- | ---: | ---: |
| Adapter, context, neutral fact, evidence, and diagnostic contracts | Yes | Yes |
| Stable source identity, fixed source snapshots, and strict placement | Yes | Yes |
| Exact method signatures and bounded scalar, payload, and collection extraction | Yes | Yes |
| Authoritative invocation and assignment enumeration | Yes | Yes |
| Legacy `IDotNetScreenplayAdapter` | Yes | Yes |
| Descriptors, structured probes, and atomic public admission | Yes | Yes |
| Explicit modern/legacy registration and deterministic .NET runner | Yes | Yes |
| Immutable adapter-run snapshots and `Generate(snapshot)` | Yes | Yes |
| Per-fact generation dispositions | Yes | Yes |
| Vogen modern descriptor/probe with legacy contribution parity | Yes | Yes |
| Granular artifact/member/type-use/binding/role facts | No | Yes |
| Fixed-snapshot derivation rule and input/evidence lineage | No | Yes |
| Exact nested .NET type-use fact emission | No | Yes |
| Exact method eligibility with conditional observation support | No | Yes |
| Opt-in query input transfer from declaration-owned surfaces | No | Yes |
| Opt-in method diagnostic projection with diagnostic lineage | No | Yes |
| Cross-producer omission coalescing by family and exact occurrence | No | Yes |
| Ordered exclusive member-role alternatives, including explicit no-member selections | No | Yes |

Current `main` also supports [query input derivation from declaration surfaces](Documentation/guides/query-input-member-derivation.md): explicit transfer intent and proved execution/source order add exact inputs without turning source declarations into artifacts. Ordinary `Reads` facts retain their original meaning. Method-dependent observations are selected before all derivation and resolution; the adapter guide documents the source-neutral eligibility proof contract.

## Ordered exclusive member-role selection

Current `main` adds `ArtifactMemberRoleSelectionFact` for adapters that observe several alternative configurations of one exclusive role. Declare `GenerationFactCapability.ArtifactMemberRoleSelection` (value `33`) in the adapter descriptor. This is an additive contract; existing capability values, `ArtifactMemberRoleFact`, aggregate properties, and their union semantics are unchanged when no grouped alternatives are contributed.

A group is the exact `ArtifactKey` plus `ArtifactMemberRoleKind`: subject, artifact kind, store, and event-model qualifier all matter. A null scope is an unscoped occurrence, not a wildcard. Scoped alternatives require a valid backend-scope declaration; scoped concepts and queries are not supported. `Identifier` and `EventSourceIdentifier` remain different roles.

Each `ArtifactMemberRoleSelectionDefinition` requires:

- `Artifact` and `Role`, identifying the exclusive group;
- `Member`, either an exact `ArtifactMemberKey` belonging to that artifact or **null**, explicitly selecting no member;
- signed `int Priority` and nonnegative `long ObservationOrder`;
- optional `Diagnostics`, producer-owned payloads to publish only if this eligible alternative wins.

Method support is evaluated **before** ranking. No `MethodFactSupportFact` means unconditional eligibility; otherwise an ordinary or unclassified exact method alternative is sufficient. Failure-only alternatives are removed, so an earlier eligible configuration may remain. A scoped alternative's support must use that same scope. Eligible alternatives are ranked lexicographically by `(Priority, ObservationOrder)`, highest first. An explicit no-member winner suppresses earlier members without falling back. Malformed metadata blocks the identifiable group (`GEN0096`); duplicate complete ranks block it with a conflict (`GEN0097`). Adapter identity, source path, fact ID, enumeration order, and arrival order never break a tie.

Observation order comes from **one stable total observation sequence for the exact artifact/role in a fixed source snapshot**, shared by every contributing producer. It is not a producer's filtered enumeration, registration or contribution position, timestamp, adapter ID, or fact ID. For example, a frontend may enumerate the whole snapshot by stable source-file identity and source position before assigning ordinals. Both atomic adapters use that same convention. The SDK compares the supplied ordinal; it does not infer cross-file execution order or claim that source order is execution order.

For example, a source-independent adapter can emit a conventional fallback (priority `0`), an attribute-derived alternative (`100`), and two configured observations (`200`). These are the frontend's shared policy, not SDK recognition of attributes or configuration APIs:

```csharp
using Cratis.Screenplay.Generation;

var producer = new AdapterIdentity { Id = "ordering", Version = "1.0.0" };
var subject = new SubjectId { Value = "ordering://commands/submit" };
var artifact = new ArtifactKey { Subject = subject, Kind = ArtifactKind.Command };
var earlier = new ArtifactMemberRoleSelectionFact
{
    Id = new FactId { Value = "ordering:identity:1" },
    Subject = subject,
    Evidence = new Evidence { Adapter = producer, Strength = EvidenceStrength.Exact },
    Definition = new ArtifactMemberRoleSelectionDefinition
    {
        Artifact = artifact,
        Role = ArtifactMemberRoleKind.EventSourceIdentifier,
        Member = new ArtifactMemberKey { Artifact = artifact, Name = "orderId" },
        Priority = 200,
        ObservationOrder = 1
    }
};
var conventional = earlier with
{
    Id = new FactId { Value = "ordering:identity:conventional" },
    Definition = earlier.Definition with { Priority = 0, ObservationOrder = 0 }
};
var attributed = earlier with
{
    Id = new FactId { Value = "ordering:identity:attribute" },
    Definition = earlier.Definition with { Priority = 100, ObservationOrder = 0 }
};
var cleared = earlier with
{
    Id = new FactId { Value = "ordering:identity:2" },
    Definition = earlier.Definition with
    {
        Member = null,
        ObservationOrder = 2,
        Diagnostics =
        [
            new GenerationDiagnostic
            {
                Code = "ORDERING_IDENTITY_UNRESOLVED",
                Severity = GenerationDiagnosticSeverity.Warning,
                Message = "The configured member could not be resolved",
                Subject = subject
            }
        ]
    }
};
var support = new MethodFactSupportFact
{
    Id = new FactId { Value = "ordering:identity:2:support" },
    Subject = subject,
    Evidence = earlier.Evidence,
    Definition = new MethodFactSupportDefinition
    {
        Candidate = cleared.Id,
        Alternatives = [new MethodFactSupportAlternative { Method = new SubjectId { Value = "ordering://methods/configure" } }]
    }
};
```

The producer still supplies the artifact and member declarations through the existing contracts. A default tier such as `0` and an explicit tier such as `200` are producer conventions, not framework constants; each producer establishes semantic observation order from its own evidence rather than list position.

A member winner produces one ordinary derived `ArtifactMemberRoleFact`; a no-member winner produces none. All alternatives retain provenance, with core-owned rule `cratis.screenplay.artifact-member-role-selection` version `1.0.0`, aligned input/evidence lineage, and final dispositions. Only the winner's payloads and deferred support diagnostics are published; independent classification diagnostics retain their existing behavior. Payload subjects must match their candidate, referenced facts must be local to that producer contribution, and adapters cannot fabricate core lineage. Modern admission freezes payloads before producer mutation. Raw/legacy inputs are also validated and fail closed.

Do not mix grouped selection with an ungrouped assertion of the same exact role, including an aggregate property already marked as an event-source identifier. This is a local conflict (`GEN0097`), not a union or a last-writer override. Migrate all fallback assertions for that role into candidate form before relying on replacement. Ungrouped facts for other roles or occurrences keep their existing meaning; ordinary `Identifier` lowering is not redefined. The mechanism contains no source-framework syntax or API recognition; frontends contribute neutral observations only.

## Architecture

```text
source adapter
  -> admitted typed facts and evidence
  -> fixed-snapshot derivation and lineage
  -> resolved application graph
  -> lowerable Screenplay model
  -> Screenplay AST
  -> canonical printer
  -> Screenplay compiler verification
  -> .play
```

Adapters contribute semantic facts; they do not construct syntax nodes or concatenate `.play` fragments. This allows facts from related frameworks—such as Marten and Wolverine—to be resolved together before one document is emitted.

`Cratis.Screenplay.Generation.DotNet` deliberately does not own `MSBuildWorkspace`. Hosts such as Cratis CLI load a project once and pass Roslyn compilations to official adapters.

## Adapter discovery and execution

The original `IDotNetScreenplayAdapter` remains source- and binary-compatible. New adapters should implement `IDescribedDotNetScreenplayAdapter`; its `AdapterDescriptor` declares the adapter category, source language, compatible Generation version range, required host and API capabilities, and the neutral fact families it may emit. `Probe()` returns `AdapterProbeNotApplicable`, `AdapterProbeApplicable`, or `AdapterProbeBlocked` together with canonical capability and source evidence instead of reducing admission to a Boolean.

Hosts own an explicit roster. Register modern adapters with `DotNetAdapterRegistration.For(...)` and unchanged legacy adapters with `ForLegacy(...)`, then call `DotNetAdapterRunner.Run(...)` once. The runner canonicalizes the project and adapter rosters, considers and probes each eligible registration once, executes only applicable adapters once, admits each contribution atomically, and returns an immutable `AdapterRunSnapshot`. It never discovers packages implicitly.

Duplicate adapter IDs are rejected before probe or analysis. Invalid descriptors reject the registration; incompatible Generation versions, unavailable host capabilities, unsupported source languages, and unsafe project rosters block before probing; missing API evidence or a blocked probe prevents execution; nonauthoritative source and malformed contributions reject the whole contribution. Callback failures become stable diagnostics and do not expose exception text or machine paths.

A modern `SourceIndependent` adapter with no host requirements can run against an empty .NET context. A source adapter that declares `StableSourceLocations` requires every authored tree to have an authoritative `DotNetProjectSourceContext` mapping, and every located probe, fact, and diagnostic must use that mapping. The modern Vogen path requires stable locations; its legacy interface remains available for compatibility.

Pass the frozen snapshot to `ScreenplayDefinitionGenerator.Generate(snapshot, options)` to preserve runner diagnostics and receive final fact dispositions: `Lowered`, `ProvenanceOnly`, `OmittedWithDiagnostic`, or `Conflicted`. Generation runs one closed derivation pass over the fixed admitted base facts, attaches stable rule/input/evidence lineage under `AdapterRunSnapshot.Derivation`, and applies exact member bindings as granular overlays. The snapshot records one run only; it does not add issue #24 serialization or fingerprints.

### Adapter syntax robustness

`DotNetSource.AuthoredInvocationsIn(...)` and `AuthoredAssignmentsIn(...)` enumerate operations only from the host-authoritative authored syntax-tree snapshot, in deterministic source order. `DotNetSymbols`, `DotNetInvocations`, and `DotNetMethodSignatures` provide shared metadata-name, companion-method, named-argument, invocation-binding, and exact normalized signature mechanics. `DotNetMethodSignatures` preserves nullability, generic arity, return/ref shape, and ordered parameter/ref/`params`/extension-receiver shape across direct and reduced calls. Its Roslyn symbols are compilation-bound: create expected signatures from the exact allowlisted method symbols in the analyzed compilation rather than reconstructing them from names. Adapters should use these helpers instead of duplicating syntax-shape assumptions.

`DotNetSourceValues` extracts exact constants, `typeof` operands, constructed payloads, and authored collection elements as typed `DotNetKnown<T>` results. Dynamic, conditional, computed, compiler-invalid, user-converted, ambiguous, unsupported, incomplete, or opaque source returns an immutable `DotNetUnknown<T>` failure snapshot instead. Payload and collection extraction is atomic: one shape or child failure exposes no partial value, while every deterministic child failure retains its exact authored location. See the adapter guide for the intentionally bounded constructor, initializer, array, and collection forms.

Adapter discovery is based on compiler symbols and semantic models, not preferred source formatting. Semantically valid generated, decompiler-style, fully qualified, explicitly cast, or otherwise non-idiomatic C# must produce the same facts as its idiomatic equivalent. Every .NET adapter should keep one reusable source fixture that deliberately exercises non-idiomatic syntax and assert that its ordinary supported facts still resolve without additional loss.

## Screenplay-to-code rendering

Screenplay Generation owns source-to-Screenplay recovery only. To turn Screenplay into framework code, implement `IArtifactRenderPlanner` from `Cratis.Stage.Contracts`; do not reverse adapter facts or call the Generation lowerer backward. See the [Stage renderer target guide](https://github.com/Cratis/Stage/blob/main/Documentation/guides/build-renderer-target.md).

## Vogen adapter composition

`Cratis.Screenplay.Generation.DotNet.Vogen` recognizes the exact Roslyn metadata names `Vogen.ValueObjectAttribute`, ``Vogen.ValueObjectAttribute`1``, and `Vogen.VogenDefaultsAttribute`. It has no Vogen package or runtime dependency. Vogen is pinned only in the adapter's semantic spec project so production consumers remain decoupled from the source generator.

Each `DotNetProjectCompilation` requires the workspace host's authoritative `AuthoredSyntaxTrees`. Build this set from project documents before source generators update the compilation. Generated filenames and headers remain useful conventions, but they are not trusted as proof of authored origin.

Hosts can additionally provide a `DotNetProjectSourceContext` created by `DotNetSourcePaths.Create(...)`. The context keeps stable source identity separate from the path displayed in generated output and diagnostics:

- identity is the host-declared project identity plus normalized project-relative path;
- `DotNetSourceFile.ProjectRelativePath` preserves authored casing for source-structure derivation independently from identity case folding;
- `SourceRange.Path` is the host-declared display path, explicitly workspace-relative or project-relative;
- `/` separators, Unicode NFC normalization, and the `Ordinal` or `InvariantLowercase` case policy are host-owned rather than inferred from the operating system; `InvariantLowercase` folds first and NFC-normalizes the folded identity;
- the factory returns an immutable defensive snapshot whose project identity, policy, and file mappings cannot be replaced;
- Roslyn `SyntaxTree.FilePath` can retain an absolute physical checkout path, but stable identity and display values never store a physical root;
- duplicate identities and malformed, rooted, traversal, or unmapped authored paths fail with typed exceptions.

Existing `SourceRoot` and evidence overloads remain supported. When no explicit context is supplied, they retain their current display-path behavior and do not add stable identity, preserving existing hosts and package consumers.

### Shared .NET source structure

Hosts declare whether each `DotNetProjectCompilation` contains application artifacts or specifications through `DotNetProjectRole`. The default remains `Application` for existing consumers.

`DotNetSourceStructureResolver` applies one versioned, host-owned `DotNetSourceStructurePolicy` to fixed source evidence. By default, the first post-root folder or namespace segment is the module, the final segment is the slice, and intermediate segments are features. A host can declare a project-relative `FeatureRoot`, skip leading namespace segments, or explicitly collapse all roots into one module. Existing composition hosts can use `DotNetAdapterOptions.SourceStructurePolicy` to apply those options through the shared resolver without duplicating policy mapping. The semantic slice kind is supplied independently; source layout never decides whether behavior is a state change, state view, automation, or translation.

`DotNetSourceStructures.Create(...)` builds the fixed snapshot from authoritative authored trees and the host's stable project source contexts. It retains every project-relative path of a partial declaration, orders projects, subjects, paths, and diagnostics canonically, and never exposes physical checkout paths. Missing contexts or mappings and duplicate project-qualified subjects fail closed.

`DotNetSourcePlacementDerivation.Derive(...)` then combines exact artifact roles and independently established slice kinds with that fixed snapshot. Identical requests execute once, request order cannot change output, and mismatched subjects, unknown artifact kinds, or multiple distinct requests for one artifact role block that artifact rather than selecting a provider's answer. A method-backed or synthetic artifact can set `SourceOwner` to the exact different subject whose unchanged snapshot structure owns its declaration; absent or incorrect ownership remains `DOTNETSP0012`, and conflicting owner requests remain `DOTNETSP0013`.

Folder and namespace evidence must agree when both can establish placement. Rooted, traversing, malformed, missing-root, unknown-role, unknown-slice-kind, insufficient, and conflicting inputs return stable `DOTNETSP####` error diagnostics with the affected source subject; the resolver never chooses one structure heuristically.

Flat compatibility is opt-in per artifact. An adapter may supply a versioned `DotNetSourcePlacementCompatibilityPolicy` containing one exact `ArtifactPlacement`. It is used only when strict derivation fails solely with `DOTNETSP0004`; strict success wins over a valid configured compatibility policy, malformed compatibility is `DOTNETSP0015`, and every other diagnostic remains blocking. Snapshot creation failures `DOTNETSP0009` through `DOTNETSP0011` occur before a placement request exists and cannot be bypassed by compatibility. No filename convention is applied. Each successful result retains defensive snapshots of the strict and optional compatibility policy, the optional source owner, `UsedCompatibilityPlacement`, and the authorizing `CompatibilityReasonCode` so a CLI can report deterministic provenance without reconstructing adapter policy.

### Source-owner candidate composition

Adapters may contribute `ArtifactSourcePlacementCandidateFact` (capability 26) rather than selecting a completed placement themselves. Each candidate identifies an exact artifact role and backend scope, an exact source owner, an independently established `GenerationSliceKind`, a `Self` or `ProducingCommand` reason, and version 1 of `StrongestEvidenceThenEventProduction`. This opt-in does not change existing `ArtifactPlacementFact` behavior.

The shared derivation validates declarations and independent command-to-event `Produces` relationships across adapters, ranks candidates by `EvidenceStrength`, and then collapses distinct producing commands. One strongest producer owns the event's source; zero or several strongest producers use the event's own source. Duplicate sightings of one command do not create multiple producers. Equal-strength semantic kind disagreements and disagreements with effective completed placement evidence block generation. All considered candidate, declaration, production, and backend-scope inputs remain in deterministic lineage.

Set `ScreenplayGenerationOptions.SourcePlacementResolver` to a host-owned `IArtifactSourcePlacementResolver`. `DotNetArtifactSourcePlacementResolver` copies a fixed `DotNetSourceStructureSnapshot`, policies keyed by exact project identity, and optional compatibility policies keyed by exact artifact. It passes the selected owner through the existing strict .NET mapping rules; it does not rediscover source, perform I/O, or relax flat compatibility. Missing mapping, invalid candidates, and conflicting source owners are blocking diagnostics. A null resolver remains supported for runs without candidates.

A composition host references `Cratis.Screenplay.Generation` and `Cratis.Screenplay.Generation.DotNet.Vogen` directly, plus its external ecosystem adapter package. The Vogen adapter package brings `Cratis.Screenplay.Generation.DotNet` and `Cratis.Screenplay.Generation.Contracts` transitively; the analyzed application references Vogen itself.

A clean consumer composes Vogen with explicitly selected ecosystem adapters through one runner invocation:

```csharp
var roster = new DotNetAdapterRegistration[]
{
    DotNetAdapterRegistration.For(new VogenConceptScreenplayAdapter()),
    DotNetAdapterRegistration.For(externalModernAdapter),
    DotNetAdapterRegistration.ForLegacy(unchangedLegacyAdapter)
};

var adapterOptions = new DotNetAdapterOptions();
var snapshot = DotNetAdapterRunner.Run(roster, context, adapterOptions);
var definition = new ScreenplayDefinitionGenerator().Generate(
    snapshot,
    new ScreenplayGenerationOptions { Domain = "Ordering" });
```

`VogenConceptScreenplayAdapter` implements both contracts. Prefer `For(vogen)` for its `Concepts` descriptor, exact Vogen API probe evidence, declared fact capabilities, and stable-source enforcement. Use `ForLegacy(vogen)` only when preserving a legacy host path; run the two registrations separately because their shared `vogen` identity is intentionally a duplicate in one roster. Safely applicable modern and legacy runs produce the same Vogen contribution.

The Vogen contribution establishes authored concepts, supported primitive representations, and one named validation rule only when the attribute-bearing declaration contains an authored static `Validate(TBacking)` method returning the exact `Vogen.Validation` type. The rule keeps the authored predicate and implementation file; a single semantically constant `Validation.Invalid("message")` return can also preserve its message. Arbitrary validation bodies are never translated into built-in rules.

Generated members never provide primary evidence. The adapter never infers identity from `Guid` or `Id`, never treats normalization as validation, and never treats named instances as optional values or defaults. It reports stable loss diagnostics instead: `VOG0001` for unsupported backing representations, `VOG0002` for authored `NormalizeInput(TBacking)` behavior, and `VOG0003` for authored `Vogen.InstanceAttribute` declarations.

## Concepts

.NET adapters can nominate a source type as a concept through `DotNetConceptFacts.Emit(...)`. Nomination requires an authored declaration surface—such as an attribute or an explicit framework registration API—with the adapter's own evidence. Structural guessing from wrapper shape or naming is not sufficient. The helper emits the neutral concept artifact and a representation only for a supported explicitly declared primitive backing; unsupported backings retain the concept nomination without inventing a representation.

Adapters can contribute `ArtifactKind.Concept` together with independently proven `ConceptRepresentationFact`, `ConceptAttributeFact`, and `ConceptValidationRuleFact` assertions. Primitive/enumeration representations, named attributes, and named external predicate rules resolve deterministically and lower to top-level Screenplay concepts without module placement.

`TypeReferenceDefinition.Subject` continues to bind an aggregate artifact property directly when one adapter proves the exact target. Missing, conflicting, unsupported, or same-named concept definitions produce stable diagnostics; generation never falls back to `String`.

## Granular type uses and derivation

Adapters that establish different facets independently can emit `ArtifactDeclarationFact`, `ArtifactMemberDeclarationFact`, `ArtifactMemberTypeUseFact`, and `ArtifactMemberRoleFact` without repeating a complete `ArtifactFact`. `TypeUseDefinition.Shape` orders optional and collection wrappers from outermost to the terminal `Named` node, so optional elements, optional collections, and nested collections remain distinct.

### Versioned printable type-use views

Exact observations stay strict by default: the SDK does not flatten nested collections or treat element nullability as outer optionality. An adapter that needs the legacy one-layer printing behavior explicitly contributes a `TypeUseCompatibilityProjectionFact` alongside the original observation and declares `GenerationFactCapability.TypeUseCompatibilityProjection` (31).

Its definition references the local observation's `FactId` in `TypeUse`, selects `Profile = TypeUseCompatibilityProfile.OneLayerCollection` and `Version = 1`, and carries a separate printable `TypeUseDefinition` in `Type`. `SourceNodeIndex` is the zero-based index of the source symbol being printed. Version 1 consumes outer optionality and at most one collection; it does not recursively unwrap the selected symbol.

`TypeUseDefinition.Nodes` is optional supplemental evidence aligned with every node of the exact `Shape`. Each node records its source name, optional subject, and optional backend scope. When supplied, the entire sequence must be present and its last node must agree with the original terminal name, subject, and scope. Intermediate selection requires this evidence; selection of the original terminal can use the existing terminal fields without `Nodes`. Missing supplemental evidence on another equivalent observation is not a competing type assertion; conflicting supplied evidence is a conflict.

The .NET opt-in helper observes Roslyn symbols in one traversal and returns both views without changing `DotNetTypeShapes.TypeUseFor` or `TypeReferenceFor`:

```csharp
var id = new FactId { Value = "adapter:member-type" };
var result = DotNetTypeUseCompatibility.Project(symbol, context, id);

var observation = new ArtifactMemberTypeUseFact
{
    Id = id, Subject = owner, Evidence = evidence,
    Definition = new() { Member = member, Type = result.Observation }
};
var projection = new TypeUseCompatibilityProjectionFact
{
    Id = new() { Value = "adapter:member-view" },
    Subject = owner, Evidence = evidence,
    Definition = result.Projection
};
```

Here `symbol`, `context`, `owner`, `member`, and exact `evidence` are the adapter's existing source-analysis values. Contribute both facts, not a replacement observation. For example, `List<List<Value>>` retains `Collection → Collection → Named` and the terminal `Value` subject, but prints the intermediate `List` with one collection flag. It must never bind to the hidden `Value` concept.

For scoped source types, pass the optional fourth `scopeForType` callback (`Func<INamedTypeSymbol, BackendScopeKey?>`). The helper calls it only for authored symbols with an observed subject and consistently records its result on that node, the exact terminal when applicable, and the selected printable type. The callback can return different scopes for a container and its element. Existing three-argument calls remain unscoped. Do not patch only `Observation.ObservedScope`: supplied terminal node evidence must agree with it.

Array symbols have no legacy printable `Name`. Their nodes retain that name rather than substituting `ToDisplayString()`; compatibility evidence containing an unnamed array node is rejected, including `List<int[]>`, instead of guessing an identity or flattening another collection. The existing exact `TypeUseFor` path remains available for directly representable array shapes.

| Source type | Exact shape (outermost first) | Version 1 printable type |
| --- | --- | --- |
| `string` | `Named` | `String` |
| `string?` | `Optional, Named` | `String optional` |
| `int?` | `Optional, Named` | `Int optional` |
| `List<string>` | `Collection, Named` | `String[]` |
| `List<string>?` | `Optional, Collection, Named` | `String[] optional` |
| `List<string?>` | `Collection, Optional, Named` | `String[]` |
| `List<string?>?` | `Optional, Collection, Optional, Named` | `String[] optional` |
| `List<int?>` | `Collection, Optional, Named` | `Nullable[]` |
| `List<int[]>` | `Collection, Collection, Named` | No legacy printable name; rejected |
| `List<List<string>>` | `Collection, Collection, Named` | `List[]` |
| `List<Container<Value>>` with an authored enumerable container | `Collection, Collection, Named` | `Container[]`, using the container's subject |

The selected node's subject and scope govern binding before lowering; the original terminal never substitutes for an intermediate symbol. Concept-only binding remains a separate opt-in: contribute `TypeUseBindingPolicyFact` against the original observation to combine the view with concept-only matching and unbound fallback. Declaration-input transfer preserves the exact type and node evidence, emits a separate endpoint projection, and includes the original observation and projection in transferred lineage.

Projection references must match the observation's adapter identity, owner subject, exact evidence, and source location. A reused fact ID from another adapter receives no projection. Unknown versions/profiles, missing or foreign references, duplicate views, invalid printable flags, and mismatched source-node evidence fail closed with located diagnostics (`GEN0090` on raw generation). Different exact assertions remain conflicts even when their printable views agree. No profile, scope, identity, or primitive fallback is guessed.

### Opt-in jagged array view

An array of arrays such as `int[][]` has no version 1 printable name, so an exact observation of it still reports `GEN0049`. A .NET adapter that wants the legacy printable reference (an unnamed collection, with only the outer optionality and no target) for such a member calls `DotNetTypeUseCompatibility.TryCreateLegacyJaggedArrayMember` with the original property symbol and contributes the returned `ArtifactMemberTypeUseDefinition` as the member's type use. It sets `UseLegacyJaggedArrayReference` and records `CollectionOrigins` read from the symbols (one entry per `Collection` step, outermost first); the request alone is never evidence, and `List<int[]>`, `List<List<int>>`, a single array and a lone multidimensional array such as `int[,]` are not marked, while an array nested with a multidimensional array (`int[,][]`, `int[][,]`) is marked, matching the legacy reference, because rank is not checked. Missing, misaligned, unknown or non-array-of-array origins, or a same-observation compatibility projection, are rejected with `GEN0090`. The member stays in the application graph; whether its artifact prints still follows the existing lowering rules. This is a printing view only: `Project` version 1 still rejects unnamed nodes and no language extension is added.

### Binding exact observations

Generation runs the built-in `cratis.screenplay.type-use-binding@1.0.0` rule once over one fixed admitted base snapshot. It joins only exact artifact keys and subjects, never display names, adapter IDs, registration order, Roslyn symbols, or another adapter instance. A derived `TypeUseBindingFact` retains canonical input `FactId` values and complete evidence in `GenerationFactLineage`; resolved compatibility references retain the exact target role in `TypeReferenceDefinition.TargetArtifactKind`. Conflicting, incomplete, foreign-owned, malformed, or unsupported inputs omit the affected artifact without a winner, flattened fallback, or partial output.

When an adapter deliberately contributes no target for an authored member type (for example, an unsupported flags enum), emit `TypeUseTargetOmittedFact` alongside that member's `ArtifactMemberTypeUseFact`. Its `Definition.Member` uses the same `ArtifactMemberKey`; `Definition.Diagnostic` carries the adapter's warning and source location. Declare `GenerationFactCapability.TypeUseTargetOmitted` in the adapter descriptor. Admission requires the same producer and member subject, an observed type subject, and valid diagnostic references. The omission cannot waive another adapter's type use.

When no target is declared, generation preserves the observed subject as provenance and keeps the unbound member's name and supported optional/collection shape. An adapter can set `TypeUseDefinition.Name` to an underlying primitive such as `Int` while retaining `ObservedTypeSubject`. The diagnostic is reported once, without a missing-target or incomplete-member error. A declared target takes precedence: normal binding applies and the omission warning is not reported. Missing targets without an omission remain errors; unsupported shapes and other conflicts are unchanged. Declaration-input types use the separate declaration-owned omission contract below; missing targets without that evidence still report GEN0045 (`MissingTypeUseTarget`).

An adapter can make its own concept declaration yield on an ambiguous concept name by setting `ArtifactDeclarationDefinition.NameConflictYield` to a `ConceptNameYieldDefinition` carrying its omission `Diagnostic`. This property belongs to the declaration itself: it cannot refer to another producer's declaration or a declaration in another contribution. Admission requires a concept, a valid diagnostic with `Outcome = Conflict` or `Outcome = Unsupported` naming the declaration's subject, and a `Facts` reference to that declaration's own scoped `FactId`. The omission diagnostic's outcome is preserved, never rewritten. The diagnostic and its references are frozen with the contribution; no additional capability is needed beyond `ArtifactDeclaration`.

Before type-use binding, a name claimed by distinct concept subjects causes marked concepts in that group to yield, including when all concepts yield. If an identical, non-yielding declaration with the same artifact key retains the concept, its marked declaration stays too and reports no omission warning. Any remaining non-yielding name conflict still reports GEN0015. Admitted member type uses that remain unbound to a concept also reserve their authored type name: a yielding concept with that name is omitted with the same once-only diagnostic. Exact concept bindings are excluded from reservations, so a concept never clashes with its own bound uses. `ArtifactFact` properties reserve names only when unbound: a subjectless property whose member type use binds to a concept is not a reservation. Non-yielding declarations keep their existing behavior, and a marked concept with no clash is kept without a warning. Co-owners of the same yielded declaration (the full artifact key, including backend scope) report one diagnostic when code, severity, outcome, subject, source location, and message match. Its facts and lineage are the canonical, order-independent union of every contributing producer's local declaration, support facts, and evidence; fact IDs remain unique per producer. Different declarations or diagnostic payloads remain separate. Authored member types targeting a yielded declaration use the intentional omission path above, preserving their observed subject, terminal name, and supported optional/collection shape without binding to a different same-named concept. The selection and fallback derivation retain the original declaration and evidence as provenance and are independent of adapter order. Other structural conflicts and unsupported shapes remain errors.

Declaration-owned inputs use `DeclarationInputMemberTargetOmittedFact` and capability `DeclarationInputMemberTargetOmitted` (32), alongside the exact same-producer `DeclarationInputMemberTypeUseFact`. Its `DeclarationMemberKey` includes the declaration, member name and backend scope; its warning identifies the same subject and source. On transfer, the core preserves the authored name, full optional/collection shape and exact observed subject without inventing a binding. Derived omission evidence retains declaration/type-use/omission lineage, and the original warning is published once. Cross-producer, cross-member, cross-scope and malformed omissions never waive a missing target; an exact declared target still binds normally.

A producer can opt one exact type observation into concept-only binding with `TypeUseBindingPolicyFact` and capability `TypeUseBindingPolicy` (30). `Definition.TypeUse` references the local `ArtifactMemberTypeUseFact` or `DeclarationInputMemberTypeUseFact` by `FactId`; `Definition.Policy` is `TypeUseBindingPolicyKind.ConceptOnlyWithUnboundFallback` (silent) or `TypeUseBindingPolicyKind.ConceptOnlyWithUnboundFallbackAndMissingConceptDiagnostic` (reporting). Admission freezes the reference and requires a unique policy, the same producer, subject and source, and an exact observed type subject. Unknown policies, dangling or foreign references, duplicate policies, and references to other fact families are rejected (raw snapshots report GEN0085).

Concept-only observations bind only an exact concept subject in the observed backend scope, never an event, record, or another same-named concept. If no concept survives selection, or a bound concept cannot be lowered, generation keeps the authored printable name and supported optional/collection wrappers without a missing-target or missing-concept error. The reporting policy behaves identically but additionally emits the unlocated GEN0016 warning (same message, severity and outcome as the legacy diagnostic, once per target subject) when a selected declared concept cannot be emitted; the owner is kept with its printable fallback. It reports nothing for undeclared types, non-concept targets, emitted concepts or intentionally yielded concepts, and the warning carries no observation location. Every observation of a co-observed member must use the reporting policy; a mix with the silent policy stays silent. Declaration-input transfer preserves the reporting policy. Existing omission and unsupported-concept warnings retain their original source and outcome. Explicit contradictory targets, incompatible type uses, unsupported shapes and backend-scope collisions still fail normally. All observations for a co-observed member must opt in; one producer's policy never rewrites or waives another producer's facts. Declaration-input transfer synthesizes a policy for the endpoint observation and retains the original policy in canonical lineage. Adapters without a policy keep their existing binding behavior.

.NET adapters keep compatibility aggregate properties unbound with `DotNetTypeShapes.PropertiesOf(type)` and append `DotNetTypeUseFacts.Emit(...)`. `TypeUseFor(...)` preserves exact nested use-site shape and terminal source subject; an optional role callback emits only roles explicitly established by source-framework semantics. `EventSourceIdentifier` lowers through the existing identifier syntax, while the distinct ordinary `Identifier` role remains provenance until Screenplay owns separate syntax for it. Non-.NET and source-independent frontends contribute the same contracts directly.

Concept validation stays independent from identity, representation, attributes, and optionality. A rule uses an adapter-authored `RuleIdentity` for deterministic resolution, while `Predicate` is the authored predicate name emitted by lowering. Adapters contribute framework-neutral data and provenance only; they never reference Screenplay syntax:

```csharp
var accountNumber = new SubjectId { Value = "dotnet://Banking/Concepts.AccountNumber" };
var validation = new ConceptValidationRuleFact
{
    Id = new FactId { Value = "account-number:validation:format" },
    Subject = accountNumber,
    Definition = new ConceptValidationRuleDefinition
    {
        Concept = accountNumber,
        RuleIdentity = "format",
        Kind = ConceptValidationRuleKind.NamedPredicate,
        Predicate = "BeValidAccountNumber",
        Message = "Must be a valid account number",
        ImplementationFile = "Concepts/Validation/BeValidAccountNumber.cs"
    },
    Evidence = new Evidence
    {
        Adapter = new AdapterIdentity { Id = "my-adapter", Version = "1.0.0" },
        Strength = EvidenceStrength.Exact
    }
};
```

Multiple adapters can assert the same definition with separate evidence. Incompatible definitions with the same `(Concept, RuleIdentity)` remain visible as a conflict without erasing the concept's representation or attributes. The nullable kind-specific `Predicate` keeps the transport record additive: named-predicate rules require it today, while later rule kinds can add typed operands without forcing unrelated placeholder values.

## Typed fail-closed outcomes

Every public fact discriminator has an explicit `Unknown = -1` sentinel without changing any existing numeric value. Unknown or undefined artifact, placement, slice, relationship, evidence, representation, primitive, attribute, or validation-rule values are diagnosed before resolution and only the affected fact is omitted. They never fall through to another semantic role or disappear silently.

`GenerationDiagnostic.Outcome` independently classifies `Unknown`, `Conflict`, and `Unsupported` results while preserving stable codes, severity, source range, subject, and all incompatible resolver variants. The nullable additive property keeps older producers and consumers binary compatible. Valid unrelated facts continue to generate deterministic compiler-verified Screenplay output.

### Convention-altering extension diagnostics

When an authored framework extension can alter discovery or runtime conventions beyond what an adapter can prove statically, adapters use a stable adapter-owned diagnostic code and `GenerationDiagnosticOutcome.Unsupported`. `Subject` identifies the authored extension type. The message names both the exact hook surface and the affected convention scope, using this shape: `Authored extension '<type>' uses '<hook surface>' and may alter <scope>; the recovered model reflects default <scope> only.` This distinguishes bounded default recovery from an unrecognized framework generation without introducing a new diagnostic outcome.

## Build and test

The specifications use Cratis.Specifications 5.0 and the native xUnit v3 executable runner. The .NET 11 RC SDK is required to build. The three dedicated specification projects default to .NET 11 in Debug and Release and also run on .NET 10; the four libraries target net10.0 and net11.0 in Release and remain libraries.

```shell
dotnet build Screenplay.Generation.slnx --configuration Debug
bash .github/scripts/run-specifications.sh Debug
dotnet build Screenplay.Generation.slnx --configuration Release -p:Version=9999.0.0
bash .github/scripts/run-specifications.sh Release
dotnet build Screenplay.Generation.slnx --configuration Release -p:Version=9999.0.0 -p:SpecTargetFramework=net10.0
bash .github/scripts/run-specifications.sh Release net10.0
dotnet pack Screenplay.Generation.slnx --no-build --configuration Release -o Artifacts/NuGet -p:Version=9999.0.0
./scripts/verify-package-consumers.sh 9999.0.0 Artifacts/NuGet
```

The runner executes all three specification projects in parallel and writes fresh logs and TRX files under `TestResults/`. Missing projects or executables, nonzero child exits, and missing, invalid, or zero-test TRX results fail the run. Run `bash .github/scripts/run-specifications.sh --self-test` to exercise these guards (Node.js required); validating TRX results requires Python 3.

Run one project directly (append `.exe` on Windows):

```shell
Source/DotNET/Generation.Specs/bin/Debug/net11.0/Cratis.Screenplay.Generation.Specs
```

Use `-class`, `-method`, or `-namespace` with fully qualified names to filter tests; `-?` lists all options. For example:

```shell
Source/DotNET/Generation.Specs/bin/Debug/net11.0/Cratis.Screenplay.Generation.Specs -namespace 'Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator*'
```

`dotnet test Screenplay.Generation.slnx --configuration Debug --no-build` and IDE test explorers remain supported through the VSTest adapter; no Microsoft.Testing.Platform configuration is required. For Release, use `--configuration Release`.

Package validation runs during pack against the latest released API baseline, `0.16.0`, for all four packages. Baseline strict mode remains disabled so intentional compatible additions are accepted while removals and signature changes still fail; only the intentional removal of the .NET 8 and .NET 9 package targets is suppressed in the compatibility suppression files. The sentinel version must be applied to both the Release build and the no-build pack so package and assembly versions agree. The consumer smoke keeps clean legacy binaries compiled against the `0.1.0` core and `0.5.0` Vogen ancestry and runs them unchanged with current packages. A separate clean current-source consumer compiles only against the candidate packages and verifies the current authored-source, shared symbol helpers, declared concept nomination, neutral-fact, resolver, Vogen, adapter-composition, and deterministic compiler-verified generation APIs.

All builds require zero errors and zero warnings. Generated Screenplay output must compile and remain stable through print/compile/print.

## The Cratis ecosystem

This project is part of [Cratis](https://www.cratis.io) — free, MIT-licensed tools for building event-sourced and CQRS applications.

- **[Chronicle](https://github.com/Cratis/Chronicle)** — event-sourcing database and runtime. Orleans-based kernel, pluggable storage (MongoDB default; PostgreSQL, SQL Server, SQLite, in-memory), language-agnostic gRPC contracts. [Docs](https://www.cratis.io/chronicle/)
- **Chronicle clients** — first-class [.NET SDK](https://github.com/Cratis/Chronicle), plus [TypeScript](https://github.com/Cratis/Chronicle.TypeScript), [Kotlin/Java](https://github.com/Cratis/Chronicle.Kotlin), and [Elixir](https://github.com/Cratis/Chronicle.Elixir); [Python](https://github.com/Cratis/Chronicle.Python) coming soon (pre-alpha). AI agents connect through the [Chronicle MCP server](https://github.com/Cratis/Chronicle.Mcp).
- **[Arc](https://github.com/Cratis/Arc)** — opinionated CQRS framework for ASP.NET Core with commands, queries, validation, authorization, and TypeScript proxy generation. Works without event sourcing. [Docs](https://www.cratis.io/arc/)
- **[Components](https://github.com/Cratis/Components)** — React components aligned with Arc patterns. [Docs](https://www.cratis.io/components/)
- **[CLI](https://github.com/Cratis/cli) + Workbench** — inspect and diagnose Chronicle from the terminal or the browser. [Docs](https://www.cratis.io/cli/)
- **Model-first layer (experimental)** — [Studio](https://github.com/Cratis/Studio), [Screenplay](https://github.com/Cratis/Screenplay), [Stage](https://github.com/Cratis/Stage), [Scene](https://github.com/Cratis/Scene), [Prologue](https://github.com/Cratis/Prologue)
- **Supporting** — [Fundamentals](https://github.com/Cratis/Fundamentals), [Specifications](https://github.com/Cratis/Specifications), [Synopsis](https://github.com/Cratis/Synopsis), [Lens](https://github.com/Cratis/Lens), [Narrator](https://github.com/Cratis/Narrator), and free [AI tooling](https://github.com/Cratis/AI) (preview); [Ensemble](https://github.com/Cratis/Ensemble) coming soon (pre-release)
- **[Samples](https://github.com/Cratis/Samples)** — runnable event sourcing and CQRS samples for the whole stack

Everything Cratis publishes today is MIT licensed and free to use.

## License

Screenplay.Generation is licensed under the [MIT license](LICENSE).
