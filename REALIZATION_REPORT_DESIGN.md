<!--
Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information.
-->

# Generation #24 realization report design

> **Status: DESIGN COMPLETE FOR GENERATION #24, BUT UNIMPLEMENTED.** No report contract,
> serializer, fingerprint, `GeneratedScreenplayDefinition` property, or CLI integration described
> here exists yet. Implementation and release are **blocked**. The core projection design is
> settled, while generated-artifact and implementation-attachment links remain **design-blocked**
> on their owning Screenplay contracts. This document is a plan, not release evidence.

## Decision summary

Generation will project its existing frozen contribution, resolution, admission, and lowering
pipeline into a versioned realization-report fragment outside the generated `.play` document.
The fragment is Generation-owned. It does not replace `AdapterContribution`,
`ResolvedApplicationGraph`, or the CLI's provenance envelope.

The design has these invariants:

1. The report has independent `purpose`, `disposition`, and `category` dimensions.
2. Stable source, evidence, fact, derivation, admission, and artifact-link identities are never
   conflated.
3. Canonical JSON is the only fingerprint input for semantic and realization projections.
4. The final-byte fingerprint is over the exact canonical `.play` bytes, not report bytes.
5. Technical realization facts cannot change semantic equality.
6. Only a confirmed `eventModelGap` admission can feed Screenplay lowering.
7. Absolute physical paths, source excerpts, and implementation bodies never enter the fragment.
8. `GeneratedScreenplayDefinition` gains only an additive, init-only, nullable report property.
9. The CLI owns its envelope, provider/package/source-policy provenance, output destinations, and
   preservation of unknown fragments.
10. `.play` standard output remains byte-pure.

## Current contract baseline

This design builds on, but does not alter the meaning of, the current contracts:

- `Source/DotNET/Generation.Contracts/Identity.cs` defines `AdapterIdentity`, `FactId`, and
  `SubjectId`.
- `Source/DotNET/Generation.Contracts/Evidence.cs` defines project-qualified
  `SourceFileIdentity`, display-oriented `SourceRange`, `EvidenceStrength`, and `Evidence`.
- `Source/DotNET/Generation.Contracts/GenerationFact.cs` requires every fact to carry a fact ID,
  subject ID, and evidence.
- `Source/DotNET/Generation.Contracts/AdapterContribution.cs` retains producer identity, raw
  facts, and adapter diagnostics.
- `Source/DotNET/Generation.Contracts/Diagnostics.cs` retains stable code, severity, optional
  typed outcome, source range, and subject.
- `Source/DotNET/Generation/ResolvedApplicationGraph.cs` retains canonical resolved variants,
  their evidence, conflicts, atomically admitted specifications, and diagnostics.
- `Source/DotNET/Generation/ScreenplayDefinitionGenerator.cs` currently returns source, syntax,
  resolved graph, diagnostics, and `IsSuccess`. It has no report or independent fingerprints.
- `Source/DotNET/Generation/Canonical.cs` provides internal structural ordering keys. It is not a
  public JSON or report compatibility contract.
- `COMPATIBILITY.md` makes source identity distinct from display path, forbids physical checkout
  paths in stable outputs, and keeps protocol, package, Screenplay language, Generation, and CLI
  versions independent.

The resolved graph remains the semantic-resolution result consumed by lowering. It must not become
an interchange envelope. A report projection needs the frozen contributions as well as the graph,
because graph-level diagnostics no longer retain every contribution producer association.

The CLI already owns `ScreenplayGenerationProvenance` and `ScreenplayDiagnosticsWriter`. Its current
contract keeps generated `.play` on standard output and diagnostics/provenance on standard error.
Generation must not absorb provider selection, package/assembly provenance, source-policy
provenance, or CLI formatting.

## Report dimensions

Every reportable item has three independent dimensions. No value in one dimension implies a value
in another.

### Purpose

| Wire value | Meaning |
| --- | --- |
| `businessMeaning` | Portable domain or behavioral meaning eligible for semantic comparison |
| `realization` | A target/source implementation choice that realizes portable meaning |
| `operations` | Hosting, deployment, transport, observability, or other operational detail |
| `uncertain` | A proposal or observation whose semantic role is not confirmed |

### Disposition

| Wire value | Meaning |
| --- | --- |
| `represented` | Represented in the generated Screenplay or an explicitly linked artifact |
| `reportOnly` | Preserved as provenance but deliberately excluded from Screenplay |
| `notRepresented` | Recognized, but no safe representation exists |
| `unresolved` | Available evidence cannot establish one result |
| `rejected` | Admission rejected the item under a named, versioned rule |

### Category

| Wire value | Meaning |
| --- | --- |
| `eventModelGap` | A confirmed omission from portable event-model meaning |
| `realization` | A source or target realization detail |
| `operations` | An operational detail outside portable event-model meaning |
| `uncertainty` | Ambiguity, unsupported interpretation, or unresolved evidence |

`eventModelGap` is not a synonym for a warning or an unsupported behavior. It is a positive,
reviewable classification produced by admission.

## Generation-owned fragment schema

The first schema is identified as
`https://schemas.cratis.io/screenplay/generation/realization-report/v1` with integer
`schemaVersion: 1`. The URI and integer are both emitted so a consumer can route without inferring
schema from a package version.

The canonical top-level shape is:

```json
{
  "schema": "https://schemas.cratis.io/screenplay/generation/realization-report/v1",
  "schemaVersion": 1,
  "fingerprints": {
    "algorithm": "sha256",
    "semantic": "sha256:<lowercase-hex>",
    "realization": "sha256:<lowercase-hex>",
    "byte": "sha256:<lowercase-hex>"
  },
  "sources": [],
  "evidence": [],
  "facts": [],
  "derivations": [],
  "admissions": [],
  "artifactLinks": [],
  "diagnostics": []
}
```

The schema has no timestamp, machine name, current directory, process ID, random report ID, or
implicit package-version coupling. Empty collections are emitted as empty arrays, not omitted or
written as `null`.

### Source records

A source record contains:

- a report-local stable `sourceId` derived from `SourceFileIdentity`;
- the stable project identity and normalized project-relative path;
- the host-declared display path only as non-identity presentation data;
- no physical root or absolute path.

Ranges are references to `sourceId` plus 1-based start/end line and column. A display path is never
used to join records or calculate semantic or realization equality. When stable
`SourceFileIdentity` is unavailable through a legacy host, the record is explicitly marked
`legacyUnstable`; it can be reported but cannot participate in a cross-host realization claim.

### Evidence records

An evidence record contains:

- `evidenceId`;
- producer adapter ID and producer version as separate fields;
- `EvidenceStrength` as its stable lower-camel wire value;
- an optional source/range reference;
- an optional privacy-safe explanation;
- the fact, diagnostic, derivation, or admission records that reference it.

Producer version is provenance, not adapter identity. Changing explanation prose does not silently
change source or fact identity.

### Fact records

A fact record contains:

- a producer-scoped `factId` consisting of stable adapter ID plus the adapter's existing `FactId`;
- the exact `SubjectId`;
- a stable fact-contract name and contract version;
- canonical neutral payload;
- evidence references;
- purpose, disposition, and category.

Adapter version is retained on the producer/evidence record but is not folded into the stable fact
identity. Existing fact discriminators retain `Unknown = -1`; an unknown or undefined value is
reported and fails closed rather than being mapped to a nearby known role.

### Derivation records

A derivation record contains:

- `derivationId`;
- stable derivation-rule ID and rule version;
- canonically ordered input fact references;
- output fact references;
- evidence references and producer lineage;
- purpose, disposition, category, and diagnostics.

A derivation rule consumes one frozen base snapshot. It cannot inspect adapter registration order,
another adapter instance, Roslyn state, or a previously derived mutable graph. Rule ID and version
remain distinct from the derivation occurrence ID.

### Admission records

An admission record contains:

- `admissionId`;
- stable admission-policy ID and policy version;
- the candidate fact, scenario, or derivation identity;
- all canonical input references;
- decision and reason code;
- purpose, disposition, and category;
- admitted semantic identity or artifact link, when one exists;
- evidence, source ranges, subjects, producers, outcomes, and diagnostics.

Evidence strength alone does not confirm admission. An item may feed Screenplay lowering only when a
named admission decision explicitly confirms all of the following:

- category is `eventModelGap`;
- purpose is `businessMeaning`;
- disposition is `represented`;
- the decision is accepted rather than unresolved, rejected, conflicted, or unsupported;
- every required input is from the frozen, structurally verified contribution snapshot.

Realization, operations, and uncertainty records are never lowered merely because they are exact or
configured.

### Artifact-link records

An artifact link connects identities; it does not create or reinterpret them. It contains:

- `artifactLinkId`;
- the owning Screenplay semantic identity;
- the generated-artifact identity and role;
- the admission or derivation identity that established the link;
- optional implementation-requirement and accepted-attachment identities;
- optional exact generated source map;
- disposition and loss/ambiguity diagnostics.

Source, semantic, generated-artifact, implementation-requirement, attachment, candidate, and
runtime identities are separate fields and separate identity domains. A path is placement or
provenance, never artifact or attachment identity.

The initial implementation must not invent semantic, implementation-requirement, attachment, or
generated-artifact identity contracts in Generation. Those identities are admitted only after their
owning Screenplay contracts from Screenplay issues #139 and #148 are public and versioned. Until
then, artifact links are absent rather than approximated from names or paths. This is the remaining
**design block**.

### Diagnostic records

Diagnostics retain stable code, severity, optional typed outcome, subject, source/range, producer,
and related fact/derivation/admission identities. Human-readable messages may be emitted only after
the privacy gate; stable codes and typed outcomes, not prose, are the compatibility contract.

## Identity construction

Structured identity seeds are canonical JSON objects, not delimiter-concatenated strings. Each
report-local identity uses `sha256:<lowercase-hex>` over the canonical UTF-8 seed. The complete seed
fields remain in the report so hashes are verifiable and are not opaque semantic authority.

| Identity | Canonical seed |
| --- | --- |
| Source | stable project identity + normalized project-relative path |
| Evidence | producer ID + source/range + strength + owning fact/decision reference |
| Fact | stable producer ID + existing producer-scoped `FactId` |
| Derivation | rule ID/version + sorted input fact IDs + sorted output fact IDs |
| Admission | policy ID/version + candidate ID + sorted input IDs |
| Artifact link | semantic ID + generated-artifact ID + link role |

Display paths, explanations, diagnostic messages, adapter registration position, input enumeration
position, timestamps, and physical locations never enter identity seeds. Ordered domain data uses
an explicit index in the seed; it is never made ordered by arrival sequence.

## Canonical JSON contract

Generation owns one public canonical serializer for its fragment and fingerprint projections. It
uses a constrained RFC 8785-compatible profile:

- UTF-8 without BOM;
- one JSON value and no trailing newline;
- no insignificant whitespace;
- object member names ordered by ordinal UTF-16 code-unit order;
- strings escaped using the RFC 8785 rules;
- integers in invariant minimal decimal form;
- no floating-point values in the schema;
- booleans and `null` in JSON lowercase;
- enum wire values fixed in lower camel case and never renamed or reused;
- arrays sorted by their stable identity unless the schema declares semantic order;
- semantically ordered arrays carry an explicit index and validate uniqueness/contiguity;
- source identity strings use the normalization and case policy already established by their owning
  source contract; arbitrary user text is not silently normalized.

Canonicalization occurs after contributions are materialized and structurally verified. The
serializer rejects duplicate object names, duplicate identities, noncanonical source identities,
invalid ranges, forbidden privacy data, unknown required discriminators, and references to missing
records. It never repairs or guesses.

### Compatibility policy

- The report schema version is independent of package, adapter, Screenplay language, and CLI
  versions.
- Additive optional fields and new opaque fragment types do not change an existing field's meaning.
- Required-field changes, identity-seed changes, canonicalization changes, fingerprint-projection
  changes, or enum reinterpretation require a new schema version.
- Existing enum wire values and identity namespaces are never reused.
- A Generation reader either understands the declared schema version or treats the complete
  fragment as opaque. It never partially interprets a newer schema as the current one.
- Generation writes only versions it fully understands.
- CLI envelopes preserve unknown fragments; they do not ask Generation to deserialize them.

## Fingerprints

All three fingerprints use SHA-256 and lowercase hexadecimal with the `sha256:` prefix. They answer
different questions and are never substituted for one another.

### Semantic fingerprint

The semantic projection includes only accepted, represented `businessMeaning` records and their
stable Screenplay semantic identities, portable definitions, event contracts, and normalized
specification outcomes. It excludes:

- sources and ranges;
- adapter identity/version and evidence strength;
- derivations and admission mechanics except their accepted semantic result;
- realization, operations, and uncertainty records;
- diagnostics and human prose;
- generated paths, implementation bodies, and attachment/candidate/runtime identities.

Therefore a technical fact, source relocation, adapter version, evidence explanation, or output
path cannot alter semantic equality.

### Realization fingerprint

The realization projection includes the semantic fingerprint plus stable realization facts,
producer/rule/policy revisions, stable source identities and ranges, derivation lineage, admission
decisions, artifact links, and typed diagnostic codes/outcomes. It excludes display-only paths,
human prose, CLI package/source-policy envelope data, timestamps, and every privacy-forbidden field.

A realization implementation or evidence change can alter this fingerprint without altering the
semantic fingerprint.

### Byte fingerprint

The byte fingerprint is SHA-256 over the exact final canonical `.play` bytes encoded as UTF-8
without BOM. It is not a self-hash of the report. If output is not trustworthy enough to emit, the
byte fingerprint is absent and the blocking diagnostic is retained. Any byte change changes this
fingerprint even when semantic and realization equality remain unchanged.

The canonical report bytes can be hashed by an enclosing CLI fragment descriptor, but that transport
hash is not one of Generation's three equality fingerprints.

## Privacy and data-minimization boundary

The fragment must preserve stable identities and full logical ranges while excluding:

- absolute checkout, repository, user-profile, temporary, cache, package-cache, and tool paths;
- source excerpts, syntax text, generated code bodies, and implementation attachment bodies;
- environment variables, command lines, machine/user names, credentials, tokens, and secrets;
- unbounded exception text or stack traces;
- timestamps and process-local identifiers.

Logical project identity, normalized project-relative source identity, host-declared display path,
line/column range, stable diagnostic code, typed outcome, subject, and producer are allowed.
Explanations and diagnostic messages are allowed only when they satisfy the same producer contract
and privacy validation. The serializer fails closed on forbidden data; it does not redact and then
pretend the redacted report has the same fingerprint.

## Additive `GeneratedScreenplayDefinition` surface

The planned public addition is an init-only nullable property on the existing record:

```csharp
public GenerationRealizationReportFragment? RealizationReport { get; init; }
```

The final type name may change only before implementation review; its semantics may not. The
existing constructor shape, `Generate(...)` overload, `Source`, `Application`, `Graph`,
`Diagnostics`, and `IsSuccess` remain source- and binary-compatible. The existing overload delegates
to the frozen-snapshot pipeline and sets the property when report projection succeeds.

Report projection failure is a typed generation error. It must not silently return a successful
`.play` without the requested report. Older consumers that do not read the nullable property remain
compatible.

The public Generation API must also expose canonical serialization without exposing mutable
serializer options. Callers cannot change naming, ordering, escaping, omission, or enum behavior.

## CLI envelope and output separation

The CLI assembles a versioned CLI-owned envelope containing its existing provider, package,
assembly, capability, compatibility, and source-policy provenance beside one or more report
fragments. Generation contributes only its fragment and canonical fragment bytes.

The CLI contract is:

- generated `.play` goes to standard output only when no `.play` file is selected;
- diagnostics and human-readable report text go to standard error;
- JSON report output goes to standard error when `.play` occupies standard output;
- `--report-file` writes one complete envelope to the selected file;
- when `.play` is written to a file, ordinary CLI result formatting may use standard output, but it
  still emits only one selected result shape;
- no stream contains `.play` mixed with text, JSON, diagnostics, or a second JSON value;
- failure before trustworthy `.play` output keeps standard output empty;
- existing behavior remains unchanged when no report option is requested.

The exact option spelling is CLI-owned and must be characterized in the CLI change before public
commitment. Generation does not reference CLI types.

### Unknown-fragment preservation

A CLI envelope fragment carries schema identity, schema version, media type, canonical-byte hash,
and its canonical JSON payload. The CLI stores an unrecognized payload as opaque raw UTF-8 JSON and
re-emits it without interpreting, dropping, renaming, or rebuilding its members. Canonical envelope
ordering uses fragment identity, not discovery order. Known text renderers may skip an unknown
fragment with a typed notice, but envelope read/write must preserve it and verify its transport
hash.

A newer Generation fragment must therefore survive an older CLI envelope round trip unchanged even
when that CLI cannot render it. Unknown-fragment preservation is a CLI gate, not a reason to weaken
Generation's typed reader.

## First red vectors

Implementation starts by adding failing characterization/specification vectors in this order:

1. **Canonical golden vector** — one minimal fragment has an exact checked-in canonical UTF-8 byte
   sequence and exact SHA-256 projections.
2. **Permutation vector** — reversing adapters, projects, facts, evidence, diagnostics, derivation
   inputs, and admissible roster order produces identical fragment bytes and all fingerprints.
3. **Technical-only change vector** — changing only realization evidence changes the realization
   fingerprint, leaves the semantic fingerprint unchanged, and leaves `.play` bytes unchanged.
4. **Final-byte vector** — changing only canonical `.play` bytes changes the byte fingerprint; it
   does not manufacture semantic drift.
5. **Source-policy vector** — relocated checkouts and equivalent stable source identities preserve
   semantic and realization fingerprints; a different valid display policy is visible only in the
   CLI envelope/presentation.
6. **Identity separation vector** — same display names and same relative paths in different projects
   do not alias source, subject, fact, semantic, or artifact-link identities.
7. **Admission firewall vector** — only a confirmed, accepted `businessMeaning` + `eventModelGap` +
   `represented` record reaches lowering. Realization, operations, uncertainty, unresolved,
   rejected, and report-only records never do.
8. **Loss vector** — authored, generated, recovered, unsupported, ambiguous, and lossy observations
   remain distinguishable with source, producer, disposition, and typed outcome.
9. **Conflict vector** — incompatible variants preserve every fact/evidence identity and produce no
   order-selected winner.
10. **Privacy vector** — an absolute path, source excerpt, implementation body, environment value,
    or stack trace causes report serialization to fail before bytes are emitted.
11. **Unknown-schema vector** — Generation refuses partial interpretation of a newer fragment.
12. **Unknown-fragment CLI vector** — an older CLI reads and writes an envelope containing a future
    fragment with identical opaque canonical payload bytes and transport hash.
13. **Output-isolation CLI vector** — redirected standard output is exactly `.play`; text/JSON report
    and diagnostics are isolated to standard error or the report file.
14. **Artifact-link vector** — accepted implementation requirement/attachment identity survives
    render/recover while source, semantic, attachment, candidate, and runtime identities remain
    distinct. This vector stays red and blocks implementation until the Screenplay contracts exist.
15. **Compatibility vector** — legacy binaries run unchanged and a current-source package consumer
    can read and canonically serialize the new nullable report surface.

No production type is added before vectors 1, 2, 6, 7, 10, 11, and 15 fail for the expected reason.
CLI implementation does not begin before vectors 12 and 13 are committed red in the CLI repository.

## Repository ownership

| Repository/package | Owns | Must not own |
| --- | --- | --- |
| `Cratis/Screenplay` | Semantic IDs, implementation requirements, accepted attachment identities, generated-artifact identity/source-map contracts | Source-adapter facts or CLI envelopes |
| `Cratis/Screenplay.Generation.Contracts` | Framework-neutral source/evidence/fact identity contracts and additive report data contracts that require no compiler | Roslyn, CLI, or Screenplay printer behavior |
| `Cratis/Screenplay.Generation` | Report projection, admission firewall, canonical JSON, three fingerprint projections, additive generated-definition surface | Provider/package discovery or output destinations |
| `Cratis/Screenplay.Generation.DotNet` | Stable .NET source identity and fixed-snapshot derivation inputs | Workspace loading, physical roots, or report envelopes |
| Ecosystem adapters | Framework semantics, safe evidence, stable producer-scoped fact IDs, adapter diagnostics | Other adapters, Screenplay AST/text, or report assembly |
| Composition hosts | Authoritative source set, stable source policy, frozen inputs, orchestration | Reinterpreting report equality |
| `Cratis/cli` | Provider/package/source-policy provenance, envelope, unknown-fragment preservation, text/JSON/report-file UX, stream isolation | Generation fragment semantics or fingerprint rules |

## Dependency order

Implementation follows this order and stops at a blocked boundary:

1. Reconcile and release the source identity/path contract from Generation #21.
2. Deliver frozen contribution snapshots, descriptors, structural verification, and admission
   dispositions from Generation #23.
3. Deliver fixed-snapshot derivation identities, rule versions, input lineage, and output
   dispositions from Generation #19.
4. Reconcile the already-present executable-specification and source-placement capabilities with
   Generation #25 and #26; open issue state is not release evidence.
5. Publish the owning Screenplay semantic, implementation-requirement, attachment, and
   generated-artifact identity contracts required by Screenplay #139/#148.
6. Add Generation red vectors and then the versioned fragment projection, canonical serializer,
   fingerprints, privacy gate, and nullable `GeneratedScreenplayDefinition` property.
7. Pass all Generation package/API/consumer gates and publish the four Generation packages in
   lockstep.
8. Upgrade the CLI to that published Generation version; add the CLI envelope, unknown-fragment
   preservation, report rendering/file output, and stream-isolation vectors.
9. Upgrade composition facades and ecosystem adapters only after the CLI and package contracts are
   available; do not use project references or unreleased source roots as release evidence.
10. Add shared render/recover conformance vectors per admitted capability.

Generation #24 is a child of Generation #17. It does not reorder #17's atomic-adapter delivery plan
or make profiles semantic authorities.

## Blocking gates

### Design gates

Implementation is **design-blocked** until all are true:

- Screenplay owns public, versioned semantic, implementation-requirement, attachment, and
  generated-artifact identities needed by artifact links.
- Generation #23 defines the frozen snapshot and producer/disposition contract used as report input.
- Generation #19 defines derivation rule identity/version and canonical input lineage.
- Generation #21 source identity and display-policy behavior is reconciled with released contracts.
- The CLI envelope has an agreed opaque-fragment preservation contract without moving CLI
  provenance into Generation.

### Generation implementation gates

- Canonical JSON golden and permutation vectors pass byte-for-byte.
- Semantic, realization, and byte fingerprint vectors pass independently.
- Technical-only facts cannot change semantic equality.
- Only confirmed event-model gaps can feed lowering.
- Full stable source ranges, outcomes, subjects, producers, conflicts, and lineage survive.
- Privacy vectors prove no absolute path or source/body text can be serialized.
- Unknown required discriminators and unsupported schema versions fail closed.
- Debug specs pass with zero warnings/errors.
- Release builds for every target framework pass with zero warnings/errors.
- API/package validation passes against the latest released baseline.
- Legacy binary smoke and clean current-source consumer pass.
- All four packages pack and release in lockstep with the same sentinel/release version.

### CLI release gates

- The CLI consumes a published Generation package version, not a project reference.
- Existing no-report output remains unchanged.
- `.play` standard output isolation passes for success and failure.
- Text, JSON, compact JSON, and report-file modes each emit one valid selected shape.
- Unknown fragment payload and transport hash survive envelope round trip.
- Provider/package/assembly/capability/source-policy provenance remains CLI-owned.
- Project-target and solution/repository-target clean consumers pass.

### Cross-repository fidelity gates

- One accepted generated artifact can be recovered to the same semantic and attachment identities.
- Realization-only changes do not change semantic revision.
- Loss, ambiguity, unsupported recovery, and stale attachments block publication and remain visible.
- Source-adapter and renderer-target rosters remain separate.

## Explicit non-goals

Generation #24 does not:

- put provenance or realization data into `.play`;
- make the resolved graph a wire envelope;
- define a universal code intermediate representation;
- serialize source excerpts or implementation bodies;
- infer semantic or attachment identity from names, paths, hashes, or runtime IDs;
- let technical facts, profiles, adapter order, or package versions determine business meaning;
- add runtime plugin loading, workspace discovery, or CLI dependencies to Generation;
- close, release, or claim implementation of Generation #24.

## Completion criterion

This design document is complete when it records the decisions above. Generation #24 itself is not
complete until every blocking gate passes in its owning repository with fresh evidence. Until then,
status reporting must say **unimplemented, design-blocked, and release-blocked**.
