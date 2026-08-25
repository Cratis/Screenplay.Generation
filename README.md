# Screenplay.Generation

Framework-neutral source adapter SDK for generating verified [Cratis Screenplay](https://github.com/Cratis/Screenplay) definitions.

**Public compatibility floor and lifecycle:** [`COMPATIBILITY.md`](https://github.com/Cratis/Screenplay.Generation/blob/main/COMPATIBILITY.md)

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

## Architecture

```text
source adapter
  -> typed facts and evidence
  -> resolved application graph
  -> lowerable Screenplay model
  -> Screenplay AST
  -> canonical printer
  -> Screenplay compiler verification
  -> .play
```

Adapters contribute semantic facts; they do not construct syntax nodes or concatenate `.play` fragments. This allows facts from related frameworks—such as Marten and Wolverine—to be resolved together before one document is emitted.

`Cratis.Screenplay.Generation.DotNet` deliberately does not own `MSBuildWorkspace`. Hosts such as Cratis CLI load a project once and pass Roslyn compilations to official adapters.

### Adapter syntax robustness

`DotNetSymbols` and `DotNetInvocations` provide shared metadata-name, companion-method, named-argument, extension-method, formal-argument, and bounded receiver-root mechanics. Adapters should use these helpers instead of duplicating syntax-shape assumptions.

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

`DotNetSourcePlacementDerivation.Derive(...)` then combines exact artifact roles and independently established slice kinds with that fixed snapshot. Identical requests execute once, request order cannot change output, and mismatched subjects, unknown artifact kinds, or multiple distinct requests for one artifact role block that artifact rather than selecting a provider's answer.

Folder and namespace evidence must agree when both can establish placement. Rooted, traversing, malformed, missing-root, unknown-role, unknown-slice-kind, insufficient, and conflicting inputs return stable `DOTNETSP####` error diagnostics with the affected source subject; the resolver never chooses one structure heuristically.

A composition host references `Cratis.Screenplay.Generation` and `Cratis.Screenplay.Generation.DotNet.Vogen` directly, plus its external ecosystem adapter package. The Vogen adapter package brings `Cratis.Screenplay.Generation.DotNet` and `Cratis.Screenplay.Generation.Contracts` transitively; the analyzed application references Vogen itself.

A clean consumer composes Vogen with any external ecosystem adapter by keeping contributions separate until neutral resolution:

```csharp
var adapters = new IDotNetScreenplayAdapter[]
{
    new VogenConceptScreenplayAdapter(),
    externalAdapter
};

var adapterOptions = new DotNetAdapterOptions();
var contributions = adapters
    .Where(adapter => adapter.CanAnalyze(context))
    .Select(adapter => adapter.Analyze(context, adapterOptions));

var definition = new ScreenplayDefinitionGenerator().Generate(
    contributions,
    new ScreenplayGenerationOptions { Domain = "Ordering" });
```

The Vogen contribution establishes authored concepts, supported primitive representations, and one named validation rule only when the attribute-bearing declaration contains an authored static `Validate(TBacking)` method returning the exact `Vogen.Validation` type. The rule keeps the authored predicate and implementation file; a single semantically constant `Validation.Invalid("message")` return can also preserve its message. Arbitrary validation bodies are never translated into built-in rules.

Generated members never provide primary evidence. The adapter never infers identity from `Guid` or `Id`, never treats normalization as validation, and never treats named instances as optional values or defaults. It reports stable loss diagnostics instead: `VOG0001` for unsupported backing representations, `VOG0002` for authored `NormalizeInput(TBacking)` behavior, and `VOG0003` for authored `Vogen.InstanceAttribute` declarations.

## Concepts

.NET adapters can nominate a source type as a concept through `DotNetConceptFacts.Emit(...)`. Nomination requires an authored declaration surface—such as an attribute or an explicit framework registration API—with the adapter's own evidence. Structural guessing from wrapper shape or naming is not sufficient. The helper emits the neutral concept artifact and a representation only for a supported explicitly declared primitive backing; unsupported backings retain the concept nomination without inventing a representation.

Adapters can contribute `ArtifactKind.Concept` together with independently proven `ConceptRepresentationFact`, `ConceptAttributeFact`, and `ConceptValidationRuleFact` assertions. Primitive/enumeration representations, named attributes, and named external predicate rules resolve deterministically and lower to top-level Screenplay concepts without module placement.

`TypeReferenceDefinition.Subject` binds an artifact property to the exact concept subject rather than a simple display name. Missing, conflicting, unsupported, or same-named concept definitions produce stable diagnostics; generation never falls back to `String`.

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

See [`IMPLEMENTATION_STATUS.md`](IMPLEMENTATION_STATUS.md) for the current implementation checkpoint and pre-release decisions.

## Build and test

```shell
dotnet test Screenplay.Generation.slnx --configuration Debug
dotnet build Screenplay.Generation.slnx --configuration Release -p:Version=9999.0.0
dotnet pack Screenplay.Generation.slnx --no-build --configuration Release -o Artifacts/NuGet -p:Version=9999.0.0
./scripts/verify-package-consumers.sh 9999.0.0 Artifacts/NuGet
```

Package validation runs during pack against the latest released API baseline, `0.10.1`, for all four packages. Baseline strict mode remains disabled so intentional compatible additions are accepted while removals and signature changes still fail; no compatibility diagnostics are suppressed. The sentinel version must be applied to both the Release build and the no-build pack so package and assembly versions agree. The consumer smoke keeps clean legacy binaries compiled against the `0.1.0` core and `0.5.0` Vogen ancestry and runs them unchanged with current packages. A separate clean current-source consumer compiles only against the candidate packages and verifies the current authored-source, shared symbol helpers, declared concept nomination, neutral-fact, resolver, Vogen, adapter-composition, and deterministic compiler-verified generation APIs.

All builds require zero errors and zero warnings. Generated Screenplay output must compile and remain stable through print/compile/print.

## License

Screenplay.Generation is licensed under the [MIT license](LICENSE).
