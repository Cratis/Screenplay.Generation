# Screenplay.Generation

Framework-neutral source adapter SDK for generating verified [Cratis Screenplay](https://github.com/Cratis/Screenplay) definitions.

## Packages

| Package | Responsibility |
| --- | --- |
| `Cratis.Screenplay.Generation.Contracts` | Typed semantic facts, evidence, provenance, and diagnostics contributed by source adapters |
| `Cratis.Screenplay.Generation` | Deterministic fact resolution, Screenplay lowering, canonical printing, and compiler verification |
| `Cratis.Screenplay.Generation.DotNet` | Reusable Roslyn compilation, symbol, source, and type-shape APIs for .NET adapter authors |

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

See [`IMPLEMENTATION_STATUS.md`](IMPLEMENTATION_STATUS.md) for the current implementation checkpoint and pre-release decisions.

## Build and test

```shell
dotnet test Screenplay.Generation.slnx --configuration Debug
dotnet build Screenplay.Generation.slnx --configuration Release
dotnet pack Screenplay.Generation.slnx --no-build --configuration Release -o Artifacts/NuGet
```

All builds require zero errors and zero warnings. Generated Screenplay output must compile and remain stable through print/compile/print.

## License

Screenplay.Generation is licensed under the [MIT license](LICENSE).
