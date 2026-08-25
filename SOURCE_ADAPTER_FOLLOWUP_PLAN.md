<!--
Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information.
-->

# Source adapter follow-up plan

## Status

**Design only — release-blocked. No implementation is claimed by this document.**

This plan covers Screenplay handover items 3 and 4:

1. release-gated adoption of the shared .NET source-placement APIs by Arc, Critter Stack, and the CLI; and
2. completion of neutral executable-specification recovery in Arc.

Do not begin downstream package upgrades or source adoption until the required public packages and adapter releases have been verified. This document does not authorize edits to source, workflows, package manifests, or existing research and status documents.

## Verified starting point

The plan is based on the current repository status files and the completed read-only handover in:

- `Screenplay/SCREENPLAY_HANDOVER.md`, follow-up items 3 and 4;
- `Screenplay/PROMPT-screenplay-continuation.md`;
- `Screenplay.Generation/IMPLEMENTATION_STATUS.md`;
- `Screenplay.Generation/COMPATIBILITY.md`; and
- `Screenplay.Generation/Documentation/guides/build-source-adapter.md`.

The relevant status is:

- Screenplay Generation `0.10.1` is the latest verified public release.
- Generation PR #32 is merged at `d0b68c3476991ab50446fc1ab45ddf0efbb2f372`. It contains fixed source snapshots, explicit application/specification project roles, shared placement derivation, compatibility option mapping, and `DOTNETSP####` fail-closed diagnostics.
- That post-`0.10.1` capability is on `main` but is not yet a verified public package release. This is the first blocker.
- Arc PR #2602 remains the separately release-gated neutral specification baseline at `a2791a8798971d714f307836868e0d3430abbe89`. It proves exact bare rejection recovery and blocks event-predicate shapes it cannot retain. Its release must be verified before follow-up changes are based on it.
- Critter Stack `v0.21.0` and CLI `v2.17.0` still consume Generation `0.9.0` packages according to their current manifests and status files.
- Generation issue #26 owns shared placement adoption and compatibility-facade parity. Generation issue #25 owns complete success, event-predicate, read-model, and query specification recovery.

Release versions after `0.10.1` are intentionally not predicted here. In commands and acceptance records, use the exact verified public version as `GENERATION_VERSION`, the corresponding Arc release as `ARC_VERSION`, and the corresponding Critter Stack release as `CRITTER_VERSION`.

## Scope boundaries

### In scope

- Public-release verification for the merged Generation placement API.
- Shared placement adoption in Arc and Critter Stack.
- CLI establishment and propagation of project role and source-structure policy.
- CLI option and provenance parity for placement-affecting choices.
- Exact recovery of generated-style Arc success, rejection, projection/read-model, and query specifications.
- Determinism, compatibility, atomic fail-closed behavior, package-consumer validation, and public release gates.

### Out of scope

- New Screenplay syntax or semantic nodes.
- Saga syntax or framework/storage/transport vocabulary in neutral contracts.
- Realization-report work owned by Generation #24.
- Bidirectional render/recover conformance owned by Screenplay #148, except for preserving vectors that later work will reuse.
- Runtime execution, application startup, database access, arbitrary predicate execution, or loading untrusted adapter binaries.
- Frontend, deployment, attachment, AI, Studio, or Prologue work.
- Package-version guesses, dependency changes before release, or edits to historical handovers and research papers.

## Non-negotiable contracts

1. Screenplay remains the semantic authority. Source adapters contribute evidence, provenance, typed facts, and diagnostics only.
2. Source layout never determines semantic role. The adapter proves `ArtifactKind` and `GenerationSliceKind` before asking the shared placement pipeline for module, feature, and slice placement.
3. The host owns workspace loading, authoritative authored trees, `DotNetProjectRole`, source identity/display policy, source-structure policy, provider admission, and output.
4. Adapters own framework semantics. Generation owns deterministic resolution, admission, lowering, canonical printing, compiler verification, and print/compile/print stability.
5. `SourceRange.Path` is display provenance, not identity. `SourceRange.FileIdentity` is stable, project-qualified identity. Physical checkout roots, absolute project paths, and Roslyn physical paths never enter stable provenance.
6. Existing discriminator values and diagnostic meanings are immutable. Additions must be additive; `Unknown = -1` remains reserved.
7. Unknown, generated-only, computed, conditional, repeated, ambiguous, conflicting, or partially understood scenarios fail closed. No broader query, guessed event value, guessed target, guessed placement, or partial scenario may be lowered.
8. Adapter, project, fact, and syntax-tree ordering must not affect facts, diagnostics, generated bytes, or provenance.
9. Legacy callers without the new source metadata retain their documented compatibility behavior. New project-aware paths are strict and fail closed.
10. Every adapter diagnostic is surfaced by the CLI. Errors block output/publication; warnings and information are never hidden.

## Exact dependency order

```text
A. Verify and publish the merged Generation placement capability
   |
   +--> B. Verify and release Arc PR #2602 unchanged as the neutral rejection baseline
   |
   +--> C1. Adopt shared placement in Arc and release Arc
   |
   +--> C2. Adopt shared placement in Critter Stack and release Critter Stack
              (C1 and C2 both require A; execute them serially to keep parity evidence reviewable)
   |
   +--> D. Upgrade CLI to the exact released Generation, Arc, and Critter Stack set;
           establish roles/options/provenance; release CLI
   |
   +--> E. Complete Arc neutral specification recovery on the released placement/host contract
   |
   +--> F. Run cross-repository render-to-recover evidence and close only the delivered
           portions of Generation #25/#26
```

The required serial execution order is **A → B → C1 → C2 → D → E → F**. C1 and C2 are technically independent after A, but must not be merged simultaneously: each needs a frozen before/after compatibility record, and the CLI must consume both exact released adapter versions rather than local builds.

If E exposes a missing neutral contract or lowerer capability, stop and insert an additive Generation patch/minor before E continues:

```text
Generation red contract/spec -> Generation release -> Arc package upgrade -> Arc recovery resumes
```

Never work around a missing released API by copying Generation source into Arc, Critter Stack, or the CLI.

## A. Generation release gate

### Owning files

The already-merged implementation is owned by:

- `Source/DotNET/Generation.DotNet/DotNetProjectCompilation.cs`
- `Source/DotNET/Generation.DotNet/DotNetSourceStructure.cs`
- `Source/DotNET/Generation.DotNet/DotNetSourceStructures.cs`
- `Source/DotNET/Generation.DotNet/DotNetSourcePlacement.cs`
- `Source/DotNET/Generation.DotNet/IDotNetScreenplayAdapter.cs`
- `Source/DotNET/Generation.DotNet.Specs/for_DotNetSourceStructures/**`
- `Source/DotNET/Generation.DotNet.Specs/for_DotNetSourceStructureResolver/**`
- `Source/DotNET/Generation.DotNet.Specs/for_DotNetSourcePlacementDerivation/**`
- `Source/DotNET/Generation.DotNet.Specs/for_DotNetAdapterOptions/when_getting_source_structure_policy.cs`
- `scripts/verify-package-consumers.sh`

No new downstream work starts by changing these files. First verify the merge as a public lockstep package release.

### Required release evidence

1. Run the current full Generation gate from a clean release worktree:

    ```bash
    dotnet test Screenplay.Generation.slnx --configuration Debug
    dotnet build Screenplay.Generation.slnx --configuration Release -p:Version=9999.0.0
    dotnet pack Screenplay.Generation.slnx --no-build --configuration Release \
      -o Artifacts/NuGet -p:Version=9999.0.0
    ./scripts/verify-package-consumers.sh 9999.0.0 Artifacts/NuGet
    ```

2. Verify warning-free `net8.0`, `net9.0`, and `net10.0` Release output.
3. Verify all four package-validation packs against public `0.10.1`.
4. Verify the legacy core `0.1.0` and Vogen `0.5.0` binaries run unchanged.
5. Verify the clean current-source consumer against only the candidate packages.
6. After publication, verify the tag and merge ancestry include PR #32, the repository signature, package repository metadata, and SHA-256 for all four lockstep packages.
7. Set all four downstream Generation references to exactly that one public `GENERATION_VERSION`. Do not use a local feed for adoption evidence.
8. Advance the package-validation baseline only through the normal release change after the public version is verified.

### Existing red vectors that must remain green

- missing source context or source mapping;
- unknown project role or slice kind;
- invalid/traversing feature root;
- folder/namespace disagreement;
- conflicting partial declarations;
- duplicate project-qualified subjects;
- mismatched placement subject;
- conflicting requests for one artifact;
- unknown artifact kind;
- null versus empty policy values in deterministic request keys;
- project, request, and syntax-tree permutation;
- physical checkout relocation; and
- casing-preserving display placement independent from identity case folding.

Any regression keeps A blocked.

## B. Arc neutral rejection baseline release

Before shared placement or broader specification recovery, release the existing Arc PR #2602 baseline without mixing follow-up work into it.

### Baseline-owned files

- `Directory.Packages.props`
- `Source/DotNET/Screenplay/Screenplay.csproj`
- `Source/DotNET/Screenplay.Specs/Screenplay.Specs.csproj`
- `Source/DotNET/Screenplay/Analysis/Specifications/**`
- `Source/DotNET/Screenplay/Generation/ArcSpecification*.cs`
- `Source/DotNET/Screenplay.Specs/for_ArcSpecificationFactAdapter/when_analyzing_an_exact_rejection_scenario.cs`
- `Source/DotNET/Screenplay.Specs/for_ArcSpecificationFactAdapter/when_analyzing_event_predicate_values.cs`
- `Documentation/generating-a-screenplay.md`

### Baseline gate

- Arc PR checks are green.
- The full Arc Screenplay suite passes; the handover baseline is 1,288 full specs and 17 focused adapter specs.
- Release builds for `net8.0`, `net9.0`, and `net10.0` are warning-free.
- The sentinel `Cratis.Arc.Screenplay` package packs and validates.
- Markdown lint and documentation link verification pass.
- The public Arc package version, merge commit, repository metadata, and package SHA-256 are recorded.

Generation #25 remains open after B. Bare rejection is a baseline, not complete specification recovery.

## C1. Arc shared source-placement adoption

### Arc red vectors

Add failing focused specifications before replacing compatibility placement. The planned spec owners are:

- `Source/DotNET/Screenplay.Specs/for_ArcSpecificationFactAdapter/when_deriving_shared_source_placement.cs`
- `Source/DotNET/Screenplay.Specs/for_ArcSpecificationFactAdapter/when_source_folder_and_namespace_placements_conflict.cs`
- `Source/DotNET/Screenplay.Specs/for_ArcSpecificationFactAdapter/when_the_specification_and_target_are_in_different_projects.cs`
- `Source/DotNET/Screenplay.Specs/for_ArcSpecificationFactAdapter/when_project_and_fact_order_is_reversed.cs`
- `Source/DotNET/Screenplay.Specs/for_ScreenplayGenerator/when_preserving_legacy_placement_options.cs`

The vectors must prove:

1. exact existing module/feature/slice output for default options;
2. exact parity for configured module and skipped namespace segments;
3. namespace-root module behavior remains byte-identical where the compatibility facade supports it;
4. an application target and a scenario in a `Specifications` project resolve to the target's application placement;
5. folder/namespace conflict emits `DOTNETSP0005`, emits no placement for the target, and admits/lowers no dependent scenario;
6. missing context, missing mapping, duplicate subject, unsupported role, and subject mismatch preserve typed diagnostics and source/subject evidence;
7. reversed project, fact, and syntax-tree order is byte-identical; and
8. relocating the checkout changes neither identity nor generated bytes.

### Arc implementation owners

- `Source/DotNET/Screenplay/Generation/ArcSpecificationFactAdapter.cs`
    - Create one `DotNetSourceStructures.Create(context)` snapshot per analysis.
    - Retain every snapshot diagnostic.
    - Collect all placement requests and call `DotNetSourcePlacementDerivation.Derive(...)` once.
- `Source/DotNET/Screenplay/Generation/ArcSpecificationArtifactFacts.cs`
    - Replace `AddPlacement()` namespace slicing with request collection and successful shared placement facts.
    - Preserve existing fact identity and evidence strength unless an additive migration is explicitly required.
- `Source/DotNET/Screenplay/Generation/ArcSpecificationFactBuilder.cs`
    - Require the exact target placement before a scenario can be admitted for lowering.
- `Source/DotNET/Screenplay/ScreenplayOptions.cs`
    - Map existing `Module`, `SegmentsToSkip`, and namespace-root behavior to a `DotNetSourceStructurePolicy` without changing legacy option semantics.
- `Source/DotNET/Screenplay/ScreenplayGenerator.cs` and `Source/DotNET/Screenplay/IScreenplayGenerator.cs`
    - Add or use a project-aware compatibility path carrying `DotNetProjectCompilation`; do not break the existing compilation-only overload.
- `Directory.Packages.props` and the Screenplay project/spec project package references
    - Upgrade only to the verified public `GENERATION_VERSION`.

### Compatibility parity gate

Freeze source output from the released B package for all existing Screenplay generator specifications. After adoption:

- legacy compilation-only calls produce identical source and diagnostics;
- project-aware calls differ only by newly available stable source identity/provenance or by an intentional typed `DOTNETSP####` rejection;
- no existing `ScreenplayOptions` combination silently changes placement; and
- the full Arc baseline, focused vectors, multi-TFM Release builds, sentinel pack, docs, and package validation pass.

Only then release Arc and record `ARC_VERSION`.

## C2. Critter Stack shared source-placement adoption

### Critter Stack red vectors

Add focused placement specifications under:

- `Source/DotNET/CritterStack.Specs/for_CritterStackScreenplayAdapter/when_deriving_shared_source_placement.cs`
- `Source/DotNET/CritterStack.Specs/for_CritterStackScreenplayAdapter/when_source_placement_conflicts.cs`
- `Source/DotNET/CritterStack.Specs/for_CritterStackScreenplayAdapter/when_project_and_syntax_tree_order_is_reversed.cs`
- `Source/DotNET/CritterStack.Specs/for_CritterStackScreenplayGenerator/when_preserving_placement_option_compatibility.cs`

Cover at least one independently proven role of each kind:

- Marten state view and event placement;
- Wolverine state change;
- Wolverine automation/translation;
- Wolverine query/read model; and
- an artifact whose semantic feature name differs from a short type name.

### Critter Stack implementation owners

- `Source/DotNET/CritterStack/CritterStackScreenplayAdapter.cs`
    - Own the single fixed source snapshot, complete request set, one derivation call, and diagnostic retention.
- `Source/DotNET/CritterStack/Marten/MartenFacts.cs`
    - Replace `PlacementFor()` and `EventPlacementFor()` only where shared source structure can preserve the proven Marten placement.
- `Source/DotNET/CritterStack/Wolverine/WolverineFacts.cs`
    - Replace `BehaviorPlacement()` only after semantic slice kind and exact subject are established.
- `Source/DotNET/CritterStack/CritterStackScreenplayGenerator.cs`
    - Pass the complete `DotNetAdapterOptions.SourceStructurePolicy` through the compatibility facade.
- `Source/DotNET/CritterStack/CritterStackScreenplayOptions.cs`
    - Preserve existing module/namespace-skip behavior and add no incompatible positional parameter.
- `Directory.Packages.props`
    - Upgrade all four Generation packages together to public `GENERATION_VERSION`.

Do not delete semantic helpers merely because placement becomes shared. `StateFeature(...)` and framework-specific role discovery remain adapter concerns; only placement derivation moves to Generation.

### Canonical compatibility gate

Before implementation, hash every tracked `Integration/Canonical/*.expected` file. After implementation:

- every existing canonical output remains byte-identical;
- package/provider/diagnostic provenance remains equivalent;
- any intentional new `DOTNETSP####` diagnostic has a dedicated red vector and does not alter unrelated facts;
- `scripts/verify-canonical.sh` passes;
- Debug specs pass;
- Release restore/build is warning-free;
- a sentinel package packs and validates; and
- `scripts/verify-package-consumer.sh 9999.0.0 ./Artifacts/NuGet` passes with an isolated package cache.

Use the repository workflow-equivalent sequence:

```bash
dotnet restore --property:Configuration=Debug
dotnet test --configuration Debug --no-restore
dotnet restore --property:Configuration=Release
dotnet build --no-restore --configuration Release -p:Version=9999.0.0
dotnet pack --no-build --configuration Release -o ./Artifacts/NuGet -p:Version=9999.0.0
./scripts/verify-package-consumer.sh 9999.0.0 ./Artifacts/NuGet
./scripts/verify-canonical.sh
```

Only then release Critter Stack and record `CRITTER_VERSION`.

## D. CLI host, options, and provenance adoption

The CLI must consume public packages only: `GENERATION_VERSION`, `ARC_VERSION`, and `CRITTER_VERSION`.

### CLI red vectors

Add or extend specifications under these owners:

- `Source/Cli.Specs/for_ScreenplayCompilationLoader/**`
- `Source/Cli.Specs/for_ScreenplayProjectSources/**`
- `Source/Cli.Specs/for_ArcScreenplayGeneration/when_mapping_projects/**`
- `Source/Cli.Specs/for_CritterStackScreenplayGeneration/when_mapping_projects/**`
- `Source/Cli.Specs/for_ProviderScreenplayGeneration/when_selecting/**`
- `Source/Cli.Specs/for_ScreenplayDiagnosticsWriter/**`
- `Source/Cli.Specs/for_GenerateScreenplaySettings/**`

Required vectors:

1. a direct application project is `DotNetProjectRole.Application`;
2. a direct specification project is rejected unless its target application is also selected explicitly;
3. a solution retains admitted application and specification projects and marks each role explicitly;
4. a spec project name is not sufficient authority by itself: the host also requires exact framework/specification evidence and an unambiguous referenced application target;
5. missing, extra, or reordered project source/role metadata fails closed rather than indexing the wrong project;
6. Arc and Critter providers receive the same authored trees, source context, role, selected TFM, and logical project identity the loader reported;
7. direct-project and solution display roots preserve current behavior;
8. physical roots never appear in text or machine-readable provenance;
9. `--module`, `--skip-segments`, and `--modules-from-namespace-roots` either map exactly to the selected provider's shared policy or report `CLI0014` without applying the option;
10. if `--feature-root <PATH>` is approved as the host surface, it accepts only a safe project-relative path, is reported verbatim in source-structure provenance, and reports `CLI0014` for providers that do not advertise it; and
11. repeated invocation and relocated checkout produce identical `.play` bytes and stable provenance.

`--feature-root` is a design checkpoint, not an implementation claim. Do not add it merely to expose every SDK property. Approve it only if a public fixture requires a non-root source tree and the provider capability contract can advertise support without guessing.

### CLI implementation owners

- `Source/Cli/Commands/Screenplay/ScreenplayCompilationLoader.cs`
    - Load both application and admitted specification projects for source recovery.
- `Source/Cli/Commands/Screenplay/ScreenplayProjectSelection.cs`
    - Replace unconditional spec-project exclusion with explicit role classification and conservative admission.
- `Source/Cli/Commands/Screenplay/LoadedCompilation.cs`
    - Keep compilations, names, authored trees, project source metadata, roles, and provenance aligned atomically.
- `Source/Cli/Commands/Screenplay/ScreenplayProjectSource.cs`
    - Carry the role or an exact aligned role record; do not infer it again in providers.
- `Source/Cli/Commands/Screenplay/ScreenplayProjectSources.cs`
    - Continue to create the sole stable source identity/display context.
- `Source/Cli/Commands/Screenplay/ArcScreenplayGeneration.cs`
    - Use Arc's released project-aware facade and pass `DotNetProjectCompilation` values rather than bare compilations when source recovery is requested.
- `Source/Cli/Commands/Screenplay/CritterStackScreenplayGeneration.cs`
    - Set `Role`, `SourceContext`, `ProjectPath`, and authored trees on every project contract.
- `Source/Cli/Commands/Screenplay/ScreenplayGenerationOptions.cs`
- `Source/Cli/Commands/Screenplay/GenerateScreenplaySettings.cs`
    - Preserve existing option defaults and add only approved, provider-advertised source-structure choices.
- `Source/Cli/Commands/Screenplay/ScreenplaySourcePolicyProvenance.cs`
- `Source/Cli/Commands/Screenplay/ScreenplayGenerationProvenance.cs`
- `Source/Cli/Commands/Screenplay/ScreenplayDiagnosticsWriter.cs`
    - Report source-path policy and source-structure policy separately.
- `Source/Cli/Commands/Screenplay/ScreenplayFrameworkCapabilities.cs`
- `Source/Cli/Commands/Screenplay/ScreenplayCompatibility.cs`
    - Admit the exact provider/API capability before applying source-structure options.
- `Directory.Packages.props`
    - Pin the exact released adapter and Generation set.

### Provenance schema requirements

For each selected project, text and machine-readable provenance must distinguish:

- logical project path;
- stable project identity;
- selected target framework;
- package and assembly identities;
- adapter/provider identity and version;
- `DotNetProjectRole`;
- source-path policy version, display root, and case policy; and
- source-structure policy version, feature root, namespace segments skipped, and configured module.

Absent and empty values remain distinct where the Generation canonical request does. Physical workspace roots remain operational details and are never serialized as stable provenance.

### CLI gate

```bash
dotnet restore --property:Configuration=Release --source https://api.nuget.org/v3/index.json
dotnet build --no-restore --configuration Release
dotnet test --no-restore --configuration Release
```

Also require:

- provider selection and compatibility specs;
- source metadata alignment and relocation specs;
- Arc and all canonical Critter/Marten installed-tool generation flows;
- deterministic text and machine-readable diagnostics;
- package/native/GitHub/Homebrew distribution checks used by the release; and
- public release verification before the version set is used as evidence for E.

## E. Complete neutral specification recovery in Arc

This increment starts only after A through D are released and verified. It extends the exact rejection baseline; it does not rewrite the legacy Arc analyzer wholesale.

### Red vector 1: generated-style command success and event extraction

Use the exact shape emitted by Stage's:

- `Stage/Source/Rendering.Cratis/Semantics/SemanticCommandSpecificationRenderer.cs`.

The Arc focused vector must recover:

- `CommandScenario<TCommand>.Execute(new TCommand(...))` as the one `When/Command` step;
- `ShouldBeSuccessful()` as accepted outcome evidence without inventing a separate semantic step;
- each exact `ShouldHaveAppendedEvent<TCommand,TEvent>(eventSourceId, predicate)` as an ordered `Then/Event` step;
- the event contract as an exact `ArtifactKind.Event` subject;
- every event predicate equality as typed `SpecificationValueFact` values; and
- exact source ranges for scenario, command, event assertion, and each retained value.

### Red vector 2: event-predicate values

Replace the existing diagnostic-only predicate vector with positive cases for a conjunction of direct member equalities to exact literals/concepts. Keep negative cases for:

- method calls or arbitrary code in a predicate;
- inequality, range, disjunction, negation, or side effects not represented by the neutral contract;
- captured values whose exact literal construction cannot be proven;
- repeated or conditional assertion calls; and
- a predicate member that does not bind to the exact event type.

Any unsupported predicate blocks the whole scenario with `ARCSP0001` or an additive, documented stable Arc code. It must not emit an event step without all asserted values.

### Red vector 3: generated-style read-model assertion

Use the exact shape emitted by:

- `Stage/Source/Rendering.Cratis/Semantics/SemanticReadModelSpecificationRenderer.cs`.

Recover:

- `ReadModelScenario<TReadModel>` as an exact read-model target;
- `.Given.ForEventSource(...).Events(new TEvent(...))` as ordered `Given/Event` state;
- the projected instance assertions as one exact `Then/ReadModel` step with typed values; and
- `GenerationSliceKind.StateView` placement from the application read-model target, not from the spec namespace alone.

Block the scenario if the event source, event construction, instance member, comparison value, or target read model is ambiguous or computed.

### Red vector 4: generated-style query/read assertion

Use the exact shape emitted by:

- `Stage/Source/Rendering.Cratis/Semantics/SemanticQuerySpecificationRenderer.cs`.

Recover:

- the exact static query method as `ArtifactKind.Query`;
- query arguments as ordered values on a `When/Read` step;
- the expected result as typed result values on the same neutral read behavior required by the current contract/lowerer; and
- the equality assertion as exact query-result evidence.

Do not treat arbitrary calls returning a read model as queries. Require an exact Arc query artifact already admitted from application source.

### Red vector 5: adapter and project permutations

Each positive vector must run in all applicable arrangements:

1. application and specification source in one project;
2. a `Specifications` project referencing one application project;
3. a specification project referring to an artifact in a sibling application project;
4. reversed application/specification project order;
5. reversed syntax-tree order;
6. neutral Arc contribution before and after another independent adapter contribution; and
7. relocated physical checkout with unchanged logical project/document identities.

The resolved graph, diagnostics, lowered AST, canonical source, and print/compile/print result must be identical.

### Atomic fail-closed rules

For each scenario, Arc must stage scenario, step, and value facts locally and publish them only after all required evidence is exact.

- A blocker emits no `SpecificationScenarioFact`, `SpecificationStepFact`, or `SpecificationValueFact` for that scenario.
- Independently valid artifact facts may remain, but no scenario-dependent placement or relationship may imply that the blocked behavior was recovered.
- One blocked scenario does not erase another exact scenario.
- A scenario with one unsupported step or one unsupported value is omitted as a unit.
- A missing target placement prevents admission and lowering; it never falls back to display-name or namespace-only placement.
- Duplicate identical evidence collapses deterministically. Incompatible evidence remains a conflict and admits no scenario.
- The resolver may retain independently valid raw facts for provenance, but `SpecificationAdmission` must never admit an incomplete scenario and `SpecificationSyntaxLowerer` must never lower one.
- Every diagnostic preserves stable code, outcome, severity, subject, and best exact source range.

### Owning Arc files

Analysis/evidence:

- `Source/DotNET/Screenplay/Analysis/Specifications/SpecificationCalls.cs`
- `Source/DotNET/Screenplay/Analysis/Specifications/SpecificationMembers.cs`
- `Source/DotNET/Screenplay/Analysis/Specifications/SpecificationReader.cs`
- `Source/DotNET/Screenplay/Analysis/Specifications/SpecificationStepReader.cs`
- `Source/DotNET/Screenplay/Analysis/Specifications/SpecificationOutcomeReader.cs`
- `Source/DotNET/Screenplay/Analysis/Specifications/SpecificationValues.cs`
- `Source/DotNET/Screenplay/Analysis/Specifications/SpecificationScenarioEvidence.cs`
- `Source/DotNET/Screenplay/Analysis/Specifications/SpecificationStateEvidence.cs`

Neutral contribution:

- `Source/DotNET/Screenplay/Generation/ArcSpecificationFactAdapter.cs`
- `Source/DotNET/Screenplay/Generation/ArcSpecificationFactBuilder.cs`
- `Source/DotNET/Screenplay/Generation/ArcSpecificationArtifactFacts.cs`
- `Source/DotNET/Screenplay/Generation/ArcSpecificationValueFacts.cs`
- `Source/DotNET/Screenplay/Generation/ArcSpecificationEvidence.cs`
- `Source/DotNET/Screenplay/Generation/ArcSpecificationFacts.cs`

Focused specifications:

- keep the two existing `for_ArcSpecificationFactAdapter` vectors;
- add one positive generated-style file per success, read-model, and query shape;
- add one atomic-negative matrix file per unsupported family; and
- add one project/adapter permutation file that snapshots graph, diagnostics, lowered source, and round-trip output.

### Conditional Generation owners

The current neutral contracts already expose `Event`, `ReadModel`, `Command`, `Read`, and `Error` step kinds and scalar/composite/collection values. Change Generation only if a red Arc vector proves a contract or lowerer gap. Conditional owners are:

- `Source/DotNET/Generation.Contracts/Specifications.cs`
- `Source/DotNET/Generation.Contracts/SpecificationValues.cs`
- `Source/DotNET/Generation/SpecificationFactResolver.cs`
- `Source/DotNET/Generation/SpecificationAdmission.cs`
- `Source/DotNET/Generation/SpecificationValueAdmission.cs`
- `Source/DotNET/Generation/SpecificationSyntaxLowerer.cs`
- `Source/DotNET/Generation/SpecificationValueSyntaxLowerer.cs`
- `Source/DotNET/Generation.Specs/for_GenerationResolver/**`

Any change there is additive, red-first, package-validated, released, and adopted before Arc continues.

## F. Final cross-repository gate

Do not close Generation #25 or #26 from unit tests alone. The final evidence bundle must record exact public versions, commits, repository signatures, and package hashes and must prove:

1. Generation package consumers compile using only the public lockstep package set.
2. Arc's legacy compatibility facade remains byte-compatible for existing vectors.
3. Critter Stack's canonical expected outputs remain byte-identical.
4. CLI option handling is explicit and provider-capability gated.
5. CLI provenance records project role and source-structure policy without physical paths.
6. Stage's generated rejection, success/event, projection/read-model, and query specification shapes recover through Arc into the expected neutral scenarios.
7. The recovered document compiles with the Screenplay compiler and survives print/compile/print unchanged.
8. Reversing projects, adapters, facts, and syntax trees changes no canonical output.
9. Unsupported or ambiguous scenarios produce typed diagnostics and no partial lowered scenario.
10. All repository CI checks and release workflows are green.

Issue closure is narrow:

- Close Generation #26 only when Arc, Critter Stack, and CLI consume the released shared placement contract and parity evidence is recorded.
- Close Generation #25 only when all four generated-style specification families and the atomic/permutation matrices pass through public packages.
- Do not claim completion of Generation #24, Screenplay #148, or the full Screenplay program.

## Stop conditions

Stop immediately and keep the work release-blocked if any of these occurs:

- the shared placement API is not on nuget.org in all four lockstep packages;
- Arc PR #2602 is not released and publicly verifiable;
- an adoption branch needs a local package feed for its acceptance evidence;
- existing Arc or Critter Stack output changes without an approved compatibility vector;
- the CLI cannot classify a specification project or target application unambiguously;
- a provider option is silently ignored;
- a physical path enters stable provenance;
- a scenario can lower after any required step/value/placement was rejected;
- package validation or a legacy binary smoke fails; or
- any required CI/release check is queued, canceled, or red.

At a stop condition, report the exact blocker and retain the last green release boundary. Do not weaken admission, copy unreleased APIs, suppress diagnostics, or merge around unavailable release infrastructure.
