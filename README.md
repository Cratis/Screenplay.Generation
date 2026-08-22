# Screenplay.Generation

Framework-neutral source adapter SDK for generating verified [Cratis Screenplay](https://github.com/Cratis/Screenplay) definitions.

## Packages

| Package | Responsibility |
| --- | --- |
| `Cratis.Screenplay.Generation.Contracts` | Typed semantic facts, evidence, provenance, and diagnostics contributed by source adapters |
| `Cratis.Screenplay.Generation` | Deterministic fact resolution, Screenplay lowering, canonical printing, and compiler verification |
| `Cratis.Screenplay.Generation.DotNet` | Reusable Roslyn compilation, symbol, authored-source, and type-shape APIs for .NET adapter authors |
| `Cratis.Screenplay.Generation.DotNet.Vogen` | Authored-source Vogen value-object discovery as neutral concept and primitive-representation facts |

Framework adapters remain owned by their source ecosystems:

- Arc: [`Cratis.Arc.Screenplay`](https://github.com/Cratis/Arc)
- Critter Stack: [`Cratis.CritterStack.Screenplay`](https://github.com/Cratis/Screenplay.CritterStack)

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

## Vogen adapter composition

`Cratis.Screenplay.Generation.DotNet.Vogen` recognizes the exact Roslyn metadata names `Vogen.ValueObjectAttribute`, ``Vogen.ValueObjectAttribute`1``, and `Vogen.VogenDefaultsAttribute`. It has no Vogen package or runtime dependency. Vogen is pinned only in the adapter's semantic spec project so production consumers remain decoupled from the source generator.

Each `DotNetProjectCompilation` requires the workspace host's authoritative `AuthoredSyntaxTrees`. Build this set from project documents before source generators update the compilation. Generated filenames and headers remain useful conventions, but they are not trusted as proof of authored origin.

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

The Vogen contribution establishes only authored concepts and supported primitive representations. It never infers identity or validation, never treats generated members as primary evidence, and reports `VOG0001` instead of inventing `String` for an unsupported backing type.

## Concepts

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

See [`IMPLEMENTATION_STATUS.md`](IMPLEMENTATION_STATUS.md) for the current implementation checkpoint and pre-release decisions.

## Build and test

```shell
dotnet test Screenplay.Generation.slnx --configuration Debug
dotnet build Screenplay.Generation.slnx --configuration Release -p:Version=9999.0.0
dotnet pack Screenplay.Generation.slnx --no-build --configuration Release -o Artifacts/NuGet -p:Version=9999.0.0
./scripts/verify-package-consumers.sh 9999.0.0 Artifacts/NuGet
```

Package validation runs during pack against the first public package for each assembly: `0.1.0` for Contracts, Generation, and DotNet, and `0.5.0` for DotNet.Vogen. Baseline strict mode remains disabled so intentional compatible additions are accepted while removals and signature changes still fail; no compatibility diagnostics are suppressed. The sentinel version must be applied to both the Release build and the no-build pack so package and assembly versions agree. The consumer smoke compiles clean binaries against those public baselines, then runs them unchanged with the current packages to cover record, positional-record analysis, generator, adapter, and Vogen APIs.

All builds require zero errors and zero warnings. Generated Screenplay output must compile and remain stable through print/compile/print.

## License

Screenplay.Generation is licensed under the [MIT license](LICENSE).
