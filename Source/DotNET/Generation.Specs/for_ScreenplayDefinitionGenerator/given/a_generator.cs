// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Globalization;
using System.Reflection;

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator.given;

public class a_generator : Specification
{
    protected readonly AdapterIdentity Adapter = new() { Id = "critter-stack", Version = "1.0.0" };
    protected ScreenplayDefinitionGenerator Generator = null!;

    void Establish() => Generator = new();

    protected GenerationFact[] Event(string name, string slice, params PropertyDefinition[] properties)
    {
        var subject = new SubjectId { Value = $"dotnet://Banking/Events.{name}" };
        var artifact = new ArtifactKey { Subject = subject, Kind = ArtifactKind.Event };
        var evidence = new Evidence
        {
            Adapter = Adapter,
            Strength = EvidenceStrength.Exact,
            Source = new SourceRange
            {
                Path = $"Accounts/{slice}/{name}.cs",
                StartLine = 1,
                StartColumn = 1,
                EndLine = 1,
                EndColumn = 1
            }
        };

        return
        [
            new ArtifactFact
            {
                Id = new FactId { Value = $"event:{name}" },
                Subject = subject,
                Definition = new ArtifactDefinition
                {
                    Key = artifact,
                    Name = name,
                    File = $"Accounts/{slice}/{name}.cs",
                    Properties = properties
                },
                Evidence = evidence
            },
            new ArtifactPlacementFact
            {
                Id = new FactId { Value = $"placement:{name}" },
                Subject = subject,
                Artifact = artifact,
                Placement = new ArtifactPlacement
                {
                    Module = "Accounts",
                    Features = ["Accounts"],
                    Slice = slice,
                    SliceKind = GenerationSliceKind.StateChange
                },
                Evidence = evidence
            }
        ];
    }

    protected (SubjectId Subject, GenerationFact[] Facts) Concept(
        string name,
        GenerationPrimitiveKind primitive,
        string? file = null)
    {
        var subject = new SubjectId { Value = $"dotnet://Banking/Concepts.{name}" };
        var evidence = new Evidence
        {
            Adapter = Adapter,
            Strength = EvidenceStrength.Exact,
            Source = new SourceRange
            {
                Path = file ?? $"Concepts/{name}.cs",
                StartLine = 1,
                StartColumn = 1,
                EndLine = 1,
                EndColumn = 1
            }
        };
        return (subject,
        [
            new ArtifactFact
            {
                Id = new FactId { Value = $"concept:{name}" },
                Subject = subject,
                Definition = new ArtifactDefinition
                {
                    Key = new ArtifactKey { Subject = subject, Kind = ArtifactKind.Concept },
                    Name = name,
                    File = file ?? $"Concepts/{name}.cs"
                },
                Evidence = evidence
            },
            new ConceptRepresentationFact
            {
                Id = new FactId { Value = $"concept-representation:{name}" },
                Subject = subject,
                Definition = new ConceptRepresentationDefinition
                {
                    Concept = subject,
                    Kind = ConceptRepresentationKind.Primitive,
                    Primitive = primitive
                },
                Evidence = evidence
            }
        ]);
    }

    protected ConceptValidationRuleFact Validation(
        string id,
        SubjectId subject,
        string ruleIdentity,
        string? predicate,
        string? message = null,
        string? implementationFile = null) => new()
    {
        Id = new FactId { Value = id },
        Subject = subject,
        Definition = new ConceptValidationRuleDefinition
        {
            Concept = subject,
            RuleIdentity = ruleIdentity,
            Kind = ConceptValidationRuleKind.NamedPredicate,
            Predicate = predicate,
            Message = message,
            ImplementationFile = implementationFile
        },
        Evidence = new Evidence { Adapter = Adapter, Strength = EvidenceStrength.Exact }
    };

    protected static PropertyDefinition Property(string name, string type, SubjectId? subject = null) => new()
    {
        Name = name,
        Type = new TypeReferenceDefinition { Name = type, Subject = subject }
    };

    protected AdapterContribution Contribution(params GenerationFact[] facts) => new()
    {
        Adapter = Adapter,
        Facts = facts
    };

    protected static AdapterRunRecord Completed(
        AdapterIdentity adapter,
        IReadOnlyList<GenerationFact> facts,
        IReadOnlyList<GenerationDiagnostic>? diagnostics = null)
    {
        var descriptor = new AdapterDescriptor
        {
            Identity = adapter,
            SourceLanguage = AdapterSourceLanguage.SourceIndependent,
            Category = AdapterCategory.ApplicationFramework
        };
        var contribution = new AdapterContributionSnapshot
        {
            Descriptor = descriptor,
            Facts = [.. facts],
            Diagnostics = diagnostics is null ? [] : [.. diagnostics]
        };
        return new AdapterRunRecord
        {
            Considered = true,
            Probed = true,
            Executed = true,
            Descriptor = descriptor,
            Probe = new AdapterProbeApplicable(),
            Execution = new AdapterExecutionCompleted
            {
                Contribution = contribution,
                Diagnostics = contribution.Diagnostics
            },
            Disposition = AdapterRunDisposition.Admitted
        };
    }

    protected static AdapterRunSnapshot Snapshot(params AdapterRunRecord[] adapters) => new()
    {
        Adapters = [.. adapters],
        Facts =
        [
            .. adapters
                .SelectMany(record => record.Execution is AdapterExecutionCompleted completed
                    ? completed.Contribution.Facts
                    : [])
                .Select(fact => new GenerationFactRecord { Fact = fact })
        ]
    };

    protected static string AdapterRunProjection(object? value)
    {
        if (value is null)
        {
            return ProjectionNode([null]);
        }

        if (value is string text)
        {
            return ProjectionNode([typeof(string).FullName, text]);
        }

        if (value is Version version)
        {
            return ProjectionNode([typeof(Version).FullName, version.ToString()]);
        }

        var type = value.GetType();
        if (type.IsEnum || type.IsPrimitive || value is decimal)
        {
            return ProjectionNode([type.FullName, Convert.ToString(value, CultureInfo.InvariantCulture)]);
        }

        if (value is IEnumerable enumerable)
        {
            return ProjectionNode(
            [
                type.FullName,
                .. enumerable.Cast<object?>().Select(AdapterRunProjection)
            ]);
        }

        var properties = type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.GetIndexParameters().Length == 0)
            .OrderBy(property => property.Name, StringComparer.Ordinal);
        return ProjectionNode(
        [
            type.FullName,
            .. properties.Select(property => ProjectionNode([property.Name, AdapterRunProjection(property.GetValue(value))]))
        ]);
    }

    static string ProjectionNode(IEnumerable<string?> values) => string.Concat(values.Select(value => value is null
        ? "-1:"
        : $"{value.Length.ToString(CultureInfo.InvariantCulture)}:{value}"));
}
