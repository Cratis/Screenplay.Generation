// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Represents the syntax tree and diagnostics produced while lowering a resolved graph.
/// </summary>
public sealed record ScreenplayLoweringResult
{
    /// <summary>
    /// Gets the generated Screenplay syntax tree.
    /// </summary>
    public required ApplicationSyntax Application { get; init; }

    /// <summary>
    /// Gets diagnostics produced while lowering the graph.
    /// </summary>
    public IReadOnlyList<GenerationDiagnostic> Diagnostics { get; init; } = [];

    internal ScreenplayLoweringCoverage Coverage { get; init; } = ScreenplayLoweringCoverage.Empty;
}

/// <summary>
/// Lowers a resolved semantic application graph into the canonical Screenplay syntax tree.
/// </summary>
public sealed class ScreenplayLowerer
{
    static readonly SourceLocation _generated = SourceLocation.Start;

    /// <summary>
    /// Lowers the supplied graph.
    /// </summary>
    /// <param name="graph">The resolved application graph.</param>
    /// <param name="domain">The generated Screenplay domain name.</param>
    /// <returns>The lowering result.</returns>
    public ScreenplayLoweringResult Lower(ResolvedApplicationGraph graph, string domain)
    {
        var diagnostics = new List<GenerationDiagnostic>();
        var coverage = new ScreenplayLoweringCoverageBuilder();
        ResolverDiagnosticCoverage.Propagate(graph, coverage);
        ReportUnsupportedRelationships(graph, diagnostics);
        var placements = graph.Placements
            .Where(_ => !_.IsConflicted)
            .ToDictionary(_ => Structural.ArtifactKey(_.Artifact), _ => _.EffectiveVariants.Single().Placement, StringComparer.Ordinal);
        var definitions = graph.Artifacts
            .Where(_ => !_.IsConflicted)
            .Select(_ => _.Variants.Single().Definition)
            .ToArray();
        var (concepts, conceptNames) = BuildConcepts(graph, definitions, diagnostics, coverage);
        var artifactsWithMissingConceptReferences = ReportMissingConceptReferences(definitions, conceptNames, diagnostics, coverage);
        var context = new LoweringContext(graph, conceptNames);
        foreach (var unplaced in definitions.Where(_ =>
                     _.Key.Kind != ArtifactKind.Concept &&
                     !placements.ContainsKey(Structural.ArtifactKey(_.Key))))
        {
            var diagnostic = UnplacedArtifactDiagnostic(unplaced, SourceForArtifact(graph, unplaced.Key));
            diagnostics.Add(diagnostic);
            coverage.Omitted(GenerationFactSemanticKey.Artifact(unplaced), diagnostic);
        }

        var artifacts = definitions
            .Where(_ => placements.ContainsKey(Structural.ArtifactKey(_.Key)) &&
                        !artifactsWithMissingConceptReferences.Contains(Structural.ArtifactKey(_.Key)))
            .Select(_ => new PlacedArtifact(
                _,
                placements[Structural.ArtifactKey(_.Key)],
                SourceForPlacement(graph, _.Key)))
            .OrderBy(_ => Canonical.Artifact(_.Definition), StringComparer.Ordinal)
            .ToArray();

        foreach (var unsupported in artifacts.Where(_ =>
                     _.Definition.Key.Kind != ArtifactKind.Concept &&
                     !CanLower(_.Definition.Key.Kind)))
        {
            var isKnownKind = IsKnownArtifactKind(unsupported.Definition.Key.Kind);
            var diagnostic = new GenerationDiagnostic
            {
                Code = isKnownKind ? GenerationDiagnosticCodes.UnsupportedArtifact : GenerationDiagnosticCodes.UnsupportedArtifactKind,
                Severity = GenerationDiagnosticSeverity.Warning,
                Outcome = isKnownKind
                    ? GenerationDiagnosticOutcome.Unsupported
                    : OutcomeFor(unsupported.Definition.Key.Kind, ArtifactKind.Unknown),
                Message = isKnownKind
                    ? $"The recognized {unsupported.Definition.Key.Kind} artifact '{unsupported.Definition.Name}' cannot yet be represented by the Screenplay lowerer and was omitted"
                    : $"Artifact '{unsupported.Definition.Name}' uses unknown or undefined ArtifactKind value '{(int)unsupported.Definition.Key.Kind}' and was omitted",
                Source = SourceForArtifact(graph, unsupported.Definition.Key),
                Subject = unsupported.Definition.Key.Subject
            };
            diagnostics.Add(diagnostic);
            coverage.Omitted(GenerationFactSemanticKey.Artifact(unsupported.Definition), diagnostic);
            coverage.Omitted(GenerationFactSemanticKey.Placement(unsupported.Definition.Key, unsupported.Placement), diagnostic);
        }

        ModuleSyntax[] modules =
        [
            .. artifacts
                .Where(_ => CanLower(_.Definition.Key.Kind))
                .GroupBy(_ => _.Placement.Module, StringComparer.Ordinal)
                .OrderBy(_ => _.Key, StringComparer.Ordinal)
                .Select(_ => BuildModule(_.Key, _, context, diagnostics, coverage))
        ];

        return new()
        {
            Application = new ApplicationSyntax(
                [],
                concepts,
                [],
                modules,
                _generated,
                new DomainSyntax(domain, _generated)),
            Diagnostics = [.. diagnostics.OrderBy(Canonical.Diagnostic, StringComparer.Ordinal)],
            Coverage = coverage.Build()
        };
    }

    static (ConceptSyntax[] Concepts, IReadOnlyDictionary<string, string> Names) BuildConcepts(
        ResolvedApplicationGraph graph,
        IReadOnlyList<ArtifactDefinition> definitions,
        List<GenerationDiagnostic> diagnostics,
        ScreenplayLoweringCoverageBuilder coverage)
    {
        var conceptDefinitions = definitions.Where(_ => _.Key.Kind == ArtifactKind.Concept).ToArray();
        var conflictingNames = conceptDefinitions
            .GroupBy(_ => _.Name, StringComparer.Ordinal)
            .Where(_ => _.Select(definition => definition.Key.Subject.Value).Distinct(StringComparer.Ordinal).Count() > 1)
            .ToDictionary(_ => _.Key, _ => _.ToArray(), StringComparer.Ordinal);
        foreach (var conflict in conflictingNames.OrderBy(_ => _.Key, StringComparer.Ordinal))
        {
            var diagnostic = new GenerationDiagnostic
            {
                Code = GenerationDiagnosticCodes.ConflictingConceptName,
                Severity = GenerationDiagnosticSeverity.Error,
                Outcome = GenerationDiagnosticOutcome.Conflict,
                Message = $"Concept name '{conflict.Key}' is required by {conflict.Value.Length} distinct source subjects",
                Subject = conflict.Value.OrderBy(_ => _.Key.Subject.Value, StringComparer.Ordinal).First().Key.Subject
            };
            diagnostics.Add(diagnostic);
            foreach (var definition in conflict.Value)
            {
                coverage.Conflicted(GenerationFactSemanticKey.Artifact(definition), diagnostic);
            }
        }

        var concepts = new List<ConceptSyntax>();
        var names = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var definition in conceptDefinitions
                     .Where(_ => !conflictingNames.ContainsKey(_.Name))
                     .OrderBy(_ => _.Name, StringComparer.Ordinal)
                     .ThenBy(_ => _.Key.Subject.Value, StringComparer.Ordinal))
        {
            var resolved = graph.ConceptRepresentations.FirstOrDefault(_ => _.Concept == definition.Key.Subject);
            if (resolved is null)
            {
                var diagnostic = new GenerationDiagnostic
                {
                    Code = GenerationDiagnosticCodes.MissingConceptRepresentation,
                    Severity = GenerationDiagnosticSeverity.Warning,
                    Outcome = GenerationDiagnosticOutcome.Unknown,
                    Message = $"Concept '{definition.Name}' has no proven representation and was omitted",
                    Subject = definition.Key.Subject
                };
                diagnostics.Add(diagnostic);
                coverage.Omitted(GenerationFactSemanticKey.Artifact(definition), diagnostic);
                continue;
            }

            if (resolved.IsConflicted)
            {
                continue;
            }

            var representationVariant = resolved.Variants.Single();
            var representation = representationVariant.Definition;
            if (representation.Kind == ConceptRepresentationKind.Unknown || !Enum.IsDefined(representation.Kind))
            {
                var diagnostic = new GenerationDiagnostic
                {
                    Code = GenerationDiagnosticCodes.UnsupportedConceptRepresentationKind,
                    Severity = GenerationDiagnosticSeverity.Warning,
                    Outcome = OutcomeFor(representation.Kind, ConceptRepresentationKind.Unknown),
                    Message = $"Concept '{definition.Name}' uses unknown or undefined ConceptRepresentationKind value '{(int)representation.Kind}' and was omitted",
                    Source = FirstSource(representationVariant.Evidence),
                    Subject = definition.Key.Subject
                };
                diagnostics.Add(diagnostic);
                coverage.Omitted(GenerationFactSemanticKey.Artifact(definition), diagnostic);
                coverage.Omitted(GenerationFactSemanticKey.ConceptRepresentation(representation), diagnostic);
                continue;
            }

            if (representation.Primitive is { } primitive &&
                (primitive == GenerationPrimitiveKind.Unknown || !Enum.IsDefined(primitive)))
            {
                var diagnostic = new GenerationDiagnostic
                {
                    Code = GenerationDiagnosticCodes.UnsupportedPrimitiveKind,
                    Severity = GenerationDiagnosticSeverity.Warning,
                    Outcome = OutcomeFor(primitive, GenerationPrimitiveKind.Unknown),
                    Message = $"Concept '{definition.Name}' uses unknown or undefined GenerationPrimitiveKind value '{(int)primitive}' and was omitted",
                    Source = FirstSource(representationVariant.Evidence),
                    Subject = definition.Key.Subject
                };
                diagnostics.Add(diagnostic);
                coverage.Omitted(GenerationFactSemanticKey.Artifact(definition), diagnostic);
                coverage.Omitted(GenerationFactSemanticKey.ConceptRepresentation(representation), diagnostic);
                continue;
            }

            if (TypeOf(representation) is not { } type)
            {
                var diagnostic = new GenerationDiagnostic
                {
                    Code = GenerationDiagnosticCodes.UnsupportedConceptRepresentation,
                    Severity = GenerationDiagnosticSeverity.Warning,
                    Outcome = GenerationDiagnosticOutcome.Unsupported,
                    Message = $"Concept '{definition.Name}' has an invalid or unsupported {representation.Kind} representation and was omitted",
                    Source = FirstSource(representationVariant.Evidence),
                    Subject = definition.Key.Subject
                };
                diagnostics.Add(diagnostic);
                coverage.Omitted(GenerationFactSemanticKey.Artifact(definition), diagnostic);
                coverage.Omitted(GenerationFactSemanticKey.ConceptRepresentation(representation), diagnostic);
                continue;
            }

            IReadOnlyList<string> values = representation.Kind == ConceptRepresentationKind.Enumeration
                ? [.. representation.EnumerationValues.Select(EnumValue)]
                : [];
            if (values.Any(_ => !IsEnumValue(_)) || values.Distinct(StringComparer.Ordinal).Count() != values.Count)
            {
                var diagnostic = new GenerationDiagnostic
                {
                    Code = GenerationDiagnosticCodes.UnsupportedConceptRepresentation,
                    Severity = GenerationDiagnosticSeverity.Warning,
                    Outcome = GenerationDiagnosticOutcome.Unsupported,
                    Message = $"Concept '{definition.Name}' has empty or duplicate enumeration values after Screenplay naming and was omitted",
                    Source = FirstSource(representationVariant.Evidence),
                    Subject = definition.Key.Subject
                };
                diagnostics.Add(diagnostic);
                coverage.Omitted(GenerationFactSemanticKey.Artifact(definition), diagnostic);
                coverage.Omitted(GenerationFactSemanticKey.ConceptRepresentation(representation), diagnostic);
                continue;
            }

            var attributes = BuildConceptAttributes(graph, definition, diagnostics, coverage);
            var validations = BuildConceptValidations(graph, definition, diagnostics, coverage);
            concepts.Add(new ConceptSyntax(definition.Name, type, attributes, values, _generated, validations)
            {
                File = FileFrom(definition.File)
            });
            names[definition.Key.Subject.Value] = definition.Name;
            coverage.Lowered(GenerationFactSemanticKey.Artifact(definition));
            coverage.Lowered(GenerationFactSemanticKey.ConceptRepresentation(representation));
        }

        return ([.. concepts], names);
    }

    static ConceptAttributeSyntax[] BuildConceptAttributes(
        ResolvedApplicationGraph graph,
        ArtifactDefinition concept,
        List<GenerationDiagnostic> diagnostics,
        ScreenplayLoweringCoverageBuilder coverage)
    {
        var attributes = new List<ConceptAttributeSyntax>();
        foreach (var resolved in graph.ConceptAttributes
                     .Where(_ => _.Concept == concept.Key.Subject && !_.IsConflicted)
                     .OrderBy(_ => _.Name, StringComparer.Ordinal))
        {
            var definition = resolved.Variants.Single().Definition;
            if (definition.Kind != ConceptAttributeKind.Named)
            {
                var diagnostic = new GenerationDiagnostic
                {
                    Code = GenerationDiagnosticCodes.UnsupportedConceptAttributeKind,
                    Severity = GenerationDiagnosticSeverity.Warning,
                    Outcome = OutcomeFor(definition.Kind, ConceptAttributeKind.Unknown),
                    Message = $"Concept '{concept.Name}' has unknown or undefined ConceptAttributeKind value '{(int)definition.Kind}', which was omitted",
                    Source = FirstSource(resolved.Variants.Single().Evidence),
                    Subject = concept.Key.Subject
                };
                diagnostics.Add(diagnostic);
                coverage.Omitted(GenerationFactSemanticKey.ConceptAttribute(definition), diagnostic);
                continue;
            }

            if (!IsIdentifier(definition.Name))
            {
                var diagnostic = new GenerationDiagnostic
                {
                    Code = GenerationDiagnosticCodes.UnsupportedConceptAttribute,
                    Severity = GenerationDiagnosticSeverity.Warning,
                    Outcome = GenerationDiagnosticOutcome.Unsupported,
                    Message = $"Concept '{concept.Name}' has invalid attribute name '{definition.Name}', which was omitted",
                    Source = FirstSource(resolved.Variants.Single().Evidence),
                    Subject = concept.Key.Subject
                };
                diagnostics.Add(diagnostic);
                coverage.Omitted(GenerationFactSemanticKey.ConceptAttribute(definition), diagnostic);
                continue;
            }

            attributes.Add(new ConceptAttributeSyntax(definition.Name, _generated, definition.Reason));
            coverage.Lowered(GenerationFactSemanticKey.ConceptAttribute(definition));
        }

        return [.. attributes];
    }

    static ValidateSyntax[] BuildConceptValidations(
        ResolvedApplicationGraph graph,
        ArtifactDefinition concept,
        List<GenerationDiagnostic> diagnostics,
        ScreenplayLoweringCoverageBuilder coverage)
    {
        var rules = new List<ValidationRuleSyntax>();
        foreach (var resolved in graph.ConceptValidationRules
                     .Where(_ => _.Concept == concept.Key.Subject)
                     .OrderBy(_ => _.RuleIdentity, StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(resolved.RuleIdentity))
            {
                var diagnostic = UnsupportedValidation(
                    concept,
                    resolved.RuleIdentity,
                    "has no rule identity",
                    FirstSource(resolved.Variants.SelectMany(_ => _.Evidence)));
                diagnostics.Add(diagnostic);
                foreach (var variant in resolved.Variants)
                {
                    coverage.Omitted(GenerationFactSemanticKey.ConceptValidationRule(variant.Definition), diagnostic);
                }
                continue;
            }

            if (resolved.IsConflicted)
            {
                continue;
            }

            var validationVariant = resolved.Variants.Single();
            var definition = validationVariant.Definition;
            if (definition.Kind == ConceptValidationRuleKind.Unknown || !Enum.IsDefined(definition.Kind))
            {
                var diagnostic = new GenerationDiagnostic
                {
                    Code = GenerationDiagnosticCodes.UnsupportedConceptValidationRuleKind,
                    Severity = GenerationDiagnosticSeverity.Warning,
                    Outcome = OutcomeFor(definition.Kind, ConceptValidationRuleKind.Unknown),
                    Message = $"Concept '{concept.Name}' validation rule '{definition.RuleIdentity}' uses unknown or undefined ConceptValidationRuleKind value '{(int)definition.Kind}' and was omitted",
                    Source = FirstSource(validationVariant.Evidence),
                    Subject = concept.Key.Subject
                };
                diagnostics.Add(diagnostic);
                coverage.Omitted(GenerationFactSemanticKey.ConceptValidationRule(definition), diagnostic);
                continue;
            }

            if (definition.Kind != ConceptValidationRuleKind.NamedPredicate ||
                definition.Predicate is not { } predicate ||
                !IsRuleIdentifier(predicate) ||
                !IsValidImplementationFile(definition.ImplementationFile))
            {
                var diagnostic = UnsupportedValidation(
                    concept,
                    definition.RuleIdentity,
                    "has invalid or missing required data",
                    FirstSource(validationVariant.Evidence));
                diagnostics.Add(diagnostic);
                coverage.Omitted(GenerationFactSemanticKey.ConceptValidationRule(definition), diagnostic);
                continue;
            }

            rules.Add(new ValidationRuleSyntax(
                ValidationRuleSyntax.ConceptValue,
                ValidationRuleKind.Rule,
                new PathExpressionSyntax(predicate, _generated),
                definition.Message,
                _generated,
                FileFrom(definition.ImplementationFile)));
            coverage.Lowered(GenerationFactSemanticKey.ConceptValidationRule(definition));
        }

        return rules.Count == 0
            ? []
            : [new DeclarativeValidateSyntax(rules, _generated)];
    }

    static GenerationDiagnostic UnsupportedValidation(
        ArtifactDefinition concept,
        string? ruleIdentity,
        string reason,
        SourceRange? source) => new()
        {
            Code = GenerationDiagnosticCodes.UnsupportedConceptValidationRule,
            Severity = GenerationDiagnosticSeverity.Warning,
            Outcome = GenerationDiagnosticOutcome.Unsupported,
            Message = $"Concept '{concept.Name}' validation rule '{ruleIdentity ?? string.Empty}' {reason} and was omitted",
            Source = source,
            Subject = concept.Key.Subject
        };

    static HashSet<string> ReportMissingConceptReferences(
        IReadOnlyList<ArtifactDefinition> definitions,
        IReadOnlyDictionary<string, string> conceptNames,
        List<GenerationDiagnostic> diagnostics,
        ScreenplayLoweringCoverageBuilder coverage)
    {
        var missing = definitions
            .SelectMany(definition => definition.Properties
                .Where(property => property.Type.Subject is not null && !conceptNames.ContainsKey(property.Type.Subject.Value))
                .Select(property => new { Artifact = definition, property.Type }))
            .ToArray();
        foreach (var type in missing
                     .GroupBy(_ => _.Type.Subject!.Value, StringComparer.Ordinal)
                     .OrderBy(_ => _.Key, StringComparer.Ordinal)
                     .Select(_ => _.First().Type))
        {
            diagnostics.Add(new GenerationDiagnostic
            {
                Code = GenerationDiagnosticCodes.MissingConceptReference,
                Severity = GenerationDiagnosticSeverity.Warning,
                Outcome = GenerationDiagnosticOutcome.Unknown,
                Message = $"Type reference '{type.Name}' targets concept '{type.Subject!.Value}', which could not be emitted",
                Subject = type.Subject
            });
        }

        foreach (var definition in missing
                     .Select(_ => _.Artifact)
                     .DistinctBy(Canonical.Artifact, StringComparer.Ordinal))
        {
            var diagnostic = diagnostics.First(_ =>
                _.Code == GenerationDiagnosticCodes.MissingConceptReference &&
                definition.Properties.Any(property => property.Type.Subject == _.Subject));
            coverage.Omitted(GenerationFactSemanticKey.Artifact(definition), diagnostic);
        }

        return [.. missing.Select(_ => Structural.ArtifactKey(_.Artifact.Key))];
    }

    static string EnumValue(string value) => value.Length == 0
        ? value
        : $"{char.ToLowerInvariant(value[0])}{value[1..]}";

    static bool IsIdentifier(string value) =>
        value.Length > 0 &&
        (value[0] == '_' || char.IsLower(value[0])) &&
        value.Skip(1).All(_ => _ == '_' || char.IsLetterOrDigit(_));

    static bool IsEnumValue(string value) =>
        IsIdentifier(value) &&
        !string.Equals(value, "file", StringComparison.Ordinal) &&
        !string.Equals(value, "validate", StringComparison.Ordinal);

    static bool IsRuleIdentifier(string value) =>
        value.Length > 0 &&
        (value[0] == '_' || (value[0] is >= 'A' and <= 'Z') || (value[0] is >= 'a' and <= 'z')) &&
        value.Skip(1).All(_ =>
            _ == '_' ||
            (_ is >= 'A' and <= 'Z') ||
            (_ is >= 'a' and <= 'z') ||
            (_ is >= '0' and <= '9'));

    static bool IsValidImplementationFile(string? value) =>
        value is null ||
        (!string.IsNullOrWhiteSpace(value) && value.IndexOfAny(['\r', '\n']) < 0);

    static string? TypeOf(ConceptRepresentationDefinition representation) => representation.Kind switch
    {
        ConceptRepresentationKind.Primitive when representation.Primitive is not null && representation.EnumerationValues.Count == 0 => representation.Primitive.Value switch
        {
            GenerationPrimitiveKind.Uuid => "Uuid",
            GenerationPrimitiveKind.Text => "String",
            GenerationPrimitiveKind.WholeNumber => "Int",
            GenerationPrimitiveKind.Number => "Decimal",
            GenerationPrimitiveKind.Boolean => "Bool",
            GenerationPrimitiveKind.Date => "Date",
            GenerationPrimitiveKind.DateTime => "DateTime",
            _ => null
        },
        ConceptRepresentationKind.Enumeration when representation.Primitive is null && representation.EnumerationValues.Count > 0 => "Enum",
        _ => null
    };

    static void ReportUnsupportedRelationships(
        ResolvedApplicationGraph graph,
        List<GenerationDiagnostic> diagnostics)
    {
        foreach (var relationship in graph.Relationships.Where(_ =>
                     _.Key.Kind == RelationshipKind.Unknown ||
                     !Enum.IsDefined(_.Key.Kind)))
        {
            diagnostics.Add(new GenerationDiagnostic
            {
                Code = GenerationDiagnosticCodes.UnsupportedRelationshipKind,
                Severity = GenerationDiagnosticSeverity.Warning,
                Outcome = OutcomeFor(relationship.Key.Kind, RelationshipKind.Unknown),
                Message = $"Relationship from '{relationship.Key.Source.Value}' to '{relationship.Key.Target.Value}' uses unknown or undefined RelationshipKind value '{(int)relationship.Key.Kind}' and was omitted",
                Source = FirstSource(relationship.Evidence),
                Subject = relationship.Key.Source
            });
        }
    }

    static GenerationDiagnostic UnplacedArtifactDiagnostic(ArtifactDefinition artifact, SourceRange? source)
    {
        if (!IsKnownArtifactKind(artifact.Key.Kind))
        {
            return new()
            {
                Code = GenerationDiagnosticCodes.UnsupportedArtifactKind,
                Severity = GenerationDiagnosticSeverity.Warning,
                Outcome = OutcomeFor(artifact.Key.Kind, ArtifactKind.Unknown),
                Message = $"Artifact '{artifact.Name}' uses unknown or undefined ArtifactKind value '{(int)artifact.Key.Kind}' and was omitted",
                Source = source,
                Subject = artifact.Key.Subject
            };
        }

        if (!CanLower(artifact.Key.Kind))
        {
            return new()
            {
                Code = GenerationDiagnosticCodes.UnsupportedArtifact,
                Severity = GenerationDiagnosticSeverity.Warning,
                Outcome = GenerationDiagnosticOutcome.Unsupported,
                Message = $"The recognized {artifact.Key.Kind} artifact '{artifact.Name}' cannot yet be represented by the Screenplay lowerer and was omitted",
                Source = source,
                Subject = artifact.Key.Subject
            };
        }

        return new()
        {
            Code = GenerationDiagnosticCodes.IncompleteArtifact,
            Severity = GenerationDiagnosticSeverity.Warning,
            Outcome = GenerationDiagnosticOutcome.Unknown,
            Message = $"The recognized {artifact.Key.Kind} artifact '{artifact.Name}' has no Screenplay placement and was omitted",
            Source = source,
            Subject = artifact.Key.Subject
        };
    }

    static SourceRange? SourceForArtifact(ResolvedApplicationGraph graph, ArtifactKey key) =>
        FirstSource(graph.Artifacts
            .Where(_ => Structural.ArtifactKey(_.Key) == Structural.ArtifactKey(key))
            .SelectMany(_ => _.Variants)
            .SelectMany(_ => _.Evidence));

    static SourceRange? SourceForPlacement(ResolvedApplicationGraph graph, ArtifactKey key) =>
        FirstSource(graph.Placements
            .Where(_ => Structural.ArtifactKey(_.Artifact) == Structural.ArtifactKey(key))
            .SelectMany(_ => _.EffectiveVariants)
            .SelectMany(_ => _.Evidence));

    static SourceRange? FirstSource(IEnumerable<Evidence> evidence) =>
        evidence
            .Where(_ => _.Source is not null)
            .OrderBy(Canonical.Evidence, StringComparer.Ordinal)
            .Select(_ => _.Source)
            .FirstOrDefault();

    static bool IsKnownArtifactKind(ArtifactKind kind) => Enum.IsDefined(kind) && kind != ArtifactKind.Unknown;

    static bool CanLower(ArtifactKind kind) => kind is ArtifactKind.Command or ArtifactKind.Event or ArtifactKind.Query or ArtifactKind.ReadModel or ArtifactKind.Reducer;

    static GenerationDiagnosticOutcome OutcomeFor<TEnum>(TEnum value, TEnum unknown)
        where TEnum : struct, Enum =>
        EqualityComparer<TEnum>.Default.Equals(value, unknown)
            ? GenerationDiagnosticOutcome.Unknown
            : GenerationDiagnosticOutcome.Unsupported;

    static ModuleSyntax BuildModule(
        string name,
        IEnumerable<PlacedArtifact> artifacts,
        LoweringContext context,
        List<GenerationDiagnostic> diagnostics,
        ScreenplayLoweringCoverageBuilder coverage)
    {
        var root = new FeatureNode(string.Empty);
        foreach (var artifact in artifacts)
        {
            var features = artifact.Placement.Features.Count == 0
                ? ["General"]
                : artifact.Placement.Features;
            var feature = root;
            foreach (var namePart in features)
            {
                feature = feature.Feature(namePart);
            }

            feature.Add(artifact);
        }

        return new(name, [], [.. root.Children.Values.Select(_ => _.Build(context, diagnostics, coverage))], _generated);
    }

    static SliceSyntax BuildSlice(
        string name,
        GenerationSliceKind kind,
        IReadOnlyList<PlacedArtifact> artifacts,
        LoweringContext context,
        List<GenerationDiagnostic> diagnostics,
        ScreenplayLoweringCoverageBuilder coverage)
    {
        var commandArtifacts = ArtifactsOfKind(artifacts, ArtifactKind.Command);
        var commands = commandArtifacts
            .Select(_ => BuildCommand(_.Definition, context, coverage))
            .ToArray();
        MarkLowered(commandArtifacts, coverage);

        var eventArtifacts = ArtifactsOfKind(artifacts, ArtifactKind.Event);
        var events = eventArtifacts
            .Select(_ => BuildEvent(_.Definition, context))
            .ToArray();
        MarkLowered(eventArtifacts, coverage);

        var queries = new List<QuerySyntax>();
        foreach (var artifact in ArtifactsOfKind(artifacts, ArtifactKind.Query))
        {
            var query = BuildQuery(artifact.Definition, context, diagnostics, coverage);
            if (query is not null)
            {
                queries.Add(query);
                MarkLowered(artifact, coverage);
            }
        }

        var readModelArtifacts = ArtifactsOfKind(artifacts, ArtifactKind.ReadModel);
        var readModels = readModelArtifacts
            .Select(_ => BuildReadModel(_.Definition, context))
            .ToArray();
        MarkLowered(readModelArtifacts, coverage);

        var reducers = new List<ReducerSyntax>();
        foreach (var artifact in ArtifactsOfKind(artifacts, ArtifactKind.Reducer))
        {
            var reducer = BuildReducer(artifact.Definition, context, diagnostics, coverage);
            if (reducer is not null)
            {
                reducers.Add(reducer);
                MarkLowered(artifact, coverage);
            }
        }

        var specifications = context.SpecificationsFor(artifacts[0].Placement, diagnostics, coverage);

        return new SliceSyntax(
            SliceTypeFrom(kind),
            name,
            events,
            commands,
            queries,
            [],
            [],
            [],
            [],
            [],
            specifications,
            _generated,
            ReadModels: readModels,
            Reducers: reducers);
    }

    static PlacedArtifact[] ArtifactsOfKind(IReadOnlyList<PlacedArtifact> artifacts, ArtifactKind kind) =>
        [.. artifacts.Where(_ => _.Definition.Key.Kind == kind).OrderBy(_ => _.Definition.Name, StringComparer.Ordinal)];

    static void MarkLowered(IEnumerable<PlacedArtifact> artifacts, ScreenplayLoweringCoverageBuilder coverage)
    {
        foreach (var artifact in artifacts)
        {
            MarkLowered(artifact, coverage);
        }
    }

    static void MarkLowered(PlacedArtifact artifact, ScreenplayLoweringCoverageBuilder coverage)
    {
        coverage.Lowered(GenerationFactSemanticKey.Artifact(artifact.Definition));
        coverage.Lowered(GenerationFactSemanticKey.Placement(artifact.Definition.Key, artifact.Placement));
    }

    static CommandSyntax BuildCommand(
        ArtifactDefinition definition,
        LoweringContext context,
        ScreenplayLoweringCoverageBuilder coverage)
    {
        var productionRelationships = context.RelationshipsFrom(definition.Key.Subject, RelationshipKind.Produces);
        var imperativeProductionRelationships = productionRelationships
            .Where(_ => _.Key.Discriminator == "imperative")
            .ToArray();
        var hasImperativeProduction = imperativeProductionRelationships.Length > 0;
        var loweredProductionRelationships = productionRelationships
            .Where(_ => !hasImperativeProduction && _.Key.Discriminator != "imperative")
            .Select(_ => new
            {
                Relationship = _,
                Name = context.ArtifactName(_.Key.Target, ArtifactKind.Event)
            })
            .Where(_ => _.Name is not null)
            .ToArray();
        var produces = loweredProductionRelationships
            .Select(_ => _.Name!)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Select(_ => new ProducesSyntax(_, null, [], _generated))
            .ToArray();
        var loweredReadRelationships = context.RelationshipsFrom(definition.Key.Subject, RelationshipKind.Reads)
            .Select(_ => new
            {
                Relationship = _,
                Name = context.ArtifactName(_.Key.Target, ArtifactKind.ReadModel)
            })
            .Where(_ => _.Name is not null)
            .ToArray();
        var reads = loweredReadRelationships
            .Select(_ => new ReadsSyntax(_.Name!, _.Relationship.SourceMember, _generated))
            .ToArray();
        foreach (var relationship in loweredProductionRelationships.Select(_ => _.Relationship)
                     .Concat(loweredReadRelationships.Select(_ => _.Relationship)))
        {
            coverage.Lowered(GenerationFactSemanticKey.Relationship(relationship));
        }

        var handler = (produces.Length == 0 || hasImperativeProduction) && definition.File is not null
            ? new HandlerSyntax(FileFrom(definition.File), null, _generated)
            : null;
        if (handler is not null)
        {
            foreach (var relationship in imperativeProductionRelationships)
            {
                coverage.Lowered(GenerationFactSemanticKey.Relationship(relationship));
            }
        }

        return new(
            definition.Name,
            definition.Properties.Select(_ => BuildProperty(_, context)),
            null,
            [],
            produces,
            handler,
            _generated,
            Description: definition.Description,
            Reads: reads);
    }

    static QuerySyntax? BuildQuery(
        ArtifactDefinition definition,
        LoweringContext context,
        List<GenerationDiagnostic> diagnostics,
        ScreenplayLoweringCoverageBuilder coverage)
    {
        var returns = context.RelationshipsFrom(definition.Key.Subject, RelationshipKind.Returns);
        var returnType = returns.Length == 1
            ? context.ArtifactName(returns[0].Key.Target, ArtifactKind.ReadModel)
            : null;
        if (returnType is null)
        {
            var diagnostic = new GenerationDiagnostic
            {
                Code = GenerationDiagnosticCodes.IncompleteArtifact,
                Severity = GenerationDiagnosticSeverity.Warning,
                Outcome = GenerationDiagnosticOutcome.Unknown,
                Message = $"Query '{definition.Name}' was omitted because it does not return exactly one known read model",
                Subject = definition.Key.Subject
            };
            diagnostics.Add(diagnostic);
            coverage.Omitted(GenerationFactSemanticKey.Artifact(definition), diagnostic);
            return null;
        }

        var identifyingProperty = definition.Properties.FirstOrDefault(_ => _.IsIdentifier);
        var by = identifyingProperty is null
            ? null
            : new QueryParameterSyntax(identifyingProperty.Name, BuildType(identifyingProperty.Type, context), _generated);
        var filters = definition.Properties
            .Where(_ => _ != identifyingProperty)
            .Select(_ => new QueryParameterSyntax(_.Name, BuildType(_.Type, context), _generated))
            .ToArray();
        var relationship = returns[0];
        coverage.Lowered(GenerationFactSemanticKey.Relationship(relationship));

        return new(
            definition.Name,
            new TypeRefSyntax(returnType, relationship.IsCollection, relationship.IsOptional, _generated),
            by,
            filters,
            null,
            _generated,
            definition.Description,
            definition.File is null ? null : new PerformerSyntax(FileFrom(definition.File), null, _generated));
    }

    static EventSyntax BuildEvent(ArtifactDefinition definition, LoweringContext context) => new(
        definition.Name,
        definition.Properties.Select(_ => BuildProperty(_, context)),
        _generated)
    {
        File = FileFrom(definition.File)
    };

    static ReadModelSyntax BuildReadModel(ArtifactDefinition definition, LoweringContext context) => new(
        definition.Name,
        definition.Properties.Select(_ => BuildProperty(_, context)),
        _generated,
        definition.Description)
    {
        File = FileFrom(definition.File)
    };

    static ReducerSyntax? BuildReducer(
        ArtifactDefinition definition,
        LoweringContext context,
        List<GenerationDiagnostic> diagnostics,
        ScreenplayLoweringCoverageBuilder coverage)
    {
        var builds = context.RelationshipsFrom(definition.Key.Subject, RelationshipKind.Builds);
        var consumes = context.RelationshipsFrom(definition.Key.Subject, RelationshipKind.Consumes);
        var readModel = builds.Length == 1
            ? context.ArtifactName(builds[0].Key.Target, ArtifactKind.ReadModel)
            : null;
        if (readModel is null || consumes.Length == 0)
        {
            var diagnostic = new GenerationDiagnostic
            {
                Code = GenerationDiagnosticCodes.IncompleteArtifact,
                Severity = GenerationDiagnosticSeverity.Warning,
                Outcome = GenerationDiagnosticOutcome.Unknown,
                Message = $"Reducer '{definition.Name}' was omitted because it does not identify exactly one read model and at least one consumed event",
                Subject = definition.Key.Subject
            };
            diagnostics.Add(diagnostic);
            coverage.Omitted(GenerationFactSemanticKey.Artifact(definition), diagnostic);
            return null;
        }

        var loweredConsumes = consumes
            .Where(_ => context.ArtifactName(_.Key.Target, ArtifactKind.Event) is not null)
            .ToArray();
        var rules = loweredConsumes
            .Select(_ => context.ArtifactName(_.Key.Target, ArtifactKind.Event)!)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Select(_ => new ReducerRuleSyntax(_, FileFrom(definition.File), null, _generated))
            .ToArray();
        if (rules.Length == 0)
        {
            var diagnostic = new GenerationDiagnostic
            {
                Code = GenerationDiagnosticCodes.IncompleteArtifact,
                Severity = GenerationDiagnosticSeverity.Warning,
                Outcome = GenerationDiagnosticOutcome.Unknown,
                Message = $"Reducer '{definition.Name}' was omitted because none of its consumed event subjects resolve to event artifacts",
                Subject = definition.Key.Subject
            };
            diagnostics.Add(diagnostic);
            coverage.Omitted(GenerationFactSemanticKey.Artifact(definition), diagnostic);
            return null;
        }

        coverage.Lowered(GenerationFactSemanticKey.Relationship(builds[0]));
        foreach (var relationship in loweredConsumes)
        {
            coverage.Lowered(GenerationFactSemanticKey.Relationship(relationship));
        }

        return new(definition.Name, readModel, rules, _generated, definition.Description);
    }

    static PropertySyntax BuildProperty(PropertyDefinition property, LoweringContext context) => new(
        property.Name,
        BuildType(property.Type, context),
        _generated,
        property.IsIdentifier);

    static TypeRefSyntax BuildType(TypeReferenceDefinition type, LoweringContext context) => new(
        context.TypeName(type),
        type.IsCollection,
        type.IsOptional,
        _generated);

    static FileReferenceSyntax? FileFrom(string? path) =>
        path is null ? null : new FileReferenceSyntax(path.Replace('\\', '/'), _generated);

    static SliceType SliceTypeFrom(GenerationSliceKind kind) => kind switch
    {
        GenerationSliceKind.StateChange => SliceType.StateChange,
        GenerationSliceKind.StateView => SliceType.StateView,
        GenerationSliceKind.Automation => SliceType.Automation,
        GenerationSliceKind.Translate => SliceType.Translate,
        _ => throw new InvalidOperationException("Slice kinds must be validated before lowering")
    };

    sealed class LoweringContext(
        ResolvedApplicationGraph graph,
        IReadOnlyDictionary<string, string> conceptNames)
    {
        readonly IReadOnlyList<RelationshipDefinition> _relationships =
        [
            .. graph.Relationships
                .Where(_ => !_.IsConflicted &&
                            _.Key.Kind != RelationshipKind.Unknown &&
                            Enum.IsDefined(_.Key.Kind))
                .Select(_ => _.Definitions.Single())
        ];
        readonly IReadOnlyList<ArtifactDefinition> _artifacts =
        [
            .. graph.Artifacts
                .Where(_ => !_.IsConflicted)
                .Select(_ => _.Variants.Single().Definition)
        ];

        public RelationshipDefinition[] RelationshipsFrom(SubjectId source, RelationshipKind kind) =>
        [
            .. _relationships
                .Where(_ => _.Key.Source == source && _.Key.Kind == kind)
                .OrderBy(Canonical.Relationship, StringComparer.Ordinal)
        ];

        public string? ArtifactName(SubjectId subject, ArtifactKind kind) =>
            _artifacts.FirstOrDefault(_ => _.Key.Subject == subject && _.Key.Kind == kind)?.Name;

        public string TypeName(TypeReferenceDefinition type) =>
            type.Subject is not null && conceptNames.TryGetValue(type.Subject.Value, out var conceptName)
                ? conceptName
                : type.Name;

        public SpecificationSyntax[] SpecificationsFor(
            ArtifactPlacement placement,
            ICollection<GenerationDiagnostic> diagnostics,
            ScreenplayLoweringCoverageBuilder coverage) =>
            SpecificationSyntaxLowerer.Lower(
                graph,
                placement,
                artifact => ArtifactName(artifact.Subject, artifact.Kind),
                diagnostics,
                coverage);
    }

    sealed record PlacedArtifact(
        ArtifactDefinition Definition,
        ArtifactPlacement Placement,
        SourceRange? Source);

    sealed class FeatureNode(string name)
    {
        readonly Dictionary<string, List<PlacedArtifact>> _slices = new(StringComparer.Ordinal);

        public string Name { get; } = name;

        public SortedDictionary<string, FeatureNode> Children { get; } = new(StringComparer.Ordinal);

        public FeatureNode Feature(string featureName)
        {
            if (!Children.TryGetValue(featureName, out var feature))
            {
                feature = new(featureName);
                Children.Add(featureName, feature);
            }

            return feature;
        }

        public void Add(PlacedArtifact artifact)
        {
            if (!_slices.TryGetValue(artifact.Placement.Slice, out var artifacts))
            {
                artifacts = [];
                _slices.Add(artifact.Placement.Slice, artifacts);
            }

            artifacts.Add(artifact);
        }

        public FeatureSyntax Build(
            LoweringContext context,
            List<GenerationDiagnostic> diagnostics,
            ScreenplayLoweringCoverageBuilder coverage)
        {
            var slices = _slices
                .OrderBy(_ => _.Key, StringComparer.Ordinal)
                .Select(_ => BuildSliceGroup(_.Key, _.Value, context, diagnostics, coverage))
                .Where(_ => _ is not null)
                .Cast<SliceSyntax>()
                .ToArray();

            return new(
                Name,
                [.. Children.Values.Select(_ => _.Build(context, diagnostics, coverage))],
                slices,
                _generated);
        }

        static SliceSyntax? BuildSliceGroup(
            string name,
            IReadOnlyList<PlacedArtifact> artifacts,
            LoweringContext context,
            List<GenerationDiagnostic> diagnostics,
            ScreenplayLoweringCoverageBuilder coverage)
        {
            var kinds = artifacts
                .Select(_ => _.Placement.SliceKind)
                .Distinct()
                .Order()
                .ToArray();
            var unsupportedKind = kinds.FirstOrDefault(_ => _ == GenerationSliceKind.Unknown || !Enum.IsDefined(_));
            if (unsupportedKind == GenerationSliceKind.Unknown || !Enum.IsDefined(unsupportedKind))
            {
                var firstArtifact = artifacts
                    .OrderBy(_ => Canonical.Artifact(_.Definition), StringComparer.Ordinal)
                    .First();
                var diagnostic = new GenerationDiagnostic
                {
                    Code = GenerationDiagnosticCodes.UnsupportedSliceKind,
                    Severity = GenerationDiagnosticSeverity.Warning,
                    Outcome = OutcomeFor(unsupportedKind, GenerationSliceKind.Unknown),
                    Message = $"Slice '{name}' uses unknown or undefined GenerationSliceKind value '{(int)unsupportedKind}' and was omitted",
                    Source = firstArtifact.Source,
                    Subject = firstArtifact.Definition.Key.Subject
                };
                diagnostics.Add(diagnostic);
                foreach (var artifact in artifacts)
                {
                    coverage.Omitted(GenerationFactSemanticKey.Artifact(artifact.Definition), diagnostic);
                    coverage.Omitted(GenerationFactSemanticKey.Placement(artifact.Definition.Key, artifact.Placement), diagnostic);
                }
                return null;
            }

            if (kinds.Length > 1)
            {
                var firstArtifact = artifacts
                    .OrderBy(_ => Canonical.Artifact(_.Definition), StringComparer.Ordinal)
                    .First();
                var diagnostic = new GenerationDiagnostic
                {
                    Code = GenerationDiagnosticCodes.ConflictingSliceKind,
                    Severity = GenerationDiagnosticSeverity.Error,
                    Outcome = GenerationDiagnosticOutcome.Conflict,
                    Message = $"Slice '{name}' was assigned incompatible kinds: {string.Join(", ", kinds)}",
                    Source = firstArtifact.Source,
                    Subject = firstArtifact.Definition.Key.Subject
                };
                diagnostics.Add(diagnostic);
                foreach (var artifact in artifacts)
                {
                    coverage.Omitted(GenerationFactSemanticKey.Artifact(artifact.Definition), diagnostic);
                    coverage.Conflicted(GenerationFactSemanticKey.Placement(artifact.Definition.Key, artifact.Placement), diagnostic);
                }
                return null;
            }

            return BuildSlice(name, kinds[0], artifacts, context, diagnostics, coverage);
        }
    }
}
