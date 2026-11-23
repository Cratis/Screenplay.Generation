---
title: Build a .NET source adapter
description: Recover a framework's authored .NET semantics as neutral facts and generate deterministic, compiler-verified Screenplay.
---

Build a source adapter when you need to recover a framework's authored .NET semantics into a verified Screenplay model. Establish the host's source authority first, then implement the adapter and compose its contribution into verified output.

A source adapter owns **framework interpretation**. It does not load an MSBuild workspace, start the analyzed application, inspect live infrastructure, construct Screenplay syntax, or print `.play` text.

```mermaid
flowchart LR
    Host[Workspace host] --> Context[Roslyn compilations and authoritative authored trees]
    Context --> Adapters[One or more source adapters]
    Adapters --> Facts[Neutral facts, evidence, and diagnostics]
    Facts --> Generate[Resolve, lower, print, and compile]
    Generate --> Play[Canonical Screenplay]
```

## Choose the packages

| Package | Use it for |
| --- | --- |
| `Cratis.Screenplay.Generation.Contracts` | Framework-neutral facts, identities, evidence, and diagnostics |
| `Cratis.Screenplay.Generation` | Resolution, lowering, canonical printing, and compiler verification |
| `Cratis.Screenplay.Generation.DotNet` | Roslyn analysis, source evidence, type shapes, concepts, and source placement |
| `Cratis.Screenplay.Generation.DotNet.Vogen` | Optional Vogen source adapter composed by a host; ecosystem adapters do not need it unless they own that composition |

Only these four Generation packages release in lockstep. Ecosystem adapters and hosts have independent versions.

| Project role | Typical references |
| --- | --- |
| Adapter library | Contracts + DotNet |
| Composition host | Generation + every explicitly selected adapter |
| Adapter specs | Adapter library + Roslyn CSharp + specification packages |
| Analyzed application | Its own framework packages only; it does not reference Generation |

Keep runtime packages for the framework you analyze out of the adapter whenever Roslyn metadata names can express the contract. This avoids loading one framework version into a host that analyzes another version.

Reference one version across all directly referenced Screenplay Generation packages. Choose a package baseline using the support table below; current-main additions are not all part of the public release.

:::note
`v0.16.0` is the current public release and package-validation baseline. Described adapters, atomic admission, deterministic runner snapshots, and final fact dispositions are included in that lockstep package set. Granular type-use derivation is additive on `main`.
:::

| Capability | Released `0.16.0` | Current `main` |
| --- | ---: | ---: |
| Adapter, context, neutral fact, evidence, and diagnostic contracts | Yes | Yes |
| Stable source identity, fixed source snapshots, and strict placement | Yes | Yes |
| Executable specification facts and `DotNetConceptFacts` | Yes | Yes |
| Symbol, invocation, and exact normalized signature helpers | Yes | Yes |
| Bounded scalar, `typeof`, payload, and collection extraction | Yes | Yes |
| Overload-safe subjects, alternate source owners, and flat compatibility placement | Yes | Yes |
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

## Declare method-dependent observations

On current `main`, an adapter can attach a `MethodFactSupportFact` to a semantic candidate from the same contribution. Declare the `MethodFactSupport` capability. Supply the candidate's exact fact ID and subject, with one or more independent support alternatives. Each alternative identifies the exact executable, its optional backend occurrence, its explicit lexical enclosing executables, and any diagnostics conditional on that observation. Alternatives are OR; an executable and its supplied lexical parents are AND. Do not infer ancestry from names or source ranges.

A frontend declaring `MethodExecutionClassification` may contribute `MethodExecutionClassificationFact` with exact evidence. `Ordinary` has no omission reason. `FailureOnly` carries the frontend's original located warning. Executable identity includes project, signature, overload and generic identity; null scope selects only the unscoped occurrence. Classification has no name-based or single-candidate fallback.

Generation selects eligible inputs **before** type binding, relationship joins, graph resolution and lowering. A failure-only executable or lexical parent blocks that alternative. An independent ordinary alternative retains the candidate. Missing classification preserves compatibility; malformed or conflicting supplied metadata fails closed. Candidates without support facets retain legacy behavior. Structural artifacts are not implicitly removed with their behaviors.

Keep conditional semantic diagnostics inside the alternative. Top-level adapter, host and source-authority diagnostics are unconditional and are never suppressed by eligibility. Relevant failure-only warnings retain their original code, text, severity, subject and location, even if another independent alternative survives. The admitted snapshot retains the candidate and metadata with explicit dispositions; derived lineage includes only eligible support/classification dependencies under rule `cratis.screenplay.method-fact-eligibility` version `1.0.0`.

These are source-neutral proof contracts, not C# callback recognition. Ecosystem adapters must provide exact executable/lexical ownership and retain unproved execution as unknown rather than inventing ordinary behavior.

## Project a method diagnostic

Declare `GenerationFactCapability.MethodDiagnosticProjection` (28) to contribute `MethodDiagnosticProjectionFact`. Set its exact method subject, backend scope, Command or Query `EntryPointRole`, and `Mode = MethodDiagnosticProjectionMode.EntryPointArtifact`. Its located diagnostic payload must match the intent's exact evidence and subject, reference nonempty same-contribution support facts, and omit engine-owned `Lineage`. Provide one same-contribution `MethodFactSupportFact` whose alternatives all name that exact method and scope. Do not also contribute the payload as an ordinary or alternative diagnostic.

Generation applies eligibility first, then independently resolves exact entry-point facts for that method, role, and scope. Null scope is unscoped, never a wildcard. It does not infer ownership from names, containers, property shapes, or provider-specific symbols. Repeated proofs of one artifact are not ambiguous. Failure-only observations do not project their payloads.

A unique mapping changes only the payload's diagnostic subject to the artifact subject. Its original code, message, severity, outcome, location, and support references remain intact. Engine-owned diagnostic `Lineage` retains the intent, local observations, eligible support/classification dependencies, exact entry-point proofs, and scope evidence. The diagnostics-only rule `cratis.screenplay.method-diagnostic-projection` version `1.0.0` has no fact outputs. Missing mappings report `GEN0081`; ambiguous mappings report `GEN0082`. Malformed direct inputs fail closed with `GEN0080`. Legacy diagnostics retain null lineage and unchanged meaning.

## Transfer declaration-owned inputs

A reusable library can contribute `DeclarationInputMemberDeclarationFact` and `DeclarationInputMemberTypeUseFact` without inventing an artifact role. An application adapter opts an exact query endpoint into transfer with `EntryPointInputTransferFact`, its exact `EntryPointFact`, and ordered `MethodArtifactUsageFact` observations. Direct endpoint members win; proved execution order then source member order determines additions. Full substituted type and named-binding proof are retained in one fixed-snapshot derivation pass. Plain `Reads` facts remain relationship-only.

See [Derive query inputs from declaration surfaces](query-input-member-derivation.md) for capabilities, ownership, precedence, omissions, eligibility and lineage.

## Establish the analysis context in the host

The workspace host owns project loading and source authority. `Cratis.Screenplay.Generation.DotNet` deliberately does not reference `MSBuildWorkspace`; an MSBuild host additionally references `Microsoft.CodeAnalysis.Workspaces.MSBuild` and `Microsoft.Build.Locator`. The host supplies a `DotNetAnalysisContext` built from its project compilations and authored source sets; adapters consume that context rather than loading the application.

For each project, supply a `DotNetProjectCompilation` with:

- a stable logical `Name`;
- the Roslyn `Compilation`;
- the exact `AuthoredSyntaxTrees` captured from project documents;
- an optional `DotNetProjectSourceContext` for stable source identity and display paths;
- `DotNetProjectRole.Application` or `DotNetProjectRole.Specifications`;
- optional compatibility values such as `ProjectPath` and `SourceRoot`.

The host example below constructs the context consumed by the adapter and runner. Replace the shown application-project path with a project whose documents and compilation references you control; workspace failures and compilation errors stop context construction before adapter analysis.

```csharp
using Cratis.Screenplay.Generation.DotNet;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

MSBuildLocator.RegisterDefaults();
using var workspace = MSBuildWorkspace.Create();
var workspaceFailures = new List<string>();
workspace.WorkspaceFailed += (_, args) => workspaceFailures.Add(args.Diagnostic.Message);
var projectPath = Path.GetFullPath("Source/Application/Application.csproj");
var workspaceRoot = Path.GetFullPath(".");
var loadedProject = await workspace.OpenProjectAsync(projectPath);
var projectDirectory = Path.GetDirectoryName(projectPath)!;
var authoredTrees = new HashSet<SyntaxTree>();
var sourceDocuments = new List<DotNetSourceDocument>();

foreach (var document in loadedProject.Documents.OrderBy(_ => _.FilePath, StringComparer.Ordinal))
{
    if (document.FilePath is null || await document.GetSyntaxTreeAsync() is not { } tree)
    {
        continue;
    }

    authoredTrees.Add(tree);
    sourceDocuments.Add(new DotNetSourceDocument
    {
        SyntaxTree = tree,
        ProjectRelativePath = Path.GetRelativePath(projectDirectory, document.FilePath),
        WorkspaceRelativePath = Path.GetRelativePath(workspaceRoot, document.FilePath)
    });
}

var compilation = await loadedProject.GetCompilationAsync() ??
                  throw new InvalidOperationException("The project did not produce a compilation");
var compilationErrors = compilation.GetDiagnostics()
    .Where(_ => _.Severity == DiagnosticSeverity.Error)
    .ToArray();
if (workspaceFailures.Count > 0 || compilationErrors.Length > 0)
{
    throw new InvalidOperationException(string.Join(
        Environment.NewLine,
        workspaceFailures.Concat(compilationErrors.Select(_ => _.ToString()))));
}

var sourceContext = DotNetSourcePaths.Create(
    "Source/Application/Application",
    new DotNetSourcePathPolicy
    {
        Version = 1,
        DisplayRoot = DotNetSourceDisplayRoot.Workspace,
        CasePolicy = DotNetSourcePathCasePolicy.Ordinal
    },
    sourceDocuments);
var project = new DotNetProjectCompilation
{
    Name = loadedProject.Name,
    Role = DotNetProjectRole.Application,
    ProjectPath = projectPath,
    SourceContext = sourceContext,
    Compilation = compilation,
    AuthoredSyntaxTrees = authoredTrees
};
var context = new DotNetAnalysisContext([project]);
```

Capture authored trees from `Project.Documents`. Generated filenames and headers are useful corroboration, but they are not source authority. Assign `DotNetProjectRole.Specifications` to specification projects instead of inferring that role from their name.

Use `DotNetSourcePaths.Create(...)` to map project documents into a stable source context. Physical checkout roots must not become identities. Prefer `DotNetSource.EvidenceFor(..., project, ...)` and `DotNetSource.RangeForProject(...)` over the legacy `SourceRoot` overloads.

A modern adapter should declare `AdapterHostCapability.StableSourceLocations` when its probe, facts, or diagnostics depend on portable source identity. The runner then requires every project to expose authoritative authored trees and a complete `DotNetProjectSourceContext`; located probe evidence and contribution ranges must map to those exact trees and include their stable `SourceFileIdentity`. Missing host capability blocks before `Probe()`, malformed or nonauthoritative probe evidence blocks after `Probe()`, and nonauthoritative contribution source rejects that contribution atomically. The modern Vogen descriptor requires this capability. Legacy registrations retain path-only source compatibility.

A truly source-neutral adapter can declare `AdapterSourceLanguage.SourceIndependent`, no host capabilities, and no source ranges. It can run with `new DotNetAnalysisContext([])`. Declaring any host capability intentionally restores host and project-roster gating.

## Implement the adapter contract

The original `IDotNetScreenplayAdapter` remains supported for existing adapters and hosts. New adapters should implement `IDescribedDotNetScreenplayAdapter`. If you also support legacy hosts, implement both interfaces over one analysis path: modern hosts receive a descriptor and structured probe, while legacy hosts retain `Identity`, `CanAnalyze()`, and byte-compatible contributions. The example below follows that dual path and uses current-main granular type-use APIs.

A descriptor states what the host is about to trust. Choose the narrow semantic `AdapterCategory` (`ApplicationFramework`, `EventSourcing`, `EventStore`, `Messaging`, `Concepts`, `Validation`, or `Integration`), declare `CSharp` or truly `SourceIndependent` input, bound compatible Generation versions when the adapter has a tested range, list required host services, name exact framework API capabilities that an applicable probe must prove, and list every neutral fact family analysis may emit. `Legacy` is reserved for the compatibility registration synthesized by `ForLegacy(...)`.

```csharp
using Cratis.Screenplay.Generation;
using Cratis.Screenplay.Generation.DotNet;
using Microsoft.CodeAnalysis;

namespace Acme.Screenplay;

public sealed class AcmeScreenplayAdapter :
    IDescribedDotNetScreenplayAdapter,
    IDotNetScreenplayAdapter
{
    static readonly AdapterApiCapability _commandDeclarationApi = new()
    {
        Id = "acme.command-declaration"
    };

    public AdapterDescriptor Descriptor { get; } = new()
    {
        Identity = new AdapterIdentity { Id = "acme", Version = "1.0.0" },
        SourceLanguage = AdapterSourceLanguage.CSharp,
        Category = AdapterCategory.ApplicationFramework,
        RequiredHostCapabilities =
        [
            AdapterHostCapability.AuthoredSource,
            AdapterHostCapability.StableSourceLocations,
            AdapterHostCapability.SemanticAnalysis
        ],
        RequiredApiCapabilities = [_commandDeclarationApi],
        EmittedFactCapabilities =
        [
            GenerationFactCapability.Artifact,
            GenerationFactCapability.ArtifactMemberDeclaration,
            GenerationFactCapability.ArtifactMemberTypeUse
        ]
    };

    // Legacy compatibility surface.
    public AdapterIdentity Identity => Descriptor.Identity;

    // A blocked modern probe maps to false because the legacy Boolean cannot report why analysis is unsafe.
    public bool CanAnalyze(DotNetAnalysisContext context) =>
        Probe(context) is AdapterProbeApplicable;

    public AdapterProbeResult Probe(DotNetAnalysisContext context)
    {
        var declarations = context.Projects
            .SelectMany(project => new DotNetArtifactCatalog(project.Compilation).Types
                .SelectMany(type => DotNetSource.AuthoredAttributesOf(type, project.AuthoredSyntaxTrees)
                    .Where(attribute =>
                        attribute.AttributeClass is not null &&
                        DotNetSubjectIds.MetadataName(attribute.AttributeClass) == "Acme.CommandAttribute")
                    .Select(attribute => (Project: project, Type: type, Attribute: attribute))))
            .ToArray();
        if (declarations.Length == 0)
        {
            return new AdapterProbeNotApplicable();
        }

        return new AdapterProbeApplicable
        {
            Evidence =
            [
                .. declarations.Select(declaration => new AdapterProbeEvidence
                {
                    Description = "An authored type uses the exact Acme command declaration API",
                    ApiCapability = _commandDeclarationApi,
                    Source = DotNetSource.RangeForProject(
                        declaration.Attribute.ApplicationSyntaxReference!.GetSyntax().GetLocation(),
                        declaration.Project),
                    Subject = declaration.Project.SubjectForType(declaration.Type)
                })
            ]
        };
    }

    public AdapterContribution Analyze(
        DotNetAnalysisContext context,
        DotNetAdapterOptions options)
    {
        var facts = new List<GenerationFact>();
        var diagnostics = new List<GenerationDiagnostic>();

        foreach (var project in context.Projects)
        {
            foreach (var type in new DotNetArtifactCatalog(project.Compilation).Types)
            {
                var commandAttribute = DotNetSource
                    .AuthoredAttributesOf(type, project.AuthoredSyntaxTrees)
                    .FirstOrDefault(attribute =>
                        attribute.AttributeClass is not null &&
                        DotNetSubjectIds.MetadataName(attribute.AttributeClass) == "Acme.CommandAttribute");
                if (commandAttribute is null)
                {
                    continue;
                }

                var subject = project.SubjectForType(type);
                var evidence = DotNetSource.EvidenceFor(
                    commandAttribute,
                    Identity,
                    project,
                    EvidenceStrength.Exact,
                    "The authored type has Acme.CommandAttribute");
                var key = new ArtifactKey
                {
                    Subject = subject,
                    Kind = ArtifactKind.Command
                };

                facts.Add(new ArtifactFact
                {
                    Id = new FactId { Value = $"acme:command:{subject.Value}" },
                    Subject = subject,
                    Definition = new ArtifactDefinition
                    {
                        Key = key,
                        Name = type.Name,
                        File = evidence.Source?.Path,
                        Properties = DotNetTypeShapes.PropertiesOf(type)
                    },
                    Evidence = evidence
                });
                facts.AddRange(DotNetTypeUseFacts.Emit(type, key, context, evidence));
            }
        }

        return new AdapterContribution
        {
            Adapter = Identity,
            Facts = facts,
            Diagnostics = diagnostics
        };
    }
}
```

Keep `Probe()` cheap, deterministic, and semantic. Package presence alone must not make a probe applicable. `AdapterProbeEvidence` can identify an exact required `AdapterApiCapability`, source range, and subject. Return `AdapterProbeBlocked` with valid diagnostics when the source applies but analysis cannot proceed safely. The runner admits and freezes probe evidence before it considers analysis.

The compatibility `CanAnalyze()` above delegates to the modern probe without changing `Analyze()`. Existing binaries can continue to call the legacy interface, while a modern host registers the same adapter with `DotNetAdapterRegistration.For(...)`. This discovery pass emits the exact command artifact plus member-declaration and type-use facts for its declared public readable instance properties, but no placement. Add placement through the fixed source snapshot and shared derivation pipeline in [Derive source placement](#derive-source-placement); do not derive it ad hoc inside artifact discovery.

## Compose and verify once

The host supplies an explicit registration roster. There is no package scanning or implicit adapter discovery. Pass the host's context and adapter options to the runner, then pass its admitted snapshot to Generation. The roster below includes an existing legacy adapter; omit that registration when the host selects only modern adapters:

```csharp
var roster = new DotNetAdapterRegistration[]
{
    DotNetAdapterRegistration.For(new AcmeScreenplayAdapter()),
    DotNetAdapterRegistration.For(new VogenConceptScreenplayAdapter()),
    DotNetAdapterRegistration.ForLegacy(unchangedLegacyAdapter)
};

var snapshot = DotNetAdapterRunner.Run(roster, context, options);
var result = new ScreenplayDefinitionGenerator().Generate(
    snapshot,
    new ScreenplayGenerationOptions { Domain = "Ordering" });
```

`DotNetAdapterRunner` canonicalizes the roster and project input, considers every registration once, probes each eligible adapter once, and analyzes each applicable adapter once. `NotApplicable` and `Blocked` adapters never execute. Contributions remain separate and are admitted atomically before the runner returns a deeply frozen `AdapterRunSnapshot`; mutating adapter-owned lists or records after `Run()` cannot change it.

The boundary fails closed at a precise stage:

| Failure | Result |
| --- | --- |
| Duplicate adapter ID | Every duplicate registration is `RosterRejected` before probe or analysis |
| Invalid descriptor | Registration is `RosterRejected` with deterministic descriptor-admission diagnostics |
| Incompatible Generation version | `Blocked` before probe against the host's loaded `Generation.Contracts` version |
| Unsupported language or missing host capability | `Blocked` before probe |
| Ambiguous or duplicate project identity | Source-dependent adapters are `Blocked` before probe; a host-free source-independent adapter may continue |
| Malformed or nonauthoritative probe evidence, or missing required API evidence | `Blocked` after one probe; analysis does not run |
| Probe-declared known limitation | `AdapterProbeBlocked` preserves canonical diagnostics; analysis does not run |
| Malformed, unscoped, undeclared, identity-mismatched, or nonauthoritative contribution | The complete contribution is `ContributionRejected`; no partial facts enter the snapshot |
| Probe or analysis callback throws | `ExecutionFailed` with a stable boundary diagnostic; exception details and machine paths are not exposed |

`AdapterRunSnapshot.Adapters` preserves each descriptor, structured probe, execution result, and disposition. Its admitted facts initially have `GenerationFactDisposition.Unknown`. `Generate(snapshot, options)` returns a new canonical snapshot in `GeneratedScreenplayDefinition.AdapterRun` with every admitted fact classified:

| Disposition | Meaning |
| --- | --- |
| `Lowered` | The fact contributed directly to emitted Screenplay syntax |
| `ProvenanceOnly` | The assertion was retained as supporting provenance but did not add syntax |
| `OmittedWithDiagnostic` | Generation omitted the fact and attached the diagnostic that explains why |
| `Conflicted` | The fact participated in an unresolved competing definition |

The snapshot overload preserves runner, contribution, resolution, lowering, and verification diagnostics. For the same admitted contributions it produces the same canonical bytes as the original `Generate(IEnumerable<AdapterContribution>, ...)` overload. The original overload and `IDotNetScreenplayAdapter` remain supported; use them only when a host does not need the execution record.

After generation, inspect `result.Diagnostics` even when `result.IsSuccess` is true. Check `result.Graph` or `result.Source` for the artifacts and relationships established by your authored input, and use `result.AdapterRun` to inspect adapter admission and final fact dispositions. The composition example does not define a fixture or prescribe particular generated source.

`VogenConceptScreenplayAdapter` implements both interfaces. Prefer the modern registration when the host supplies stable mappings:

```csharp
var modern = DotNetAdapterRunner.Run(
    [DotNetAdapterRegistration.For(new VogenConceptScreenplayAdapter())],
    stableContext,
    options);

var legacy = DotNetAdapterRunner.Run(
    [DotNetAdapterRegistration.ForLegacy(new VogenConceptScreenplayAdapter())],
    legacyCompatibleContext,
    options);
```

The modern descriptor has category `Concepts`, source language `CSharp`, requires authored source, stable source locations, semantic analysis, and exact Vogen declaration API evidence, and declares its concept fact families. Its probe distinguishes no declarations, safely applicable declarations, and unsafe mappings. Run the modern and legacy registrations separately: both use the `vogen` identity, so placing both in one roster is a deliberate duplicate rejection. When each path is safely applicable, their contribution facts and diagnostics are identical.

Adapters never call the runner, resolver, lowerer, printer, or compiler themselves. Adopt a newly required API in this order:

1. release Screenplay Generation;
2. upgrade and release the ecosystem adapter;
3. upgrade the composition host.

## Use shared Roslyn mechanics

Use the shared helpers for symbol binding, bounded extraction, and evidence so each discovery rule supplies only the framework-specific meaning. These helpers do not decide which artifacts or relationships your framework proves:

- `DotNetArtifactCatalog` provides canonical source type discovery;
- `DotNetTypeShapes` converts public properties and exact type subjects;
- `DotNetSymbols` handles metadata-name attributes and interfaces, collection elements, named arguments, and companion method families;
- `DotNetInvocations` resolves exact direct and reduced methods, formal-parameter arguments, and bounded receiver roots;
- `DotNetMethodSignatures` captures and matches exact normalized containing type, nullability, method/static/extension/generic/return/ref, and ordered parameter/ref/`params`/receiver shape;
- `DotNetSourceValues` returns `DotNetKnown<T>` only for exact bounded constants, types, payloads, and collections and `DotNetUnknown<T>` with deterministic failures otherwise;
- `DotNetSource` establishes authored declarations, attributes, evidence, and ranges.

`DotNetInvocations.MethodFor(...)` returns `null` when Roslyn has no exact symbol. Do not use candidate symbols to guess through a broken compilation. Use `DefinitionOf(...)` before matching generic extension metadata, `ArgumentForParameter(...)` with the owning semantic model instead of positional assumptions, and `ReceiverRootParameter(...)` only for a bounded direct parameter-root check. Omitted optional parameters, expanded `params` arguments, aliases, invocation results, and unqualified instance receivers resolve to `null` rather than a partial guess.

Create a `DotNetMethodSignature` from the exact allowlisted method symbol available to the analyzed compilation, then match bound candidates with `DotNetMethodSignatures.Matches(...)`. The descriptor retains nullability and symbol identity, so it is intentionally compilation-bound; do not reconstruct expected signatures from display strings, aliases, short names, or symbols from an unrelated compilation.

Use `DotNetSourceValues.Extract(...)` or the typed `Constant<T>(...)` / `TypeOf(...)` / `Payload(...)` / `Collection(...)` helpers instead of reading `ConstantValue` or walking initializer syntax directly. Pattern-match `DotNetKnown<T>` and publish dependent facts only from its value; a `DotNetUnknown<T>` exposes every deterministic child failure and no partial value. Typed constants are limited to an exact primitive, string, or null runtime type. Source enum constants stay as exact `IFieldSymbol` values through untyped extraction so aliases are preserved; they are never exposed as underlying numbers by `Constant<T>`.

Payloads order constructor values by formal parameter and initializer values by authored order. Every payload constructor parameter must be supplied explicitly; expanded `params`, omitted optional values, indirect/index initializers, duplicate members, or one unknown nested child make the complete payload unknown. Collections preserve each element's exact location and support explicit or implicit arrays whose constructed outer rank is one, including recursively exact jagged arrays, plus collection expressions and direct collection initializers whose `Add` operation has exactly one authored argument. Compiler-supplied optional or `params` constructor and `Add` arguments are runtime effects, not authored collection elements, and remain allowed when the initializer entry authors exactly one `Add` argument. Computed or mismatched dimensions, invalid type or conversion binding, multi-argument `Add` entries, opaque spreads, and any unknown nested element fail the whole collection atomically. A payload or collection shape failure does not stop inspection of explicit authored arguments, right-hand sides, or elements: every deterministic child failure is reported without publishing partial values.

Adapter discovery depends on compiler symbols and semantic models, not preferred source formatting. Keep one reusable source fixture with semantically valid generated, decompiler-style, fully qualified, explicitly cast, and otherwise non-idiomatic C#. Its supported facts must resolve without extra loss.

## Use stable identities

Three identities serve different purposes:

- `AdapterIdentity` identifies the adapter implementation and version.
- `SubjectId` identifies the source entity that facts describe.
- `FactId` identifies one semantic assertion.

Use project-qualified identities:

```csharp
var typeSubject = project.SubjectForType(type);
var methodSubject = project.SubjectForMethod(method);
```

`SubjectForMethod()` uses a .NET documentation identity so overloads, generic arity, arrays, and parameter modifiers do not collide. Use `DotNetSubjectIds.MethodDisplayName(method)` for readable diagnostics; never use a short method name as identity.

Fact IDs must be globally stable and unique. Prefix them with the adapter and semantic role, and add a discriminator when one source member can produce several same-kind assertions.

## Contribute neutral facts

Use the smallest fact vocabulary that says what the source proves:

- `ArtifactFact` — the compatibility aggregate for a command, event, read model, projection, reaction, message, handler, concept, or another supported role;
- `ArtifactDeclarationFact` and `ArtifactMemberDeclarationFact` — independent artifact metadata and one ordered member declaration without repeating a complete property list;
- `ArtifactMemberTypeUseFact` — one exact use-site type shape and observed source subject;
- `TypeUseBindingFact` — an exact member-to-artifact binding, normally produced by fixed-snapshot derivation;
- `ArtifactMemberRoleFact` — an explicitly established typed identifier or event-source-identifier role;
- `ArtifactPlacementFact` — module, feature, slice, and independently established slice kind;
- `RelationshipFact` — handles, reads, produces, consumes, builds, returns, cascades, publishes, starts or appends streams, or document persistence;
- concept representation, attribute, and validation facts;
- specification scenario, step, and typed value facts.

Do not overload a nearby role. A published message is not a persisted event. A document is not an event-built read model unless source evidence proves the projection. A response is not a cascade.

Keep compatibility aggregate properties unbound with `DotNetTypeShapes.PropertiesOf(type)` when another adapter may declare their target concepts. Append `DotNetTypeUseFacts.Emit(type, artifact, context, evidence)` so each member independently records declaration order, exact use-site shape, and the terminal project-qualified source subject. Fixed-snapshot derivation then emits a granular binding without rewriting the aggregate.

`DotNetTypeShapes.TypeUseFor(type, context)` orders shape nodes from the outermost wrapper to the terminal `Named` node. This distinguishes `Collection(Optional(Named))` from `Optional(Collection(Named))` and preserves nested collections. The current Screenplay grammar lowers only the shapes it can express exactly; unsupported distinctions remain diagnosed rather than flattened.

Pass a `roleFor` callback to `DotNetTypeUseFacts.Emit(...)` only when source-framework semantics establish `ArtifactMemberRoleKind.Identifier` or `EventSourceIdentifier`. Never infer either role from a property name or primitive type. `EventSourceIdentifier` lowers through the existing identifier syntax; the distinct ordinary `Identifier` role remains provenance until Screenplay has separate syntax for it. A framework that already knows the exact target inside one adapter may continue setting `TypeReferenceDefinition.Subject` directly with `PropertiesOf(type, context)` or `TypeReferenceFor(type, context)`.

### Derive facts from one fixed admitted snapshot

`GenerationFactDerivation.Derive(...)` runs the closed built-in rule set once over `AdapterRunSnapshot.Facts`. Every rule sees the same deeply frozen base array. A rule never consumes another rule's output, inspects adapter registrations or instances, or reopens source-language state.

The type-use binding rule joins an `ArtifactMemberTypeUseFact` to an exact declared `ArtifactKey`, including its role and subject. It does not join by display name and does not replace the owning artifact's complete property list. Its derived `TypeUseBindingFact` remains separate from the admitted base facts under `AdapterRunSnapshot.Derivation`; the resolved compatibility reference retains the role in `TypeReferenceDefinition.TargetArtifactKind` so a same-subject non-concept can never be substituted as a concept.

Each derived `GenerationFactRecord` carries `GenerationFactLineage`: the stable derivation rule identity and version, canonical input `FactId` references, and complete input evidence. `GenerationDerivationRuleRecord` records the fixed inputs, outputs, and diagnostics for that rule execution. Directly invoking derivation leaves fact dispositions unknown because disposition is a later generation decision. `Generate(snapshot, options)` attaches the derivation result, propagates its diagnostics, resolves admitted and derived granular facets together, and applies an exact member binding as an overlay without publishing a replacement aggregate fact.

Exact subjects can come from any source frontend. A C# member type use can bind a declaration contributed by another adapter, while a source-independent or non-.NET adapter can contribute the same neutral contracts without Roslyn or Screenplay-layout dependencies. Missing, malformed, ambiguous, conflicting, or currently unrepresentable member evidence omits the affected artifact instead of lowering a flattened legacy fallback.

The execution and derivation snapshots are not a history model. They have no serializer or stable fingerprints; keep them in process and compare canonical generated bytes when determinism matters.

## Nominate declared concepts

When a framework explicitly declares a wrapper as a domain value through an authored attribute or registration API, use:

```csharp
var facts = DotNetConceptFacts.Emit(
    wrapper,
    backing,
    project.SubjectForType(wrapper),
    conceptEvidence,
    representationEvidence);
```

Do not infer concepts from names such as `Id`, from having one property, or from an arbitrary record-struct shape. If the framework nominates the concept but does not prove its primitive backing, retain the concept artifact and emit an adapter-owned `Unsupported` diagnostic instead of inventing a representation.

### Retain backend scope provenance

A stored occurrence is identified by `ArtifactKey`'s exact `(Subject, Kind, Scope)`. `BackendScopeKey` pairs the exact store-registration `SubjectId` with an optional, case-sensitive `EventModelName`; `BackendScopeDefinition.Name` is display metadata only. Emit a located `BackendScopeFact` for each registration and declare the appended `BackendScope` capability (17). Source authority remains the host's responsibility. Do not derive store identity from placement, CLR display names, or another adapter's private state.

Set `ArtifactKey.Scope` on explicitly proved stored occurrences, and use independent `RelationshipKey.SourceScope` / `TargetScope` selectors for endpoints, including explicitly proved cross-store edges. Named type references use `TypeReferenceDefinition.Scope`; granular type uses use `ObservedScope`. A scoped type target requires its exact observed subject. `MethodArtifactUsageDefinition.SourceScope` and `TargetScope` select the independently proved entry-point and declaration occurrences. Null always selects the legacy unscoped occurrence: it never inherits the owner store, selects a sole scoped candidate, or fans out to several stores. Concepts and queries remain global in this version.

The resolver retains declarations and evidence in `Graph.BackendScopes`. Missing scope proof or an unqualified sole scoped candidate is Unknown; an unqualified reference with several scoped candidates is Conflict. Invalid inputs cannot produce derived relationships or bindings. Derived IDs and complete lineage retain both endpoint scopes and the scope declarations used as proof. Existing null-scope identities, member IDs, placement, source subjects, and structural output are unchanged.

**Backend scope is provenance, not executable Screenplay storage semantics.** Current `.play` cannot express store registration or event-model scope. Noncolliding structural syntax is compiler-verified, but affected emitted facts are `PartiallyLowered` (4), with the located `BackendScopeNotSupported` diagnostic. Scope and usage declarations remain `ProvenanceOnly`; they are not fictitious syntax. Scope never invents modules, slices, filenames, `eventsource`, or `sink` declarations. Distinct occurrences that would erase to the same emitted name/kind in the same module/feature/slice namespace are all omitted with `BackendScopeOutputCollision` (Error); `IsSuccess` is false. Host-selected distinct namespaces remain valid. No occurrence is renamed, merged, moved, or chosen arbitrarily.

Scope diagnostics are `UnknownBackendScope` (GEN0059), `ConflictingBackendScope` (GEN0060), `AmbiguousBackendScopeOwnership` (GEN0061), `BackendScopeNotSupported` (GEN0062), `BackendScopeOutputCollision` (GEN0063), and `InvalidBackendScopeUsage` (GEN0064). The language follow-up is [Cratis/Screenplay#1352](https://github.com/Cratis/Screenplay/issues/1352); this package adds no `.play` grammar.

### Join independently proved artifact usage

An independently composed library adapter can emit `ArtifactUsageDeclarationFact` (exact declaration → exact `ArtifactKey`) and `MethodArtifactUsageFact` (exact containing method → executed declaration, with a proved relationship role). An entry-point adapter separately emits `EntryPointFact` (that exact method → source `ArtifactKey`). Fact subjects equal their declaration or method, respectively. Each adapter declares the matching appended `GenerationFactCapability`; existing capability numbers are unchanged.

The closed built-in `cratis.screenplay.artifact-usage` rule, version `1.0.0`, derives `RelationshipFact` with `Reads` only when all three facets and both compatible exact artifact declarations agree. Use project-, overload-, and generic-qualified identities, for example `DotNetSubjectIds.ForMethod`; never join display names or treat a declaration mention as executed usage. Library methods with no entry point remain provenance. Agreeing proofs consolidate into one edge with every input ID and evidence item retained. Conflicting mappings never fan out, and missing mappings, incompatible declarations, or unsupported roles produce located, input-linked diagnostics (GEN0056–GEN0058). Derived outputs are not inputs to another rule in that run; there is no plug-in rule registry.

These neutral facets are provenance, not fictitious Screenplay syntax. The resulting relationship is resolved and compiler-verified through the normal pipeline. Legacy adapters can continue emitting `RelationshipFact` directly; primitive wrappers remain the responsibility of concept adapters such as `DotNetConceptFacts`.

### Opt in to inherited members

Existing `DotNetTypeShapes.PropertiesOf` and `DotNetTypeUseFacts.Emit` calls remain declared-only. For an effective surface, pair `DotNetTypeShapes.PropertiesOf(type, context, DotNetMemberSurfaceScope.Inherited)` with `DotNetTypeUseFacts.EmitWithSurface(type, artifact, context, evidence, DotNetMemberSurfaceScope.Inherited)`. Pairing preserves the legacy/granular member order; mismatched surfaces can produce GEN0050 and omit the artifact. The distinct emission method name keeps existing calls with a default role argument source-compatible.

Inherited mode walks base classes, not interfaces, derived-first. By default, only public readable instance non-indexer properties proven to originate in analyzed authored syntax trees are eligible; generated, non-authored, and metadata-only bases contribute no members. Constructed generic types retain substitutions. The most-derived eligible raw CLR name wins, so a private getter does not hide an eligible base property. Distinct raw names that normalize to the same member remain separate assertions for existing conflict handling. All facts use the requested artifact and caller evidence; retained properties alone receive roles and contiguous ordinals. Undefined scopes are rejected.

An adapter whose framework also discovers properties from metadata, non-authored, or generated bases can explicitly select `DotNetMemberOriginPolicy.AllOrigins`:

```csharp
var properties = DotNetTypeShapes.PropertiesOf(
    type, context,
    DotNetMemberOriginPolicy.AllOrigins,
    DotNetMemberSurfaceScope.Inherited);

var facts = DotNetTypeUseFacts.EmitWithSurface(
    type, artifact, context, evidence,
    DotNetMemberOriginPolicy.AllOrigins,
    DotNetMemberSurfaceScope.Inherited,
    property => property.Name == "Id" ? ArtifactMemberRoleKind.Identifier : null);
```

Use a role callback only when source-framework semantics establish the role, not merely because a property is named `Id`. `AuthoredOnly` is the default and preserves the existing output. Both policies use identical generic substitution, shadowing, ordering, fact identity, and caller provenance rules; metadata properties without source locations sort by name. `System.Object` contributes no properties, and the protected record `EqualityContract` is never selected. The policy does not affect declared mode, artifact admission, the host's authored-tree set, or source authority. Including a base property does not authorize nominating that base as an artifact or using its generated or metadata declaration as authored evidence. The policy precedes the scope in the new overloads to keep existing default-literal role calls unambiguous. Undefined policies and scopes are rejected.

### Preserve enumeration numbers

For an enumeration representation, `ConceptRepresentationDefinition.EnumerationNumbers` optionally supplies one unique int32 number per `EnumerationValues` entry, in the same declaration order. Null retains the existing positional output. Explicit numbers preserve negative, zero, sparse, and int32 boundary values as `name = number` in compiler-verified Screenplay. Numbers cannot be supplied on primitive concepts; mismatched counts and duplicate numbers are rejected rather than renumbered. Distinct explicit numbers remain distinct resolver variants, including explicit positional numbers versus an implicit representation.

## Derive source placement

Source layout does not decide semantic role. First establish `ArtifactKind` and `GenerationSliceKind` from framework semantics. Then use the shared placement pipeline:

1. Create a fixed snapshot with `DotNetSourceStructures.Create(context)`.
2. Find the source structure for the artifact subject, or for an exact different source owner established by adapter semantics.
3. Build a `DotNetSourcePlacementRequest` with the artifact key, unchanged structure, slice kind, and `options.SourceStructurePolicy`. Set `SourceOwner` only when the artifact is method-backed or synthetic and its exact owner is known.
4. Optionally supply an exact, versioned `DotNetSourcePlacementCompatibilityPolicy` for a legacy placement that may be used only after strict `DOTNETSP0004` insufficient-structure failure.
5. Call `DotNetSourcePlacementDerivation.Derive(...)` once for the complete request set.
6. Convert successful placements to `ArtifactPlacementFact` values, retain every placement diagnostic, and pass the result policy and compatibility fields to host provenance.

```csharp
var structureSnapshot = DotNetSourceStructures.Create(context);
diagnostics.AddRange(structureSnapshot.Diagnostics);
var structures = structureSnapshot.Structures.ToDictionary(_ => _.Subject);
var placementRequests = new List<DotNetSourcePlacementRequest>();
foreach (var artifact in facts.OfType<ArtifactFact>())
{
    if (!structures.TryGetValue(artifact.Subject, out var structure))
    {
        diagnostics.Add(new GenerationDiagnostic
        {
            Code = "ACME0001",
            Severity = GenerationDiagnosticSeverity.Error,
            Outcome = GenerationDiagnosticOutcome.Unknown,
            Message = $"Authored source structure for '{artifact.Definition.Name}' is unavailable",
            Source = artifact.Evidence.Source,
            Subject = artifact.Subject
        });
        continue;
    }

    placementRequests.Add(new DotNetSourcePlacementRequest
    {
        Artifact = artifact.Definition.Key,
        Structure = structure,
        SliceKind = GenerationSliceKind.StateChange,
        Policy = options.SourceStructurePolicy
    });
}

var placementSnapshot = DotNetSourcePlacementDerivation.Derive(placementRequests);
diagnostics.AddRange(placementSnapshot.Diagnostics);
foreach (var placement in placementSnapshot.Placements)
{
    var artifact = facts.OfType<ArtifactFact>().Single(_ =>
        _.Definition.Key == placement.Artifact);
    facts.Add(new ArtifactPlacementFact
    {
        Id = new FactId { Value = $"acme:placement:{placement.Artifact.Kind}:{placement.Artifact.Subject.Value}" },
        Subject = placement.Artifact.Subject,
        Artifact = placement.Artifact,
        Placement = placement.Placement,
        Evidence = artifact.Evidence with
        {
            Strength = EvidenceStrength.Heuristic,
            Explanation = "Host-owned source structure provides the default Screenplay placement"
        }
    });
}
```

An adapter with several behavior kinds supplies the independently proven `GenerationSliceKind` for each artifact instead of assigning `StateChange` universally. Snapshot diagnostics and derivation diagnostics both belong in the adapter contribution.

For a query represented by a method subject or a reducer represented by a synthetic subject, do not change `DotNetSourceStructure.Subject`. Set `SourceOwner` to the exact containing type, projection, or model subject. The request remains invalid with `DOTNETSP0012` when the nominated owner differs from the fixed structure, and two different owner requests for one artifact conflict with `DOTNETSP0013`.

Strict placement is the default. A compatibility policy contains the exact legacy `ArtifactPlacement`; Generation does not infer it from a filename. A valid policy can supply placement only when strict resolution returns exactly one `DOTNETSP0004` diagnostic; strict success still wins, while malformed compatibility reports `DOTNETSP0015`. Every other `DOTNETSP####` result remains blocking, including snapshot diagnostics `DOTNETSP0009` through `DOTNETSP0011`, which occur before any placement request exists. `DotNetSourcePlacement.UsedCompatibilityPlacement`, `CompatibilityReasonCode`, `Policy`, `CompatibilityPolicy`, and `SourceOwner` provide defensive, deterministic host/CLI provenance.

The host owns `FeatureRoot`, explicit module, and skipped namespace segments. Folder and namespace evidence must agree when both establish placement.

## Assign evidence strength deliberately

| Strength | Meaning |
| --- | --- |
| `Exact` | A bound invocation, attribute, interface, override, or return type directly proves the fact |
| `Configured` | Authored framework configuration proves the fact |
| `Conventional` | A documented framework convention applies after exact admission |
| `Heuristic` | Naming or structure suggests display or placement only |

A heuristic must never establish persisted events, stream ownership, authorization, or another correctness-critical role.

## Report loss instead of guessing

Each adapter owns a stable diagnostic-code range. Include these values in each diagnostic:

- a stable code;
- severity;
- an `Unknown`, `Conflict`, or `Unsupported` outcome when applicable;
- a precise message;
- a source range when authored source establishes the loss;
- the affected subject.

Use `Unknown` when source cannot establish the answer, `Conflict` when exact assertions disagree, and `Unsupported` when the behavior is known but cannot be represented. Never reuse a published code for a different condition.

When an authored framework extension can alter discovery or runtime conventions beyond what the adapter can prove statically, use a stable adapter-owned code and `GenerationDiagnosticOutcome.Unsupported`. Set `Subject` to the authored extension type. Name both the exact hook surface and affected convention scope:

```text
Authored extension '<type>' uses '<hook surface>' and may alter <scope>; the recovered model reflects default <scope> only.
```

This reports bounded default recovery without treating the entire framework as unrecognized. Do not interpret arbitrary policy bodies.

A host must always display every diagnostic, even when `IsSuccess` is `true`. Errors block publication. Compiler warnings retain their `PLAY` code, message, warning severity, and source position in both generation overloads; they do not make `IsSuccess` false. Hosts may explicitly require warning-free output by rejecting results with warning diagnostics, but the default verification path never hides them. Information diagnostics describe known bounded loss.

Tests must assert required artifacts and relationships directly from `Graph` or `Source`; `IsSuccess` alone proves only that no error diagnostic was produced.

## Canonical ESM admission foundation (part of #71)

`Cratis.Screenplay` 6.32.0 can deserialize more ESM versions than Generation admits. Compiler support is not Generation admission, and an inapplicable surface is not a successful run.

| Surface | Exact matching language/semantic pairs v1–v6 | v7 | Later or unlisted pairs |
| --- | --- | --- | --- |
| Reading | Admitted | Refused | Refused |
| Reverse recovery | Admitted for existing bounded adapter behavior | Refused | Refused |
| Reference execution | Inapplicable | Inapplicable | Inapplicable |
| Rendering | Inapplicable | Inapplicable | Inapplicable |

The legacy allowance retains the existing adapter scope and diagnostics; it does **not** claim that every legacy ESM construct can be recovered. Unknown surface values are refused. v7 reading and reverse recovery remain refused until the remaining admission work is complete ([part of #71](https://github.com/Cratis/Screenplay.Generation/issues/71)). This foundation does not implement v7 recovery, reference execution, or rendering.

Hosts receiving canonical ESM bytes use the atomic reader before processing them:

```csharp
var read = EsmCanonicalReader.Read(canonicalBytes, EsmSurface.ReverseRecovery);
if (!read.IsAdmitted)
{
    // Display every blocking diagnostic. Do not process or publish a recovered definition.
    return;
}

var model = read.Model!;
// Process only within the existing adapter's documented recovery scope.
```

`EsmCanonicalReader.Read` first uses the compiler's strict canonical deserializer, then applies `EsmAdmission.Evaluate` to the model's actual language version, semantic version, and requested surface. It does not rewrite bytes, versions, revisions, or facts, and never returns a partial model. `EsmAdmissionDecision.Disposition` and `Reason` expose the typed result without parsing a message. A refusal or inapplicable result has no model and both `IsAdmitted` and `IsSuccess` are false.

Non-admitted processing emits `GEN0065` (`Error` / `Unsupported`), identifying the surface and versions and linking to the remaining admission work. Invalid canonical input emits `GEN0066` (`Error` / `Unsupported`), including malformed JSON, duplicate or unknown members, incorrect revisions, trailing data, and noncanonical bytes. Only the compiler's `InvalidSemanticContract` becomes an invalid-input diagnostic; unrelated exceptions are not hidden.

Hosts must show the diagnostic and stop processing or publication. When an adapter carries the refusal through the existing contribution or adapter-run diagnostics, both `ScreenplayDefinitionGenerator.Generate` overloads preserve it and return `IsSuccess == false`; neither overload itself ingests canonical ESM. Even if a blocked generation result contains source text, the host must not publish it as a recovered definition. No adapter or source provider is automatically wired to this reader yet.

## Test the adapter

Every adapter should prove:

1. exact positive discovery;
2. close lookalikes do not match;
3. generated-only declarations cannot originate facts;
4. unknown or ambiguous source fails closed;
5. overload and same-name identities remain distinct;
6. reversed project, adapter, fact, and syntax-tree order is byte-identical;
7. relocated checkouts preserve stable identities;
8. non-idiomatic but semantically equivalent source yields equivalent facts;
9. incompatible assertions remain visible conflicts;
10. diagnostic codes, outcomes, subjects, and locations are stable;
11. generated Screenplay compiles and survives print, compile, and print unchanged;
12. package consumers compile against the candidate package set.

Use public, pinned framework samples for package-level compatibility. Keep focused authored-source fixtures purpose-built and independent of private product source.

Start with one positive `Specification` that creates a `CSharpCompilation`, establishes its authored trees, invokes the adapter, resolves the contribution, and asserts the exact artifact, placement, evidence, and diagnostics. Add a second source tree with the same short names in an unrelated namespace and prove it contributes nothing.

Use these implementations as examples only:

- [Vogen concept adapter](https://github.com/Cratis/Screenplay.Generation/blob/main/Source/DotNET/Generation.DotNet.Vogen/VogenConceptScreenplayAdapter.cs)
- [Arc source adapter](https://github.com/Cratis/Arc)
- [Critter Stack source adapter](https://github.com/Cratis/Screenplay.CritterStack)

## Release and adopt the adapter

Before publishing an adapter or a new Generation minor, run:

```bash
dotnet test Screenplay.Generation.slnx --configuration Debug
dotnet build Screenplay.Generation.slnx --configuration Release -p:Version=9999.0.0
dotnet pack Screenplay.Generation.slnx --no-build --configuration Release \
  -o Artifacts/NuGet -p:Version=9999.0.0
./scripts/verify-package-consumers.sh 9999.0.0 Artifacts/NuGet
```

Use the ecosystem adapter's equivalent sentinel package and consumer gate. Apply the same sentinel version to the build and no-build pack so package and assembly versions agree. Run API package validation and a clean current-source consumer. Upgrade composition hosts and ecosystem adapters together when they need a newly released Generation API.
