#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# Compiles consumers against the first public package baselines, then runs those unchanged
# binaries beside the packages being validated. This catches binary breaks in records,
# source-analysis APIs, generator entry points, and the Vogen adapter that a source rebuild hides.
#
# Usage: verify-package-consumers.sh [current-version] [local-package-feed]
#   current-version      version of the current packages; defaults to the PR sentinel
#   local-package-feed   directory containing all four current packages

set -euo pipefail

CURRENT_VERSION="${1:-9999.0.0}"
REPOSITORY_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
LOCAL_FEED="${2:-$REPOSITORY_ROOT/Artifacts/NuGet}"
LOCAL_FEED="$(cd "$LOCAL_FEED" && pwd)"
WORK_DIR="$(mktemp -d)"
trap 'rm -rf "$WORK_DIR"' EXIT

for package in \
    Cratis.Screenplay.Generation.Contracts \
    Cratis.Screenplay.Generation \
    Cratis.Screenplay.Generation.DotNet \
    Cratis.Screenplay.Generation.DotNet.Vogen
do
    package_path="$LOCAL_FEED/$package.$CURRENT_VERSION.nupkg"
    if [ ! -f "$package_path" ]; then
        echo "Missing current package: $package_path" >&2
        exit 1
    fi
done

cat > "$WORK_DIR/Directory.Build.props" <<'PROPS'
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
PROPS

cat > "$WORK_DIR/nuget.config" <<CONFIG
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="current" value="$LOCAL_FEED" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
CONFIG

CORE_DIR="$WORK_DIR/CoreBaseline"
VOGEN_DIR="$WORK_DIR/VogenBaseline"
RUNNER_DIR="$WORK_DIR/CurrentRunner"
mkdir -p "$CORE_DIR" "$VOGEN_DIR" "$RUNNER_DIR"

cat > "$CORE_DIR/CoreBaseline.csproj" <<'PROJECT'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Cratis.Screenplay.Generation.Contracts" Version="0.1.0" />
    <PackageReference Include="Cratis.Screenplay.Generation" Version="0.1.0" />
    <PackageReference Include="Cratis.Screenplay.Generation.DotNet" Version="0.1.0" />
  </ItemGroup>
</Project>
PROJECT

cat > "$CORE_DIR/BaselineCoreConsumer.cs" <<'CSHARP'
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Generation;
using Cratis.Screenplay.Generation.DotNet;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public static class BaselineCoreConsumer
{
    public static string Exercise()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            namespace Ordering;
            public record OrderPlaced(System.Guid OrderId, string Description);
            """, path: "Ordering/OrderPlaced.cs");
        var compilation = CSharpCompilation.Create(
            "Ordering",
            [tree],
            TrustedPlatformReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var project = new DotNetProjectCompilation
        {
            Name = "Ordering",
            Compilation = compilation,
            SourceRoot = "/consumer"
        };
        var context = new DotNetAnalysisContext([project]);
        var type = new DotNetArtifactCatalog(compilation).Types.Single(_ => _.Name == "OrderPlaced");
        var properties = DotNetTypeShapes.PropertiesOf(type);
        if (properties.Select(_ => _.Name).ToArray() is not ["orderId", "description"])
        {
            throw new InvalidOperationException("The positional record shape was not preserved.");
        }

        var subject = project.SubjectForType(type);
        var adapter = new AdapterIdentity { Id = "binary-smoke", Version = "0.1.0" };
        var evidence = new Evidence
        {
            Adapter = adapter,
            Strength = EvidenceStrength.Exact,
            Source = new SourceRange
            {
                Path = "Ordering/OrderPlaced.cs",
                StartLine = 2,
                StartColumn = 1,
                EndLine = 2,
                EndColumn = 70
            }
        };
        var copiedEvidence = evidence with { Explanation = "Compiled against the 0.1.0 public record API" };
        var key = new ArtifactKey { Subject = subject, Kind = ArtifactKind.Event };
        var contribution = new AdapterContribution
        {
            Adapter = adapter,
            Facts =
            [
                new ArtifactFact
                {
                    Id = new FactId { Value = "event:order-placed" },
                    Subject = subject,
                    Evidence = copiedEvidence,
                    Definition = new ArtifactDefinition
                    {
                        Key = key,
                        Name = type.Name,
                        File = "Ordering/OrderPlaced.cs",
                        Properties = properties
                    }
                },
                new ArtifactPlacementFact
                {
                    Id = new FactId { Value = "placement:order-placed" },
                    Subject = subject,
                    Evidence = copiedEvidence,
                    Artifact = key,
                    Placement = new ArtifactPlacement
                    {
                        Module = "Ordering",
                        Features = ["Orders"],
                        Slice = "Place",
                        SliceKind = GenerationSliceKind.StateChange
                    }
                }
            ]
        };
        IDotNetScreenplayAdapter sourceAdapter = new BaselineAdapter(contribution);
        if (!sourceAdapter.CanAnalyze(context))
        {
            throw new InvalidOperationException("The baseline adapter did not recognize its compilation.");
        }

        var generated = new ScreenplayDefinitionGenerator().Generate(
            [sourceAdapter.Analyze(context, new DotNetAdapterOptions())],
            new ScreenplayGenerationOptions { Domain = "Sales" });
        if (!generated.IsSuccess || !generated.Source.Contains("event OrderPlaced", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The baseline generator API did not produce the positional record event.");
        }

        return generated.Source;
    }

    static IReadOnlyList<MetadataReference> TrustedPlatformReferences() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToArray();

    sealed class BaselineAdapter(AdapterContribution contribution) : IDotNetScreenplayAdapter
    {
        public AdapterIdentity Identity => contribution.Adapter;

        public bool CanAnalyze(DotNetAnalysisContext context) => context.Projects.Count == 1;

        public AdapterContribution Analyze(DotNetAnalysisContext context, DotNetAdapterOptions options)
        {
            _ = context;
            _ = options;
            return contribution;
        }
    }
}
CSHARP

cat > "$VOGEN_DIR/VogenBaseline.csproj" <<'PROJECT'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Cratis.Screenplay.Generation" Version="0.5.0" />
    <PackageReference Include="Cratis.Screenplay.Generation.DotNet.Vogen" Version="0.5.0" />
  </ItemGroup>
</Project>
PROJECT

cat > "$VOGEN_DIR/BaselineVogenConsumer.cs" <<'CSHARP'
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Generation;
using Cratis.Screenplay.Generation.DotNet;
using Cratis.Screenplay.Generation.DotNet.Vogen;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public static class BaselineVogenConsumer
{
    public static string Exercise()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            namespace Vogen
            {
                [System.AttributeUsage(System.AttributeTargets.Struct)]
                public sealed class ValueObjectAttribute<T> : System.Attribute;
            }

            namespace Ordering
            {
                [Vogen.ValueObject<System.Guid>]
                public readonly partial record struct OrderId;
            }
            """, path: "Ordering/OrderId.cs");
        var compilation = CSharpCompilation.Create(
            "Ordering",
            [tree],
            TrustedPlatformReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var project = new DotNetProjectCompilation
        {
            Name = "Ordering",
            Compilation = compilation,
            SourceRoot = "/consumer",
            AuthoredSyntaxTrees = new HashSet<SyntaxTree> { tree }
        };
        var context = new DotNetAnalysisContext([project]);
        IDotNetScreenplayAdapter adapter = new VogenConceptScreenplayAdapter();
        if (!adapter.CanAnalyze(context))
        {
            throw new InvalidOperationException("The 0.5.0 Vogen API did not recognize the authored value object.");
        }

        var contribution = adapter.Analyze(context, new DotNetAdapterOptions());
        if (!contribution.Facts.OfType<ConceptRepresentationFact>().Any(
                _ => _.Definition.Primitive == GenerationPrimitiveKind.Uuid))
        {
            throw new InvalidOperationException("The 0.5.0 Vogen API did not contribute a UUID representation.");
        }

        var generated = new ScreenplayDefinitionGenerator().Generate(
            [contribution],
            new ScreenplayGenerationOptions { Domain = "Sales" });
        if (!generated.IsSuccess || !generated.Source.Contains("concept OrderId", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The Vogen contribution did not pass through the generator API.");
        }

        return generated.Source;
    }

    static IReadOnlyList<MetadataReference> TrustedPlatformReferences() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToArray();
}
CSHARP

export NUGET_PACKAGES="$WORK_DIR/.nuget/packages"
echo "Compiling the core consumer against public package baseline 0.1.0..."
dotnet restore "$CORE_DIR/CoreBaseline.csproj" --configfile "$WORK_DIR/nuget.config" --nologo
dotnet build "$CORE_DIR/CoreBaseline.csproj" --no-restore --configuration Release --nologo

echo "Compiling the Vogen consumer against its first correctly sourced public package baseline 0.5.0..."
dotnet restore "$VOGEN_DIR/VogenBaseline.csproj" --configfile "$WORK_DIR/nuget.config" --nologo
dotnet build "$VOGEN_DIR/VogenBaseline.csproj" --no-restore --configuration Release --nologo

cat > "$RUNNER_DIR/CurrentRunner.csproj" <<PROJECT
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Cratis.Screenplay.Generation.Contracts" Version="$CURRENT_VERSION" />
    <PackageReference Include="Cratis.Screenplay.Generation" Version="$CURRENT_VERSION" />
    <PackageReference Include="Cratis.Screenplay.Generation.DotNet" Version="$CURRENT_VERSION" />
    <PackageReference Include="Cratis.Screenplay.Generation.DotNet.Vogen" Version="$CURRENT_VERSION" />
    <Reference Include="CoreBaseline">
      <HintPath>$CORE_DIR/bin/Release/net10.0/CoreBaseline.dll</HintPath>
    </Reference>
    <Reference Include="VogenBaseline">
      <HintPath>$VOGEN_DIR/bin/Release/net10.0/VogenBaseline.dll</HintPath>
    </Reference>
  </ItemGroup>
</Project>
PROJECT

cat > "$RUNNER_DIR/Program.cs" <<'CSHARP'
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Generation;

var coreSource = BaselineCoreConsumer.Exercise();
var vogenSource = BaselineVogenConsumer.Exercise();
var currentReference = new TypeReferenceDefinition
{
    Name = "OrderId",
    Subject = new SubjectId { Value = "dotnet://Ordering/Ordering.OrderId" }
};
if (currentReference.Subject is null || coreSource.Length == 0 || vogenSource.Length == 0)
{
    return 1;
}

Console.WriteLine("Baseline 0.1.0 and 0.5.0 consumer binaries ran against the current packages.");
return 0;
CSHARP

echo "Restoring a clean runner against current package version $CURRENT_VERSION..."
dotnet restore "$RUNNER_DIR/CurrentRunner.csproj" --configfile "$WORK_DIR/nuget.config" --nologo
dotnet run --project "$RUNNER_DIR/CurrentRunner.csproj" --no-restore --configuration Release
