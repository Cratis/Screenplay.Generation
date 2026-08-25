#!/usr/bin/env bash
# Copyright (c) Cratis. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.

# Compiles consumers against the compatibility ancestry, then runs those unchanged binaries
# beside the packages being validated. A separate source consumer compiles only against the
# current candidate packages and exercises the public 0.7+ composition surface. Together these
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

internal static class Program
{
    const string ExternalAdapterId = "external-smoke";
    const string VogenAdapterId = "vogen";

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
            (int)EvidenceStrength.Unknown == -1,
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
                public sealed class CustomerBatch : System.Collections.Generic.List<CustomerRegistered>;
                public static class CustomerHandler
                {
                    public static void Validate(CustomerRegistered request) => _ = request;
                    public static void Load(CustomerRegistered request, int version) => _ = (request, version);
                    public static void Validate(CustomerCode request) => _ = request;
                }
            }
            """,
            path: "/consumer/Concepts/CustomerCode.cs");
        var generatedLookalikeTree = CSharpSyntaxTree.ParseText(
            """
            namespace Ordering;

            [Vogen.ValueObject<int>]
            public readonly partial record struct GeneratedOnlyCode;
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
            mappedPlacement.Placements.Single().Structure.Source?.FileIdentity?.Project == sourceContext.ProjectIdentity,
            "CSC0031",
            "Source placement did not derive directly from the fixed host-owned source snapshot and identity.");

        _ = typeof(DotNetInvocations);
        var customerRegistered = compilation.GetTypeByMetadataName("Ordering.CustomerRegistered")!;
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

    static IReadOnlyList<MetadataReference> TrustedPlatformReferences() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToArray();

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
