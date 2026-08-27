#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# Compiles consumers against the compatibility ancestry, then runs those unchanged binaries
# beside the packages being validated. A separate source consumer compiles only against the
# current candidate packages and exercises the public 0.15+ composition and adapter-run surface. Together these
# catch binary breaks that a source rebuild hides and ensure the current package set is usable.
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
    Cratis.Screenplay.Generation.DotNet.Vogen; do
    package_path="$LOCAL_FEED/$package.$CURRENT_VERSION.nupkg"
    if [ ! -f "$package_path" ]; then
        echo "Missing current package: $package_path" >&2
        exit 1
    fi
done

cat >"$WORK_DIR/Directory.Build.props" <<'PROPS'
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
PROPS

cat >"$WORK_DIR/nuget.config" <<CONFIG
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
CURRENT_SOURCE_DIR="$WORK_DIR/CurrentSourceConsumer"
RUNNER_DIR="$WORK_DIR/CurrentRunner"
mkdir -p "$CORE_DIR" "$VOGEN_DIR" "$CURRENT_SOURCE_DIR" "$RUNNER_DIR"

cat >"$CORE_DIR/CoreBaseline.csproj" <<'PROJECT'
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

cat >"$CORE_DIR/BaselineCoreConsumer.cs" <<'CSHARP'
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

cat >"$VOGEN_DIR/VogenBaseline.csproj" <<'PROJECT'
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

cat >"$VOGEN_DIR/BaselineVogenConsumer.cs" <<'CSHARP'
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

cat >"$CURRENT_SOURCE_DIR/CurrentSourceConsumer.csproj" <<PROJECT
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <MSBuildTreatWarningsAsErrors>true</MSBuildTreatWarningsAsErrors>
    <CodeAnalysisTreatWarningsAsErrors>true</CodeAnalysisTreatWarningsAsErrors>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Cratis.Screenplay.Generation.Contracts" Version="$CURRENT_VERSION" />
    <PackageReference Include="Cratis.Screenplay.Generation" Version="$CURRENT_VERSION" />
    <PackageReference Include="Cratis.Screenplay.Generation.DotNet" Version="$CURRENT_VERSION" />
    <PackageReference Include="Cratis.Screenplay.Generation.DotNet.Vogen" Version="$CURRENT_VERSION" />
  </ItemGroup>
</Project>
PROJECT

cat >"$CURRENT_SOURCE_DIR/Program.cs" <<'CSHARP'
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Generation;
using Cratis.Screenplay.Generation.DotNet;
using Cratis.Screenplay.Generation.DotNet.Vogen;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

internal static class Program
{
    const string ExpectedGeneratedSourceHash = "747F3674C73545CD043D8B40690605C0D0BF2A67D01173E58EBC162B87C10FDD";
    const string ExpectedVogenDiagnosticsHash = "4F53CDA18C2BAA0C0354BB5F9A3ECBE5ED12AB4D8E11BA873C2F11161202B945";
    const string ExpectedVogenFactsHash = "90C12E55CE826906076A14873D82BF28FE76252AD6C695FA8729B81C94D99D07";
    const string ExternalAdapterId = "external-smoke";
    const string VogenAdapterId = "vogen";

    static readonly JsonSerializerOptions _serializerOptions = new();

    static int Main()
    {
        try
        {
            ExerciseCurrentPackages();
            Console.WriteLine("Current-source consumer compiled, composed, resolved, and compiler-verified deterministically.");
            return 0;
        }
        catch (CurrentSourceConsumerFailure exception)
        {
            Console.Error.WriteLine($"Current-source consumer failed [{exception.Code}]: {exception.Message}");
            return 1;
        }
    }

    static void ExerciseCurrentPackages()
    {
        Require(
            VogenGenerationDiagnosticCodes.UnsupportedBackingType == "VOG0001" &&
            VogenGenerationDiagnosticCodes.InputNormalizationNotRepresented == "VOG0002" &&
            VogenGenerationDiagnosticCodes.NamedInstanceNotRepresented == "VOG0003",
            "CSC0001",
            "The stable Vogen diagnostic-code API changed.");
        Require(
            (int)ArtifactKind.Unknown == -1 &&
            (int)RelationshipKind.Unknown == -1 &&
            (int)GenerationSliceKind.Unknown == -1 &&
            (int)ConceptRepresentationKind.Unknown == -1 &&
            (int)GenerationPrimitiveKind.Unknown == -1 &&
            (int)ConceptAttributeKind.Unknown == -1 &&
            (int)ConceptValidationRuleKind.Unknown == -1 &&
            (int)EvidenceStrength.Unknown == -1 &&
            (int)DotNetProjectRole.Unknown == -1 &&
            (int)DotNetProjectRole.Application == 0 &&
            (int)DotNetProjectRole.Specifications == 1 &&
            (int)GenerationSliceKind.StateChange == 0 &&
            (int)GenerationSliceKind.StateView == 1 &&
            (int)GenerationSliceKind.Automation == 2 &&
            (int)GenerationSliceKind.Translate == 3 &&
            (int)ArtifactKind.Command == 3 &&
            (int)ArtifactKind.Query == 8 &&
            (int)ArtifactKind.Reducer == 10,
            "CSC0019",
            "The additive public Unknown discriminator values are unavailable or were renumbered.");
        Require(
            new ConceptAttributeDefinition
            {
                Concept = new SubjectId { Value = "dotnet://Compatibility/Concept" },
                Name = "compatibility"
            }.Kind == ConceptAttributeKind.Named,
            "CSC0020",
            "The additive concept attribute discriminator did not preserve the legacy named default.");
        var failure = new DotNetValueFailure(DotNetValueFailureKind.Computed, Location.None, "Computed");
        var mutableFailures = new List<DotNetValueFailure> { failure };
        var unknown = new DotNetUnknown<int>(Failures: mutableFailures);
        mutableFailures.Clear();
        Require(
            unknown.Failures.SequenceEqual([failure]),
            "CSC0037",
            "The named Failures constructor argument did not retain an immutable failure snapshot.");
        ExercisePublicAdapterContracts();
        ExerciseCandidatePackageClosure();

        var authoredTree = CSharpSyntaxTree.ParseText(
            """
            namespace Vogen
            {
                [System.AttributeUsage(System.AttributeTargets.Struct)]
                public sealed class ValueObjectAttribute<T> : System.Attribute
                {
                }

                public sealed class Validation
                {
                    public static Validation Ok { get; } = new();

                    public static Validation Invalid(string message)
                    {
                        _ = message;
                        return new();
                    }
                }
            }

            namespace Ordering
            {
                [Vogen.ValueObject<string>]
                public readonly partial record struct CustomerCode
                {
                    private const string InvalidMessage = "Customer codes cannot be blank";

                    private static Vogen.Validation Validate(string value) =>
                        string.IsNullOrWhiteSpace(value)
                            ? Vogen.Validation.Invalid(InvalidMessage)
                            : Vogen.Validation.Ok;
                }

                [System.AttributeUsage(System.AttributeTargets.Class)]
                public sealed class EndpointPolicyAttribute : System.Attribute
                {
                    public bool Required { get; set; }
                }

                [EndpointPolicy(Required = true)]
                public sealed record CustomerRegistered(CustomerCode CustomerCode);
                public sealed record Delivery(string Name, string[] Tags);
                public sealed class CustomerBatch : System.Collections.Generic.List<CustomerRegistered>;
                public sealed class CustomerOptions;
                public static class CustomerOptionsExtensions
                {
                    public static void Configure(this CustomerOptions options, string name = "default") => _ = (options, name);
                }
                public static class CustomerHandler
                {
                    public static void Validate(CustomerRegistered request) => _ = request;
                    public static void Load(CustomerRegistered request, int version) => _ = (request, version);
                    public static void Validate(CustomerCode request) => _ = request;
                    public static void Configure(CustomerOptions options) => options.Configure(name: "consumer");
                    public static Delivery BuildDelivery() => new("Screenplay", new[] { "source", "exact" });
                    public static System.Type DeliveryType() => typeof(Delivery);
                }
            }
            """,
            path: "/consumer/Concepts/CustomerCode.cs");
        var generatedLookalikeTree = CSharpSyntaxTree.ParseText(
            """
            namespace Ordering;

            [Vogen.ValueObject<int>]
            public readonly partial record struct GeneratedOnlyCode
            {
                static string Value { get; set; } = string.Empty;
                static string Transform(string value) => value;
                static void Generated() => Value = Transform("generated");
            }
            """,
            path: "/consumer/obj/GeneratedOnlyCode.g.cs");
        var compilation = CSharpCompilation.Create(
            "Ordering",
            [authoredTree, generatedLookalikeTree],
            TrustedPlatformReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var compilationErrors = compilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => diagnostic.ToString())
            .ToArray();
        Require(
            compilationErrors.Length == 0,
            "CSC0002",
            $"The fake authored Vogen compilation was invalid: {string.Join(" | ", compilationErrors)}");

        var legacySymbol = compilation.GetTypeByMetadataName("Ordering.CustomerCode")!;
        var legacyAttribute = legacySymbol.GetAttributes().Single(attribute =>
            attribute.AttributeClass?.MetadataName == "ValueObjectAttribute`1");
        var legacyAuthoredTrees = new HashSet<SyntaxTree> { authoredTree };
        var legacyAdapter = new AdapterIdentity { Id = "legacy-source", Version = "0.8.0" };
        var legacyRange = DotNetSource.Range(authoredTree.GetRoot().GetLocation(), null);
        var legacySymbolEvidence = DotNetSource.EvidenceFor(legacySymbol, legacyAdapter, EvidenceStrength.Exact, null);
        _ = DotNetSource.EvidenceFor(legacySymbol, legacyAdapter, EvidenceStrength.Exact, null, "Legacy symbol evidence");
        var legacyAttributeEvidence = DotNetSource.EvidenceFor(legacyAttribute, legacyAdapter, EvidenceStrength.Exact, null);
        _ = DotNetSource.EvidenceFor(legacyAttribute, legacyAdapter, EvidenceStrength.Exact, null, "Legacy attribute evidence");
        _ = DotNetSource.EvidenceFor(legacyAttribute, legacyAuthoredTrees, legacyAdapter, EvidenceStrength.Exact, null);
        _ = DotNetSource.EvidenceFor(legacyAttribute, legacyAuthoredTrees, legacyAdapter, EvidenceStrength.Exact, null, "Legacy authored attribute evidence");
        Require(
            legacyRange?.FileIdentity is null &&
            legacySymbolEvidence.Source?.FileIdentity is null &&
            legacyAttributeEvidence.Source?.FileIdentity is null,
            "CSC0023",
            "Legacy positional-null source calls did not retain their no-identity behavior.");

        var sourceContext = DotNetSourcePaths.Create(
            "Ordering/Ordering",
            new DotNetSourcePathPolicy
            {
                DisplayRoot = DotNetSourceDisplayRoot.Workspace,
                CasePolicy = DotNetSourcePathCasePolicy.InvariantLowercase
            },
            [
                new DotNetSourceDocument
                {
                    SyntaxTree = authoredTree,
                    ProjectRelativePath = "Concepts/CustomerCode.cs",
                    WorkspaceRelativePath = "Concepts/CustomerCode.cs"
                }
            ]);
        var project = new DotNetProjectCompilation
        {
            Name = "Ordering.Project",
            Compilation = compilation,
            SourceRoot = "/consumer",
            SourceContext = sourceContext,
            AuthoredSyntaxTrees = new HashSet<SyntaxTree> { authoredTree }
        };
        Require(
            project.Role == DotNetProjectRole.Application,
            "CSC0024",
            "The additive project role did not preserve the application default.");
        var placementOptions = new DotNetAdapterOptions
        {
            FeatureRoot = "Source",
            NamespaceSegmentsToSkip = 1
        };
        var sourceStructure = DotNetSourceStructureResolver.Resolve(
            new DotNetSourceStructure
            {
                Subject = new SubjectId { Value = "dotnet://Ordering/Ordering/Customers.Register.Register" },
                Project = "Ordering/Ordering",
                ProjectRole = DotNetProjectRole.Specifications,
                Namespace = "Ordering.Customers.Register",
                ProjectRelativePaths = ["Source/Customers/Register/Register.cs"]
            },
            GenerationSliceKind.StateChange,
            placementOptions.SourceStructurePolicy);
        Require(
            sourceStructure.IsSuccess &&
            sourceStructure.Structure.ProjectRole == DotNetProjectRole.Specifications &&
            sourceStructure.Placement?.Module == "Customers" &&
            sourceStructure.Placement?.Slice == "Register",
            "CSC0025",
            "The shared source-structure policy did not preserve project role and exact placement.");
        var placementSnapshot = DotNetSourcePlacementDerivation.Derive(
        [
            new DotNetSourcePlacementRequest
            {
                Artifact = new ArtifactKey
                {
                    Subject = sourceStructure.Structure.Subject,
                    Kind = ArtifactKind.Command
                },
                Structure = sourceStructure.Structure,
                SliceKind = GenerationSliceKind.StateChange,
                Policy = placementOptions.SourceStructurePolicy
            }
        ]);
        Require(
            placementSnapshot.IsSuccess &&
            placementSnapshot.Placements.Single().Placement.Module == sourceStructure.Placement?.Module &&
            placementSnapshot.Placements.Single().Placement.Slice == sourceStructure.Placement?.Slice,
            "CSC0027",
            "The fixed-snapshot source placement derivation did not retain exact placement.");

        var context = new DotNetAnalysisContext([project]);
        var sourceStructureSnapshot = DotNetSourceStructures.Create(context);
        Require(
            sourceStructureSnapshot.IsSuccess &&
            sourceStructureSnapshot.Structures.Any(structure =>
                structure.Namespace == "Ordering" &&
                structure.ProjectRelativePaths.Contains("Concepts/CustomerCode.cs", StringComparer.Ordinal)),
            "CSC0026",
            "The fixed .NET source-structure snapshot did not retain mapped authored source.");
        var mappedStructure = sourceStructureSnapshot.Structures.Single(structure =>
            structure.Subject == project.SubjectForType(legacySymbol));
        var mappedPlacement = DotNetSourcePlacementDerivation.Derive(
        [
            new DotNetSourcePlacementRequest
            {
                Artifact = new ArtifactKey { Subject = mappedStructure.Subject, Kind = ArtifactKind.Command },
                Structure = mappedStructure,
                SliceKind = GenerationSliceKind.StateChange,
                Policy = new DotNetSourceStructurePolicy { Module = "Ordering" }
            }
        ]);
        Require(
            mappedPlacement.IsSuccess &&
            mappedPlacement.Placements.Single().Structure.Project == sourceContext.ProjectIdentity &&
            mappedPlacement.Placements.Single().Structure.Source?.FileIdentity?.Project == sourceContext.ProjectIdentity &&
            !mappedPlacement.Placements.Single().UsedCompatibilityPlacement &&
            mappedPlacement.Placements.Single().CompatibilityReasonCode is null,
            "CSC0031",
            "Strict source placement did not retain the fixed host-owned snapshot, identity, or strict-default provenance.");

        var syntheticReducerSubject = new SubjectId { Value = $"{mappedStructure.Subject.Value}#reducer" };
        var ownedPlacement = DotNetSourcePlacementDerivation.Derive(
        [
            new DotNetSourcePlacementRequest
            {
                Artifact = new ArtifactKey { Subject = syntheticReducerSubject, Kind = ArtifactKind.Reducer },
                Structure = mappedStructure,
                SourceOwner = mappedStructure.Subject,
                SliceKind = GenerationSliceKind.StateView,
                Policy = new DotNetSourceStructurePolicy { Module = "Ordering" }
            }
        ]);
        Require(
            ownedPlacement.IsSuccess &&
            ownedPlacement.Placements.Single().Artifact.Subject == syntheticReducerSubject &&
            ownedPlacement.Placements.Single().Structure.Subject == mappedStructure.Subject &&
            ownedPlacement.Placements.Single().SourceOwner == mappedStructure.Subject,
            "CSC0033",
            "An exact synthetic artifact source owner did not retain the unchanged fixed source structure.");

        var flatSubject = new SubjectId { Value = "dotnet://Ordering/Ordering/Ordering.PlaceOrder" };
        var compatibilityPolicy = new DotNetSourcePlacementCompatibilityPolicy
        {
            Placement = new ArtifactPlacement
            {
                Module = "Commerce",
                Features = ["Orders"],
                Slice = "Place",
                SliceKind = GenerationSliceKind.StateChange
            }
        };
        var flatPlacement = DotNetSourcePlacementDerivation.Derive(
        [
            new DotNetSourcePlacementRequest
            {
                Artifact = new ArtifactKey { Subject = flatSubject, Kind = ArtifactKind.Command },
                Structure = new DotNetSourceStructure
                {
                    Subject = flatSubject,
                    Project = sourceContext.ProjectIdentity,
                    ProjectRole = DotNetProjectRole.Application,
                    Namespace = "Ordering",
                    ProjectRelativePaths = ["PlaceOrder.cs"]
                },
                SliceKind = GenerationSliceKind.StateChange,
                Policy = new DotNetSourceStructurePolicy { NamespaceSegmentsToSkip = 1, Module = "Configured" },
                CompatibilityPolicy = compatibilityPolicy
            }
        ]);
        var flatResult = flatPlacement.Placements.Single();
        Require(
            flatPlacement.IsSuccess &&
            flatResult.UsedCompatibilityPlacement &&
            flatResult.CompatibilityReasonCode == DotNetSourceStructureDiagnosticCodes.InsufficientStructure &&
            flatResult.Policy.Module == "Configured" &&
            flatResult.Policy.NamespaceSegmentsToSkip == 1 &&
            flatResult.CompatibilityPolicy?.Version == compatibilityPolicy.Version &&
            flatResult.CompatibilityPolicy?.Placement.Module == compatibilityPolicy.Placement.Module &&
            flatResult.CompatibilityPolicy?.Placement.Features.SequenceEqual(compatibilityPolicy.Placement.Features, StringComparer.Ordinal) == true &&
            flatResult.CompatibilityPolicy?.Placement.Slice == compatibilityPolicy.Placement.Slice &&
            flatResult.Placement.Module == "Commerce" &&
            flatResult.Placement.Slice == "Place",
            "CSC0034",
            "Explicit flat-source compatibility placement did not retain deterministic policy and trigger provenance.");

        var semanticModel = compilation.GetSemanticModel(authoredTree);
        var invocation = authoredTree.GetRoot().DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Single(_ => _.ToString().Contains("consumer", StringComparison.Ordinal));
        var authoredInvocations = DotNetSource.AuthoredInvocationsIn(project);
        var authoredAssignments = DotNetSource.AuthoredAssignmentsIn(project);
        var configureScope = (MethodDeclarationSyntax)compilation.GetTypeByMetadataName("Ordering.CustomerHandler")!.GetMembers("Configure").OfType<IMethodSymbol>().Single().DeclaringSyntaxReferences.Single().GetSyntax();
        var scopedInvocations = DotNetSource.AuthoredInvocationsIn(configureScope, project);
        var assignmentScope = (MethodDeclarationSyntax)compilation.GetTypeByMetadataName("Ordering.CustomerOptionsExtensions")!.GetMembers("Configure").OfType<IMethodSymbol>().Single().DeclaringSyntaxReferences.Single().GetSyntax();
        var scopedAssignments = DotNetSource.AuthoredAssignmentsIn(assignmentScope, project);
        Require(
            authoredInvocations.Contains(invocation) &&
            authoredInvocations.All(candidate => candidate.SyntaxTree == authoredTree) &&
            authoredAssignments.Count > 0 &&
            authoredAssignments.All(candidate => candidate.SyntaxTree == authoredTree) &&
            scopedInvocations.Count == 1 &&
            scopedInvocations[0].SyntaxTree == invocation.SyntaxTree &&
            scopedInvocations[0].Span == invocation.Span &&
            scopedAssignments.Count == 1 &&
            authoredAssignments.Any(candidate => candidate.SyntaxTree == scopedAssignments[0].SyntaxTree && candidate.Span == scopedAssignments[0].Span),
            "CSC0035",
            "Authoritative invocation and assignment enumeration admitted generated source or lost exact scoped source.");

        var validationScope = (MethodDeclarationSyntax)legacySymbol.GetMembers("Validate").OfType<IMethodSymbol>().Single().DeclaringSyntaxReferences.Single().GetSyntax();
        var invalidInvocation = DotNetSource.AuthoredInvocationsIn(validationScope, project)
            .Single(candidate => candidate.ToString().Contains("Vogen.Validation.Invalid", StringComparison.Ordinal));
        var invalidMethod = DotNetInvocations.MethodFor(invalidInvocation, semanticModel);
        var invalidDefinition = compilation.GetTypeByMetadataName("Vogen.Validation")!.GetMembers("Invalid").OfType<IMethodSymbol>().Single();
        var invalidSignature = DotNetMethodSignatures.From(invalidDefinition);
        var invalidOperation = semanticModel.GetOperation(invalidInvocation);
        var invalidParameter = invalidMethod is null
            ? null
            : DotNetInvocations.DefinitionOf(invalidMethod).Parameters.SingleOrDefault(parameter => parameter.Ordinal == 0);
        var invalidArgument = invalidMethod is null || invalidParameter is null
            ? null
            : DotNetInvocations.ArgumentForParameter(invalidInvocation, invalidMethod, invalidParameter.Name, semanticModel);
        var invalidMessage = invalidArgument is null
            ? null
            : DotNetSourceValues.Constant<string>(invalidArgument.Expression, semanticModel);
        Require(
            invalidMethod is not null &&
            invalidOperation is IInvocationOperation &&
            DotNetMethodSignatures.Matches(invalidMethod, invalidSignature) &&
            invalidArgument?.Expression.ToString() == "InvalidMessage" &&
            invalidMessage is DotNetKnown<string> { Value: "Customer codes cannot be blank" },
            "CSC0039",
            "The current packages did not compose authored invocation, exact signature, formal argument, and bounded constant helpers on the Vogen Invalid call.");

        var deliveryTypeExpression = authoredTree.GetRoot().DescendantNodes().OfType<TypeOfExpressionSyntax>().Single();
        var deliveryType = DotNetSourceValues.TypeOf(deliveryTypeExpression, semanticModel);
        var untypedDeliveryType = DotNetSourceValues.Extract(deliveryTypeExpression, semanticModel);
        Require(
            deliveryType is DotNetKnown<ITypeSymbol> { Value.Name: "Delivery" } &&
            untypedDeliveryType is DotNetKnown<DotNetSourceValue> { Value: DotNetTypeValue { Type.Name: "Delivery" } },
            "CSC0040",
            "Bounded typeof extraction did not preserve the exact source type through typed and untyped public helpers.");

        var deliveryCreation = authoredTree.GetRoot().DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>().Single(creation => semanticModel.GetTypeInfo(creation).Type?.Name == "Delivery");
        var deliveryPayload = (DotNetKnown<DotNetPayloadValue>)DotNetSourceValues.Payload(deliveryCreation, semanticModel);
        var deliveryTagsExpression = deliveryCreation.ArgumentList!.Arguments[1].Expression;
        var deliveryTags = (DotNetKnown<DotNetCollectionValue>)DotNetSourceValues.Collection(deliveryTagsExpression, semanticModel);
        Require(
            deliveryPayload.Value.Values.Select(value => value.Name).SequenceEqual(["Name", "Tags"], StringComparer.Ordinal) &&
            deliveryPayload.Value.Values[1].Value is DotNetCollectionValue nestedTags &&
            nestedTags.Values.Select(element => ((DotNetConstantValue)element.Value).Value).SequenceEqual(["source", "exact"]) &&
            deliveryTags.Value.Values.All(element => element.Source.IsInSource),
            "CSC0036",
            "Bounded payload and collection extraction did not preserve formal order, nested values, or source locations.");

        var invocationMethod = DotNetInvocations.MethodFor(invocation, semanticModel)!;
        var invocationName = DotNetInvocations.ArgumentForParameter(invocation, invocationMethod, "name", semanticModel)!;
        var invocationRoot = DotNetInvocations.ReceiverRootParameter(invocation, invocationMethod, semanticModel)!;
        Require(
            DotNetInvocations.DefinitionOf(invocationMethod).Name == "Configure" &&
            invocationName.Expression.ToString() == "\"consumer\"" &&
            invocationRoot.Name == "options",
            "CSC0032",
            "The shared .NET invocation helpers did not preserve exact method, formal-argument, or receiver-root semantics.");

        var customerRegistered = compilation.GetTypeByMetadataName("Ordering.CustomerRegistered")!;
        ExerciseAdapterRunnerContracts(context, project, customerRegistered);
        ExerciseVogenModernAndLegacyParity();
        var batchElement = DotNetSymbols.ElementTypeOf(compilation.GetTypeByMetadataName("Ordering.CustomerBatch")!);
        var endpointPolicy = customerRegistered.GetAttributes().Single();
        var handlerType = compilation.GetTypeByMetadataName("Ordering.CustomerHandler")!;
        var companions = DotNetSymbols.CompanionMethodsFor(
            handlerType,
            customerRegistered,
            ["Validate", "Load"]);
        var validationOverloads = handlerType.GetMembers("Validate").OfType<IMethodSymbol>().ToArray();
        Require(
            SymbolEqualityComparer.Default.Equals(batchElement, customerRegistered) &&
            DotNetSymbols.NamedArgument<bool>(endpointPolicy, "Required") == true &&
            companions.Select(method => method.Name).SequenceEqual(["Load", "Validate"], StringComparer.Ordinal) &&
            project.SubjectForMethod(validationOverloads[0]) != project.SubjectForMethod(validationOverloads[1]) &&
            DotNetSubjectIds.MethodDisplayName(validationOverloads[0]).Contains("Validate", StringComparison.Ordinal),
            "CSC0028",
            "The shared .NET symbol helpers did not preserve collection, attribute, companion-method, or overload identity semantics.");

        var configuredEvidence = DotNetSource.EvidenceFor(
            legacySymbol,
            new AdapterIdentity { Id = "registered-values", Version = "1.0.0" },
            project,
            EvidenceStrength.Configured,
            "An authored registration declares the source type as a domain value");
        var nominatedConcept = DotNetConceptFacts.Emit(
            legacySymbol,
            compilation.GetSpecialType(SpecialType.System_String),
            project.SubjectForType(legacySymbol),
            configuredEvidence);
        Require(
            nominatedConcept.OfType<ArtifactFact>().Single().Definition.Key.Kind == ArtifactKind.Concept &&
            nominatedConcept.OfType<ConceptRepresentationFact>().Single().Definition.Primitive == GenerationPrimitiveKind.Text &&
            nominatedConcept.All(fact => fact.Evidence.Strength == EvidenceStrength.Configured),
            "CSC0029",
            "Declared concept nomination did not emit neutral concept and primitive facts with its own evidence.");

        IDotNetScreenplayAdapter[] adapters =
        [
            new VogenConceptScreenplayAdapter(),
            new ExternalCustomerAdapter()
        ];
        var contributions = adapters
            .Where(adapter => adapter.CanAnalyze(context))
            .Select(adapter => adapter.Analyze(context, new DotNetAdapterOptions()))
            .ToArray();

        Require(contributions.Length == 2, "CSC0003", "Both the Vogen and external adapters must contribute.");
        Require(
            contributions.Select(contribution => contribution.Adapter.Id).ToHashSet(StringComparer.Ordinal)
                .SetEquals([VogenAdapterId, ExternalAdapterId]),
            "CSC0004",
            "The composed contributions did not preserve both adapter identities.");

        var vogenContribution = contributions.Single(contribution => contribution.Adapter.Id == VogenAdapterId);
        Require(
            vogenContribution.Facts.OfType<ArtifactFact>().All(fact => fact.Definition.Name != "GeneratedOnlyCode"),
            "CSC0005",
            "A syntax tree outside authoritative AuthoredSyntaxTrees originated a Vogen concept.");
        var concept = vogenContribution.Facts.OfType<ArtifactFact>()
            .Single(fact => fact.Definition.Name == "CustomerCode");
        Require(
            concept.Definition.File == "Concepts/CustomerCode.cs" &&
            concept.Evidence.Source?.FileIdentity == new SourceFileIdentity
            {
                Project = "Ordering/Ordering",
                Path = "concepts/customercode.cs"
            },
            "CSC0022",
            "The explicit source context did not keep display path and stable file identity separate.");
        var representation = vogenContribution.Facts.OfType<ConceptRepresentationFact>()
            .Single(fact => fact.Subject == concept.Subject);
        Require(
            representation.Definition.Kind == ConceptRepresentationKind.Primitive &&
            representation.Definition.Primitive == GenerationPrimitiveKind.Text,
            "CSC0006",
            "Vogen did not contribute the neutral text concept representation.");
        var validation = vogenContribution.Facts.OfType<ConceptValidationRuleFact>()
            .Single(fact => fact.Subject == concept.Subject);
        Require(
            validation.Definition.RuleIdentity == "vogen.validate" &&
            validation.Definition.Kind == ConceptValidationRuleKind.NamedPredicate &&
            validation.Definition.Predicate == "Validate" &&
            validation.Definition.Message == "Customer codes cannot be blank" &&
            validation.Definition.ImplementationFile == "Concepts/CustomerCode.cs",
            "CSC0007",
            "The exact authored Vogen validation hook was not preserved as a named validation fact.");
        Require(vogenContribution.Diagnostics.Count == 0, "CSC0008", "Representable Vogen source reported semantic loss.");
        var vogenFactsHash = Sha256(JsonSerializer.SerializeToUtf8Bytes(vogenContribution.Facts.Cast<object>().ToArray(), _serializerOptions));
        var vogenDiagnosticsHash = Sha256(JsonSerializer.SerializeToUtf8Bytes(vogenContribution.Diagnostics, _serializerOptions));

        var externalContribution = contributions.Single(contribution => contribution.Adapter.Id == ExternalAdapterId);
        var eventFact = externalContribution.Facts.OfType<ArtifactFact>().Single();
        var eventType = eventFact.Definition.Properties.Single().Type;
        Require(
            eventType.Name == "UnresolvedCustomerCode" && eventType.Subject == concept.Subject,
            "CSC0009",
            "The external adapter did not bind TypeReferenceDefinition.Subject to the exact concept subject.");

        var specificationAdapter = new AdapterIdentity { Id = "specification-smoke", Version = "1.0.0" };
        var commandSubject = new SubjectId { Value = "dotnet://Ordering/Ordering/RegisterCustomer" };
        var commandKey = new ArtifactKey { Subject = commandSubject, Kind = ArtifactKind.Command };
        var scenarioSubject = new SubjectId { Value = "dotnet://Ordering.Specs/RegisteringCustomer" };
        var scenarioKey = new SpecificationScenarioKey { Scenario = scenarioSubject };
        var whenKey = new SpecificationStepKey { Scenario = scenarioKey, Index = 0 };
        var errorKey = new SpecificationStepKey { Scenario = scenarioKey, Index = 1 };
        var nameValueKey = new SpecificationValueKey { Step = whenKey, Path = ["name"] };
        var specificationEvidence = new Evidence
        {
            Adapter = specificationAdapter,
            Strength = EvidenceStrength.Exact,
            Source = new SourceRange
            {
                Path = "Customers/RegisteringCustomer.cs",
                StartLine = 1,
                StartColumn = 1,
                EndLine = 1,
                EndColumn = 20
            }
        };
        var specificationContribution = new AdapterContribution
        {
            Adapter = specificationAdapter,
            Facts =
            [
                new ArtifactFact
                {
                    Id = new FactId { Value = "specification-smoke:command" },
                    Subject = commandSubject,
                    Definition = new ArtifactDefinition { Key = commandKey, Name = "RegisterCustomer" },
                    Evidence = specificationEvidence
                },
                new ArtifactPlacementFact
                {
                    Id = new FactId { Value = "specification-smoke:command-placement" },
                    Subject = commandSubject,
                    Artifact = commandKey,
                    Placement = new ArtifactPlacement
                    {
                        Module = "Customers",
                        Features = ["Registration"],
                        Slice = "Register",
                        SliceKind = GenerationSliceKind.StateChange
                    },
                    Evidence = specificationEvidence
                },
                new SpecificationScenarioFact
                {
                    Id = new FactId { Value = "specification-smoke:scenario" },
                    Subject = scenarioSubject,
                    Definition = new SpecificationScenarioDefinition
                    {
                        Key = scenarioKey,
                        Name = "RegisteringCustomer",
                        TargetArtifact = commandKey,
                        Steps = [whenKey, errorKey]
                    },
                    Evidence = specificationEvidence
                },
                new SpecificationStepFact
                {
                    Id = new FactId { Value = "specification-smoke:when" },
                    Subject = new SubjectId { Value = $"{scenarioSubject.Value}/step/0" },
                    Definition = new SpecificationStepDefinition
                    {
                        Key = whenKey,
                        Phase = SpecificationStepPhase.When,
                        Kind = SpecificationStepKind.Command,
                        Artifact = commandKey,
                        Values = [nameValueKey]
                    },
                    Evidence = specificationEvidence
                },
                new SpecificationValueFact
                {
                    Id = new FactId { Value = "specification-smoke:name" },
                    Subject = new SubjectId { Value = $"{scenarioSubject.Value}/step/0/name" },
                    Definition = new SpecificationValueDefinition
                    {
                        Key = nameValueKey,
                        Kind = SpecificationValueKind.Text,
                        Type = new TypeReferenceDefinition { Name = "String" },
                        Scalar = "Cratis"
                    },
                    Evidence = specificationEvidence
                },
                new SpecificationStepFact
                {
                    Id = new FactId { Value = "specification-smoke:error" },
                    Subject = new SubjectId { Value = $"{scenarioSubject.Value}/step/1" },
                    Definition = new SpecificationStepDefinition
                    {
                        Key = errorKey,
                        Phase = SpecificationStepPhase.Then,
                        Kind = SpecificationStepKind.Error
                    },
                    Evidence = specificationEvidence
                }
            ]
        };
        AdapterContribution[] allContributions = [.. contributions, specificationContribution];

        var generated = new ScreenplayDefinitionGenerator().Generate(
            allContributions,
            new ScreenplayGenerationOptions { Domain = "Ordering" });
        var generatedFromReversedContributions = new ScreenplayDefinitionGenerator().Generate(
            allContributions.Reverse(),
            new ScreenplayGenerationOptions { Domain = "Ordering" });
        var generatedSourceHash = Sha256(Encoding.UTF8.GetBytes(generated.Source));
        var reversedGeneratedSourceHash = Sha256(Encoding.UTF8.GetBytes(generatedFromReversedContributions.Source));
        Require(generated.IsSuccess, "CSC0010", "The composed Screenplay did not pass compiler verification.");
        Require(
            generated.Diagnostics.Count == 0 &&
            !generated.Diagnostics.Any(diagnostic =>
                diagnostic.Code == GenerationDiagnosticCodes.DocumentDidNotCompile ||
                diagnostic.Code == GenerationDiagnosticCodes.UnstableRoundTrip),
            "CSC0011",
            "Composition introduced adapter, resolution, lowering, compile, or round-trip verification loss.");
        Require(
            generated.Source == generatedFromReversedContributions.Source && generatedFromReversedContributions.IsSuccess,
            "CSC0012",
            "Generated source changed when adapter contribution order changed.");
        Require(
            vogenFactsHash == ExpectedVogenFactsHash &&
            vogenDiagnosticsHash == ExpectedVogenDiagnosticsHash &&
            generatedSourceHash == ExpectedGeneratedSourceHash &&
            reversedGeneratedSourceHash == ExpectedGeneratedSourceHash,
            "CSC0038",
            $"Stable byte hashes changed. Vogen facts actual: {vogenFactsHash}; Vogen diagnostics actual: {vogenDiagnosticsHash}; generated source actual: {generatedSourceHash}; reversed generated source actual: {reversedGeneratedSourceHash}.");
        var admittedSpecification = generated.Graph.Specifications.Single();
        Require(
            admittedSpecification.Definition.Name == "RegisteringCustomer" &&
            admittedSpecification.Steps.Select(step => step.Definition.Kind).SequenceEqual(
                [SpecificationStepKind.Command, SpecificationStepKind.Error]) &&
            admittedSpecification.Steps[0].Values.Single().Definition.Scalar == "Cratis" &&
            admittedSpecification.Steps[1].Definition.ErrorCode is null &&
            admittedSpecification.Steps[1].Definition.ErrorMessage is null,
            "CSC0030",
            "Neutral specification facts did not preserve order, typed values, or a bare rejection.");
        Require(
            generated.Source.Contains("concept CustomerCode : String", StringComparison.Ordinal) &&
            generated.Source.Contains("rule Validate", StringComparison.Ordinal) &&
            generated.Source.Contains("message \"Customer codes cannot be blank\"", StringComparison.Ordinal) &&
            generated.Source.Contains("file Concepts/CustomerCode.cs", StringComparison.Ordinal) &&
            generated.Source.Contains("customerCode CustomerCode", StringComparison.Ordinal) &&
            !generated.Source.Contains("UnresolvedCustomerCode", StringComparison.Ordinal),
            "CSC0013",
            "The deterministic source lost the concept representation, validation, implementation file, or exact type binding.");
        var graphAdapterIds = generated.Graph.Artifacts
            .SelectMany(artifact => artifact.Variants)
            .SelectMany(variant => variant.Evidence)
            .Select(evidence => evidence.Adapter.Id)
            .ToHashSet(StringComparer.Ordinal);
        Require(
            graphAdapterIds.SetEquals([VogenAdapterId, ExternalAdapterId, specificationAdapter.Id]),
            "CSC0014",
            "The resolved successful graph lost adapter provenance.");

        var conflictingRepresentation = new ConceptRepresentationFact
        {
            Id = new FactId { Value = "external-smoke:representation-conflict" },
            Subject = concept.Subject,
            Definition = new ConceptRepresentationDefinition
            {
                Concept = concept.Subject,
                Kind = ConceptRepresentationKind.Primitive,
                Primitive = GenerationPrimitiveKind.Uuid
            },
            Evidence = new Evidence
            {
                Adapter = new AdapterIdentity { Id = ExternalAdapterId, Version = "0.7.0" },
                Strength = EvidenceStrength.Exact
            }
        };
        var conflictGraph = new GenerationResolver().Resolve(
        [
            vogenContribution,
            new AdapterContribution
            {
                Adapter = new AdapterIdentity { Id = ExternalAdapterId, Version = "0.7.0" },
                Facts = [conflictingRepresentation]
            }
        ]);
        var conflicted = conflictGraph.ConceptRepresentations.Single();
        Require(
            conflicted.IsConflicted && conflicted.Variants.Count == 2 &&
            conflictGraph.Diagnostics.Any(diagnostic =>
                diagnostic.Code == GenerationDiagnosticCodes.ConflictingConceptRepresentation &&
                diagnostic.Outcome == GenerationDiagnosticOutcome.Conflict),
            "CSC0015",
            "The resolver did not expose both incompatible concept representation variants.");
        Require(
            conflicted.Variants
                .SelectMany(variant => variant.Evidence)
                .Select(evidence => evidence.Adapter.Id)
                .ToHashSet(StringComparer.Ordinal)
                .SetEquals([VogenAdapterId, ExternalAdapterId]),
            "CSC0016",
            "Conflict visibility lost one adapter identity.");

        var unknownSubject = new SubjectId { Value = "dotnet://Compatibility/UnknownArtifact" };
        var unknownGraph = new GenerationResolver().Resolve(
        [
            new AdapterContribution
            {
                Adapter = new AdapterIdentity { Id = ExternalAdapterId, Version = "0.7.0" },
                Facts =
                [
                    new ArtifactFact
                    {
                        Id = new FactId { Value = "external-smoke:unknown-artifact" },
                        Subject = unknownSubject,
                        Evidence = new Evidence
                        {
                            Adapter = new AdapterIdentity { Id = ExternalAdapterId, Version = "0.7.0" },
                            Strength = EvidenceStrength.Exact
                        },
                        Definition = new ArtifactDefinition
                        {
                            Key = new ArtifactKey { Subject = unknownSubject, Kind = ArtifactKind.Unknown },
                            Name = "UnknownArtifact"
                        }
                    }
                ]
            }
        ]);
        var unknownDiagnostic = unknownGraph.Diagnostics.Single();
        Require(
            unknownGraph.Artifacts.Count == 0 &&
            unknownDiagnostic.Code == GenerationDiagnosticCodes.UnsupportedArtifactKind &&
            unknownDiagnostic.Outcome == GenerationDiagnosticOutcome.Unknown &&
            unknownDiagnostic.Subject == unknownSubject &&
            unknownDiagnostic.Source is null,
            "CSC0021",
            "The typed fail-closed Unknown outcome API did not omit and diagnose the affected fact exactly.");
    }

    static void ExercisePublicAdapterContracts()
    {
        var descriptor = new AdapterDescriptor
        {
            Identity = new AdapterIdentity { Id = "contract-smoke", Version = "1.0.0" },
            SourceLanguage = AdapterSourceLanguage.SourceIndependent,
            Category = AdapterCategory.Integration,
            RequiredApiCapabilities =
            [
                new AdapterApiCapability { Id = "contract.api.z" },
                new AdapterApiCapability { Id = "contract.api.a" }
            ],
            EmittedFactCapabilities =
            [
                GenerationFactCapability.Relationship,
                GenerationFactCapability.Artifact
            ]
        };
        Require(
            (int)AdapterSourceLanguage.Unknown == -1 &&
            (int)AdapterCategory.Unknown == -1 &&
            (int)AdapterHostCapability.Unknown == -1 &&
            (int)GenerationFactCapability.Unknown == -1 &&
            (int)AdapterRunDisposition.Unknown == -1 &&
            (int)GenerationFactDisposition.Unknown == -1 &&
            (int)AdapterContributionAdmissionDiagnosticCode.Unknown == -1 &&
            (int)GenerationDiagnosticSeverity.Unknown == -1 &&
            (int)GenerationDiagnosticOutcome.Unknown == -1,
            "CSC0041",
            "A new adapter-run or admission discriminator lost its explicit Unknown = -1 sentinel.");

        var descriptorAdmission = AdapterDescriptorAdmission.Admit(descriptor);
        Require(
            descriptorAdmission.IsAdmitted &&
            !ReferenceEquals(descriptorAdmission.Descriptor, descriptor) &&
            descriptorAdmission.Descriptor.RequiredApiCapabilities.Select(capability => capability.Id)
                .SequenceEqual(["contract.api.a", "contract.api.z"], StringComparer.Ordinal) &&
            descriptorAdmission.Descriptor.EmittedFactCapabilities.SequenceEqual(
                [GenerationFactCapability.Artifact, GenerationFactCapability.Relationship]),
            "CSC0042",
            "Public descriptor admission did not deeply freeze and canonicalize the descriptor.");

        var invalidDescriptor = AdapterDescriptorAdmission.Admit(descriptor with
        {
            SourceLanguage = AdapterSourceLanguage.Unknown
        });
        Require(
            !invalidDescriptor.IsAdmitted &&
            invalidDescriptor.Diagnostics.Any(diagnostic =>
                diagnostic.Code == AdapterContributionAdmissionDiagnosticCode.UnknownEnumValue),
            "CSC0043",
            "Public descriptor admission did not reject an explicit Unknown discriminator.");

        var admittedContribution = AdapterContributionAdmission.Admit(
            descriptorAdmission.Descriptor,
            new AdapterContribution { Adapter = descriptorAdmission.Descriptor.Identity });
        Require(
            admittedContribution.IsAdmitted &&
            admittedContribution.Snapshot is { Facts.Length: 0, Diagnostics.Length: 0 } &&
            !ReferenceEquals(admittedContribution.Snapshot.Descriptor, descriptorAdmission.Descriptor),
            "CSC0044",
            "Public contribution admission did not return an immutable admitted snapshot.");

        var rejectedContribution = AdapterContributionAdmission.Admit(
            descriptorAdmission.Descriptor,
            new AdapterContribution
            {
                Adapter = new AdapterIdentity { Id = "another-adapter", Version = "1.0.0" }
            });
        Require(
            !rejectedContribution.IsAdmitted &&
            rejectedContribution.Snapshot is null &&
            rejectedContribution.Diagnostics.Any(diagnostic =>
                diagnostic.Code == AdapterContributionAdmissionDiagnosticCode.ContributionAdapterMismatch),
            "CSC0045",
            "Public contribution admission did not reject an identity mismatch atomically.");
    }

    static void ExerciseCandidatePackageClosure()
    {
        var assemblyNames = new[]
        {
            typeof(AdapterDescriptor).Assembly.GetName().Name,
            typeof(ScreenplayDefinitionGenerator).Assembly.GetName().Name,
            typeof(DotNetAdapterRunner).Assembly.GetName().Name,
            typeof(VogenConceptScreenplayAdapter).Assembly.GetName().Name
        };
        Require(
            assemblyNames.Length == 4 &&
            assemblyNames.Distinct(StringComparer.Ordinal).Count() == 4 &&
            assemblyNames.ToHashSet(StringComparer.Ordinal).SetEquals(
            [
                "Cratis.Screenplay.Generation.Contracts",
                "Cratis.Screenplay.Generation",
                "Cratis.Screenplay.Generation.DotNet",
                "Cratis.Screenplay.Generation.DotNet.Vogen"
            ]),
            "CSC0046",
            "The runtime dependency closure did not load all four candidate package assemblies independently.");
    }

    static void ExerciseAdapterRunnerContracts(
        DotNetAnalysisContext context,
        DotNetProjectCompilation project,
        INamedTypeSymbol mappedType)
    {
        var duplicateFirst = new DescribedFakeAdapter(Descriptor("duplicate-smoke", "2.0.0"));
        var duplicateSecond = new DescribedFakeAdapter(Descriptor("duplicate-smoke", "1.0.0"));
        var duplicateSnapshot = DotNetAdapterRunner.Run(
            [
                DotNetAdapterRegistration.For(duplicateFirst),
                DotNetAdapterRegistration.For(duplicateSecond)
            ],
            new DotNetAnalysisContext([]),
            new DotNetAdapterOptions());
        Require(
            duplicateFirst.ProbeCount + duplicateSecond.ProbeCount == 0 &&
            duplicateFirst.AnalyzeCount + duplicateSecond.AnalyzeCount == 0 &&
            duplicateSnapshot.Adapters.All(record =>
                record.Disposition == AdapterRunDisposition.RosterRejected &&
                !record.Probed &&
                !record.Executed) &&
            duplicateSnapshot.Diagnostics.All(diagnostic =>
                diagnostic.Code == DotNetAdapterGenerationDiagnosticCodes.DuplicateAdapterId),
            "CSC0047",
            "Duplicate adapter IDs were not rejected deterministically before callbacks.");

        var independent = new DescribedFakeAdapter(Descriptor("source-independent"));
        var independentSnapshot = DotNetAdapterRunner.Run(
            [DotNetAdapterRegistration.For(independent)],
            new DotNetAnalysisContext([]),
            new DotNetAdapterOptions());
        Require(
            independent.ProbeCount == 1 &&
            independent.AnalyzeCount == 1 &&
            independentSnapshot.Adapters.Single().Disposition == AdapterRunDisposition.Admitted,
            "CSC0048",
            "A host-free source-independent adapter did not execute exactly once against an empty context.");

        var applicable = new DescribedFakeAdapter(Descriptor("applicable-smoke"));
        var notApplicable = new DescribedFakeAdapter(Descriptor("not-applicable-smoke"))
        {
            ProbeResult = new AdapterProbeNotApplicable()
        };
        var blocked = new DescribedFakeAdapter(Descriptor("blocked-smoke"))
        {
            ProbeResult = new AdapterProbeBlocked
            {
                Diagnostics =
                [
                    new GenerationDiagnostic
                    {
                        Code = "PACKAGEBLOCK001",
                        Severity = GenerationDiagnosticSeverity.Error,
                        Message = "The adapter recognized source but cannot analyze it safely"
                    }
                ]
            }
        };
        var legacy = new LegacyFakeAdapter(
            new AdapterIdentity { Id = "legacy-smoke", Version = "1.0.0" });
        var mixedSnapshot = DotNetAdapterRunner.Run(
            [
                DotNetAdapterRegistration.For(blocked),
                DotNetAdapterRegistration.ForLegacy(legacy),
                DotNetAdapterRegistration.For(notApplicable),
                DotNetAdapterRegistration.For(applicable)
            ],
            context,
            new DotNetAdapterOptions());
        Require(
            applicable.ProbeCount == 1 && applicable.AnalyzeCount == 1 &&
            notApplicable.ProbeCount == 1 && notApplicable.AnalyzeCount == 0 &&
            blocked.ProbeCount == 1 && blocked.AnalyzeCount == 0 &&
            legacy.CanAnalyzeCount == 1 && legacy.AnalyzeCount == 1 &&
            mixedSnapshot.Adapters.Single(record => record.Descriptor.Identity.Id == "applicable-smoke").Disposition == AdapterRunDisposition.Admitted &&
            mixedSnapshot.Adapters.Single(record => record.Descriptor.Identity.Id == "not-applicable-smoke").Disposition == AdapterRunDisposition.NotApplicable &&
            mixedSnapshot.Adapters.Single(record => record.Descriptor.Identity.Id == "blocked-smoke").Disposition == AdapterRunDisposition.Blocked &&
            mixedSnapshot.Adapters.Single(record => record.Descriptor.Identity.Id == "legacy-smoke").Descriptor.Category == AdapterCategory.Legacy &&
            mixedSnapshot.Adapters.Single(record => record.Descriptor.Identity.Id == "legacy-smoke").Disposition == AdapterRunDisposition.Admitted,
            "CSC0049",
            "Modern applicable, not-applicable, blocked, or legacy callbacks violated exactly-once runner semantics.");

        var mappedIdentity = new AdapterIdentity { Id = "runner-mapped", Version = "1.0.0" };
        var mappedApi = new AdapterApiCapability { Id = "runner-mapped.customer-event" };
        var mappedEvidence = DotNetSource.EvidenceFor(
            mappedType,
            mappedIdentity,
            project,
            EvidenceStrength.Exact,
            "The mapped authored event declaration is exact");
        var mappedSubject = project.SubjectForType(mappedType);
        var mappedKey = new ArtifactKey { Subject = mappedSubject, Kind = ArtifactKind.Event };
        var mutableFacts = new List<GenerationFact>
        {
            new ArtifactFact
            {
                Id = new FactId { Value = "runner-mapped:artifact:customer-registered" },
                Subject = mappedSubject,
                Evidence = mappedEvidence,
                Definition = new ArtifactDefinition
                {
                    Key = mappedKey,
                    Name = "CustomerRegistered",
                    File = mappedEvidence.Source?.Path
                }
            },
            new ArtifactPlacementFact
            {
                Id = new FactId { Value = "runner-mapped:placement:customer-registered" },
                Subject = mappedSubject,
                Evidence = mappedEvidence,
                Artifact = mappedKey,
                Placement = new ArtifactPlacement
                {
                    Module = "Package",
                    Features = ["Adapters"],
                    Slice = "Run",
                    SliceKind = GenerationSliceKind.StateChange
                }
            }
        };
        var mutableDiagnostics = new List<GenerationDiagnostic>
        {
            new()
            {
                Code = "RUNNERSMOKE001",
                Severity = GenerationDiagnosticSeverity.Information,
                Message = "The package consumer admitted a stable mapped contribution",
                Source = mappedEvidence.Source,
                Subject = mappedSubject
            }
        };
        var mappedContribution = new AdapterContribution
        {
            Adapter = mappedIdentity,
            Facts = mutableFacts,
            Diagnostics = mutableDiagnostics
        };
        var mappedDescriptor = Descriptor(
            mappedIdentity.Id,
            mappedIdentity.Version,
            AdapterSourceLanguage.CSharp,
            AdapterCategory.ApplicationFramework,
            [
                AdapterHostCapability.AuthoredSource,
                AdapterHostCapability.StableSourceLocations,
                AdapterHostCapability.SemanticAnalysis
            ],
            [mappedApi],
            [GenerationFactCapability.Artifact, GenerationFactCapability.ArtifactPlacement]);
        var probeEvidence = new AdapterProbeEvidence
        {
            Description = "The stable mapped customer event API is available",
            ApiCapability = mappedApi,
            Source = mappedEvidence.Source,
            Subject = mappedSubject
        };
        var mappedAdapter = new DescribedFakeAdapter(mappedDescriptor)
        {
            ProbeResult = new AdapterProbeApplicable { Evidence = [probeEvidence] },
            Contribution = mappedContribution
        };
        var mappedSnapshot = DotNetAdapterRunner.Run(
            [DotNetAdapterRegistration.For(mappedAdapter)],
            context,
            new DotNetAdapterOptions());
        Require(
            mappedAdapter.ProbeCount == 1 &&
            mappedAdapter.AnalyzeCount == 1 &&
            mappedSnapshot.Adapters.Single().Disposition == AdapterRunDisposition.Admitted &&
            mappedSnapshot.Facts.Length == 2 &&
            mappedSnapshot.Facts.All(record => record.Disposition == GenerationFactDisposition.Unknown) &&
            mappedSnapshot.Facts.All(record => record.Fact.Evidence.Source?.FileIdentity is not null),
            "CSC0050",
            "The deterministic runner did not admit a stable mapped .NET contribution exactly once.");

        var generationOptions = new ScreenplayGenerationOptions { Domain = "PackageConsumer" };
        var generator = new ScreenplayDefinitionGenerator();
        var generatedFromContribution = generator.Generate([mappedContribution], generationOptions);
        var originalFact = mutableFacts[0];
        var completed = (AdapterExecutionCompleted)mappedSnapshot.Adapters.Single().Execution;
        mutableFacts.Clear();
        mutableDiagnostics.Clear();
        Require(
            mappedSnapshot.Facts.Length == 2 &&
            completed.Contribution.Facts.Length == 2 &&
            completed.Contribution.Diagnostics.Length == 1 &&
            !ReferenceEquals(originalFact, mappedSnapshot.Facts[0].Fact) &&
            !ReferenceEquals(mappedDescriptor, mappedSnapshot.Adapters.Single().Descriptor) &&
            !ReferenceEquals(probeEvidence, mappedSnapshot.Adapters.Single().Probe.Evidence.Single()),
            "CSC0051",
            "Mutating adapter-owned inputs changed the deeply frozen adapter-run snapshot.");

        var generatedFromSnapshot = generator.Generate(mappedSnapshot, generationOptions);
        Require(
            generatedFromSnapshot.IsSuccess &&
            generatedFromSnapshot.Source.Contains("event CustomerRegistered", StringComparison.Ordinal) &&
            generatedFromSnapshot.Diagnostics.Any(diagnostic => diagnostic.Code == "RUNNERSMOKE001") &&
            Encoding.UTF8.GetBytes(generatedFromSnapshot.Source)
                .SequenceEqual(Encoding.UTF8.GetBytes(generatedFromContribution.Source)) &&
            JsonSerializer.SerializeToUtf8Bytes(generatedFromSnapshot.Diagnostics, _serializerOptions)
                .SequenceEqual(JsonSerializer.SerializeToUtf8Bytes(generatedFromContribution.Diagnostics, _serializerOptions)),
            "CSC0052",
            "Generate(snapshot) changed output or diagnostics compared with the original contribution overload.");
        Require(
            generatedFromSnapshot.AdapterRun is not null &&
            generatedFromSnapshot.AdapterRun.Facts.Length == 2 &&
            generatedFromSnapshot.AdapterRun.Facts.All(record =>
                record.Disposition == GenerationFactDisposition.Lowered &&
                record.Diagnostics.Length == 0) &&
            generatedFromSnapshot.AdapterRun.Diagnostics.Any(diagnostic => diagnostic.Code == "RUNNERSMOKE001"),
            "CSC0053",
            "Generate(snapshot) did not return final lowered fact dispositions and runner diagnostics.");
    }

    static void ExerciseVogenModernAndLegacyParity()
    {
        var apiTree = CSharpSyntaxTree.ParseText(
            """
            namespace Vogen;

            [System.AttributeUsage(System.AttributeTargets.Struct)]
            public sealed class ValueObjectAttribute<T> : System.Attribute;

            public sealed class Validation
            {
                public static Validation Ok { get; } = new();
                public static Validation Invalid(string message)
                {
                    _ = message;
                    return new();
                }
            }
            """,
            path: "/api/Vogen.SharedTypes.cs");
        var apiCompilation = CSharpCompilation.Create(
            "Vogen.SharedTypes",
            [apiTree],
            TrustedPlatformReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var apiImage = new MemoryStream();
        var apiEmit = apiCompilation.Emit(apiImage);
        Require(
            apiEmit.Success,
            "CSC0054",
            $"The in-memory exact Vogen API failed to compile: {string.Join(" | ", apiEmit.Diagnostics)}");

        var authoredTree = CSharpSyntaxTree.ParseText(
            """
            namespace ModernOrdering;

            [Vogen.ValueObject<string>]
            public readonly partial record struct CustomerCode
            {
                private static Vogen.Validation Validate(string value) =>
                    string.IsNullOrWhiteSpace(value)
                        ? Vogen.Validation.Invalid("Required")
                        : Vogen.Validation.Ok;
            }
            """,
            path: "/consumer/Concepts/CustomerCode.cs");
        var compilation = CSharpCompilation.Create(
            "ModernOrdering",
            [authoredTree],
            TrustedPlatformReferences().Append(MetadataReference.CreateFromImage(apiImage.ToArray())),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var errors = compilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        Require(
            errors.Length == 0,
            "CSC0055",
            $"The modern Vogen package-consumer compilation was invalid: {string.Join(" | ", errors.Select(error => error.ToString()))}");

        var sourceContext = DotNetSourcePaths.Create(
            "ModernOrdering/ModernOrdering",
            new DotNetSourcePathPolicy
            {
                DisplayRoot = DotNetSourceDisplayRoot.Workspace,
                CasePolicy = DotNetSourcePathCasePolicy.Ordinal
            },
            [
                new DotNetSourceDocument
                {
                    SyntaxTree = authoredTree,
                    ProjectRelativePath = "Concepts/CustomerCode.cs",
                    WorkspaceRelativePath = "Concepts/CustomerCode.cs"
                }
            ]);
        var context = new DotNetAnalysisContext(
        [
            new DotNetProjectCompilation
            {
                Name = "ModernOrdering",
                Compilation = compilation,
                SourceContext = sourceContext,
                AuthoredSyntaxTrees = new HashSet<SyntaxTree> { authoredTree }
            }
        ]);
        var modernAdapter = new VogenConceptScreenplayAdapter();
        IDescribedDotNetScreenplayAdapter modernContract = modernAdapter;
        var probe = modernContract.Probe(context);
        Require(
            modernContract.Descriptor.Category == AdapterCategory.Concepts &&
            modernContract.Descriptor.SourceLanguage == AdapterSourceLanguage.CSharp &&
            modernContract.Descriptor.RequiredHostCapabilities.Contains(AdapterHostCapability.StableSourceLocations) &&
            modernContract.Descriptor.RequiredApiCapabilities.Contains(VogenAdapterApiCapabilities.ValueObjectDeclaration) &&
            modernContract.Descriptor.EmittedFactCapabilities.SequenceEqual(
                [
                    GenerationFactCapability.Artifact,
                    GenerationFactCapability.ConceptRepresentation,
                    GenerationFactCapability.ConceptValidationRule
                ]) &&
            probe is AdapterProbeApplicable &&
            probe.Evidence.Any(evidence => evidence.ApiCapability == VogenAdapterApiCapabilities.ValueObjectDeclaration) &&
            probe.Evidence.Any(evidence => evidence.Source?.FileIdentity is not null),
            "CSC0056",
            "The Vogen modern descriptor or structured probe lost its declared capabilities or stable evidence.");

        var modernSnapshot = DotNetAdapterRunner.Run(
            [DotNetAdapterRegistration.For(modernAdapter)],
            context,
            new DotNetAdapterOptions());
        var legacyAdapter = new VogenConceptScreenplayAdapter();
        IDotNetScreenplayAdapter legacyContract = legacyAdapter;
        var legacySnapshot = DotNetAdapterRunner.Run(
            [DotNetAdapterRegistration.ForLegacy(legacyContract)],
            context,
            new DotNetAdapterOptions());
        var modernContribution = ((AdapterExecutionCompleted)modernSnapshot.Adapters.Single().Execution).Contribution;
        var legacyContribution = ((AdapterExecutionCompleted)legacySnapshot.Adapters.Single().Execution).Contribution;
        Require(
            modernSnapshot.Adapters.Single().Disposition == AdapterRunDisposition.Admitted &&
            legacySnapshot.Adapters.Single().Disposition == AdapterRunDisposition.Admitted &&
            JsonSerializer.SerializeToUtf8Bytes(modernContribution.Facts.Cast<object>().ToArray(), _serializerOptions)
                .SequenceEqual(JsonSerializer.SerializeToUtf8Bytes(legacyContribution.Facts.Cast<object>().ToArray(), _serializerOptions)) &&
            JsonSerializer.SerializeToUtf8Bytes(modernContribution.Diagnostics, _serializerOptions)
                .SequenceEqual(JsonSerializer.SerializeToUtf8Bytes(legacyContribution.Diagnostics, _serializerOptions)),
            "CSC0057",
            "The Vogen modern and legacy registrations did not produce byte-identical contributions.");
    }

    static AdapterDescriptor Descriptor(
        string id,
        string version = "1.0.0",
        AdapterSourceLanguage language = AdapterSourceLanguage.SourceIndependent,
        AdapterCategory category = AdapterCategory.Integration,
        IEnumerable<AdapterHostCapability>? hostCapabilities = null,
        IEnumerable<AdapterApiCapability>? apiCapabilities = null,
        IEnumerable<GenerationFactCapability>? factCapabilities = null) => new()
        {
            Identity = new AdapterIdentity { Id = id, Version = version },
            SourceLanguage = language,
            Category = category,
            RequiredHostCapabilities = hostCapabilities is null ? [] : [.. hostCapabilities],
            RequiredApiCapabilities = apiCapabilities is null ? [] : [.. apiCapabilities],
            EmittedFactCapabilities = factCapabilities is null ? [] : [.. factCapabilities]
        };

    static IReadOnlyList<MetadataReference> TrustedPlatformReferences() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToArray();

    static string Sha256(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

    static void Require(bool condition, string code, string message)
    {
        if (!condition)
        {
            throw new CurrentSourceConsumerFailure(code, message);
        }
    }

    sealed class ExternalCustomerAdapter : IDotNetScreenplayAdapter
    {
        public AdapterIdentity Identity { get; } = new() { Id = ExternalAdapterId, Version = "0.7.0" };

        public bool CanAnalyze(DotNetAnalysisContext context) =>
            context.Projects.Any(project =>
                project.Compilation.GetTypeByMetadataName("Ordering.CustomerRegistered") is not null);

        public AdapterContribution Analyze(DotNetAnalysisContext context, DotNetAdapterOptions options)
        {
            _ = options;
            var project = context.Projects.Single();
            var conceptType = project.Compilation.GetTypeByMetadataName("Ordering.CustomerCode");
            var eventType = project.Compilation.GetTypeByMetadataName("Ordering.CustomerRegistered");
            Require(conceptType is not null, "CSC0017", "The external adapter could not resolve CustomerCode.");
            Require(eventType is not null, "CSC0018", "The external adapter could not resolve CustomerRegistered.");
            var conceptSubject = project.SubjectForType(conceptType!);
            var eventSubject = project.SubjectForType(eventType!);
            var eventKey = new ArtifactKey { Subject = eventSubject, Kind = ArtifactKind.Event };
            var evidence = new Evidence { Adapter = Identity, Strength = EvidenceStrength.Exact };

            return new AdapterContribution
            {
                Adapter = Identity,
                Facts =
                [
                    new ArtifactFact
                    {
                        Id = new FactId { Value = "external-smoke:event:customer-registered" },
                        Subject = eventSubject,
                        Evidence = evidence,
                        Definition = new ArtifactDefinition
                        {
                            Key = eventKey,
                            Name = "CustomerRegistered",
                            File = "Customers/Register/CustomerRegistered.cs",
                            Properties =
                            [
                                new PropertyDefinition
                                {
                                    Name = "customerCode",
                                    Type = new TypeReferenceDefinition
                                    {
                                        Name = "UnresolvedCustomerCode",
                                        Subject = conceptSubject
                                    }
                                }
                            ]
                        }
                    },
                    new ArtifactPlacementFact
                    {
                        Id = new FactId { Value = "external-smoke:placement:customer-registered" },
                        Subject = eventSubject,
                        Evidence = evidence,
                        Artifact = eventKey,
                        Placement = new ArtifactPlacement
                        {
                            Module = "Customers",
                            Features = ["Registration"],
                            Slice = "Register",
                            SliceKind = GenerationSliceKind.StateChange
                        }
                    }
                ]
            };
        }
    }

    sealed class DescribedFakeAdapter(AdapterDescriptor descriptor) : IDescribedDotNetScreenplayAdapter
    {
        public AdapterDescriptor Descriptor { get; } = descriptor;
        public AdapterProbeResult ProbeResult { get; set; } = new AdapterProbeApplicable();
        public AdapterContribution Contribution { get; set; } = new() { Adapter = descriptor.Identity };
        public int ProbeCount { get; private set; }
        public int AnalyzeCount { get; private set; }

        public AdapterProbeResult Probe(DotNetAnalysisContext context)
        {
            _ = context;
            ProbeCount++;
            return ProbeResult;
        }

        public AdapterContribution Analyze(DotNetAnalysisContext context, DotNetAdapterOptions options)
        {
            _ = context;
            _ = options;
            AnalyzeCount++;
            return Contribution;
        }
    }

    sealed class LegacyFakeAdapter(AdapterIdentity identity) : IDotNetScreenplayAdapter
    {
        public AdapterIdentity Identity { get; } = identity;
        public int CanAnalyzeCount { get; private set; }
        public int AnalyzeCount { get; private set; }

        public bool CanAnalyze(DotNetAnalysisContext context)
        {
            _ = context;
            CanAnalyzeCount++;
            return true;
        }

        public AdapterContribution Analyze(DotNetAnalysisContext context, DotNetAdapterOptions options)
        {
            _ = context;
            _ = options;
            AnalyzeCount++;
            return new AdapterContribution { Adapter = Identity };
        }
    }

    sealed class CurrentSourceConsumerFailure(string code, string message) : Exception(message)
    {
        public string Code { get; } = code;
    }
}
CSHARP

export NUGET_PACKAGES="$WORK_DIR/.nuget/packages"
echo "Compiling the core consumer against public package baseline 0.1.0..."
dotnet restore "$CORE_DIR/CoreBaseline.csproj" --configfile "$WORK_DIR/nuget.config" --nologo
dotnet build "$CORE_DIR/CoreBaseline.csproj" --no-restore --configuration Release --nologo

echo "Compiling the Vogen consumer against its first correctly sourced public package baseline 0.5.0..."
dotnet restore "$VOGEN_DIR/VogenBaseline.csproj" --configfile "$WORK_DIR/nuget.config" --nologo
dotnet build "$VOGEN_DIR/VogenBaseline.csproj" --no-restore --configuration Release --nologo

echo "Compiling the current-source consumer only against candidate package version $CURRENT_VERSION..."
dotnet restore "$CURRENT_SOURCE_DIR/CurrentSourceConsumer.csproj" --configfile "$WORK_DIR/nuget.config" --nologo
python3 - "$CURRENT_SOURCE_DIR/obj/project.assets.json" "$CURRENT_SOURCE_DIR/CurrentSourceConsumer.csproj" "$CURRENT_VERSION" <<'PYTHON'
import json
import pathlib
import sys

assets_path = pathlib.Path(sys.argv[1])
project_path = pathlib.Path(sys.argv[2])
version = sys.argv[3]
expected = {
    "Cratis.Screenplay.Generation.Contracts",
    "Cratis.Screenplay.Generation",
    "Cratis.Screenplay.Generation.DotNet",
    "Cratis.Screenplay.Generation.DotNet.Vogen",
}
data = json.loads(assets_path.read_text(encoding="utf-8"))
frameworks = list(data["project"]["frameworks"].values())
direct = set().union(*(framework["dependencies"].keys() for framework in frameworks))
if direct != expected:
    raise SystemExit(f"CSC0058: Current-source direct package references were {sorted(direct)}, expected only {sorted(expected)}")

libraries = data["libraries"]
for package in expected:
    key = f"{package}/{version}"
    if key not in libraries or libraries[key].get("type") != "package":
        raise SystemExit(f"CSC0059: Candidate package dependency '{key}' is absent or is not a package")

if any(library.get("type") == "project" for library in libraries.values()):
    raise SystemExit("CSC0060: The current-source dependency closure contains a project reference")
project_text = project_path.read_text(encoding="utf-8")
if "<ProjectReference" in project_text or "<Reference Include=" in project_text:
    raise SystemExit("CSC0061: The current-source consumer contains a project or local assembly reference")
PYTHON
dotnet build "$CURRENT_SOURCE_DIR/CurrentSourceConsumer.csproj" --no-restore --configuration Release --nologo
dotnet run --project "$CURRENT_SOURCE_DIR/CurrentSourceConsumer.csproj" --no-build --no-restore --configuration Release

cat >"$RUNNER_DIR/CurrentRunner.csproj" <<PROJECT
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

cat >"$RUNNER_DIR/Program.cs" <<'CSHARP'
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
