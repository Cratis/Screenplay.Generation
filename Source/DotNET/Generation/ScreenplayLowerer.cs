// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

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
        ReportUnsupportedRelationships(graph, diagnostics);
        var placements = graph.Placements
            .Where(_ => !_.IsConflicted)
            .ToDictionary(_ => Canonical.ArtifactKey(_.Artifact), _ => _.EffectiveVariants.Single().Placement, StringComparer.Ordinal);
        var definitions = graph.Artifacts
            .Where(_ => !_.IsConflicted)
            .Select(_ => _.Variants.Single().Definition)
            .ToArray();
        var (concepts, conceptNames) = BuildConcepts(graph, definitions, diagnostics);
        var artifactsWithMissingConceptReferences = ReportMissingConceptReferences(definitions, conceptNames, diagnostics);
        var context = new LoweringContext(graph, conceptNames);
        foreach (var unplaced in definitions.Where(_ =>
                     _.Key.Kind != ArtifactKind.Concept &&
                     !placements.ContainsKey(Canonical.ArtifactKey(_.Key))))
        {
            diagnostics.Add(UnplacedArtifactDiagnostic(unplaced, SourceForArtifact(graph, unplaced.Key)));
        }

        var artifacts = definitions
            .Where(_ => placements.ContainsKey(Canonical.ArtifactKey(_.Key)) &&
                        !artifactsWithMissingConceptReferences.Contains(Canonical.ArtifactKey(_.Key)))
            .Select(_ => new PlacedArtifact(
                _,
                placements[Canonical.ArtifactKey(_.Key)],
                SourceForPlacement(graph, _.Key)))
            .OrderBy(_ => Canonical.Artifact(_.Definition), StringComparer.Ordinal)
            .ToArray();

        foreach (var unsupported in artifacts.Where(_ =>
                     _.Definition.Key.Kind != ArtifactKind.Concept &&
                     !CanLower(_.Definition.Key.Kind)))
        {
            var isKnownKind = IsKnownArtifactKind(unsupported.Definition.Key.Kind);
            diagnostics.Add(new GenerationDiagnostic
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
            });
        }

        ModuleSyntax[] modules =
        [
            .. artifacts
                .Where(_ => CanLower(_.Definition.Key.Kind))
                .GroupBy(_ => _.Placement.Module, StringComparer.Ordinal)
                .OrderBy(_ => _.Key, StringComparer.Ordinal)
                .Select(_ => BuildModule(_.Key, _, context, diagnostics))
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
            Diagnostics = [.. diagnostics.OrderBy(Canonical.Diagnostic, StringComparer.Ordinal)]
        };
    }

    static (ConceptSyntax[] Concepts, IReadOnlyDictionary<string, string> Names) BuildConcepts(
        ResolvedApplicationGraph graph,
        IReadOnlyList<ArtifactDefinition> definitions,
        List<GenerationDiagnostic> diagnostics)
    {
        var conceptDefinitions = definitions.Where(_ => _.Key.Kind == ArtifactKind.Concept).ToArray();
        var conflictingNames = conceptDefinitions
            .GroupBy(_ => _.Name, StringComparer.Ordinal)
            .Where(_ => _.Select(definition => definition.Key.Subject.Value).Distinct(StringComparer.Ordinal).Count() > 1)
            .ToDictionary(_ => _.Key, _ => _.ToArray(), StringComparer.Ordinal);
        foreach (var conflict in conflictingNames.OrderBy(_ => _.Key, StringComparer.Ordinal))
        {
            diagnostics.Add(new GenerationDiagnostic
            {
                Code = GenerationDiagnosticCodes.ConflictingConceptName,
                Severity = GenerationDiagnosticSeverity.Error,
                Outcome = GenerationDiagnosticOutcome.Conflict,
                Message = $"Concept name '{conflict.Key}' is required by {conflict.Value.Length} distinct source subjects",
                Subject = conflict.Value.OrderBy(_ => _.Key.Subject.Value, StringComparer.Ordinal).First().Key.Subject
            });
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
                diagnostics.Add(new GenerationDiagnostic
                {
                    Code = GenerationDiagnosticCodes.MissingConceptRepresentation,
                    Severity = GenerationDiagnosticSeverity.Warning,
                    Outcome = GenerationDiagnosticOutcome.Unknown,
                    Message = $"Concept '{definition.Name}' has no proven representation and was omitted",
                    Subject = definition.Key.Subject
                });
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
                diagnostics.Add(new GenerationDiagnostic
                {
                    Code = GenerationDiagnosticCodes.UnsupportedConceptRepresentationKind,
                    Severity = GenerationDiagnosticSeverity.Warning,
                    Outcome = OutcomeFor(representation.Kind, ConceptRepresentationKind.Unknown),
                    Message = $"Concept '{definition.Name}' uses unknown or undefined ConceptRepresentationKind value '{(int)representation.Kind}' and was omitted",
                    Source = FirstSource(representationVariant.Evidence),
                    Subject = definition.Key.Subject
                });
                continue;
            }

            if (representation.Primitive is { } primitive &&
                (primitive == GenerationPrimitiveKind.Unknown || !Enum.IsDefined(primitive)))
            {
                diagnostics.Add(new GenerationDiagnostic
                {
                    Code = GenerationDiagnosticCodes.UnsupportedPrimitiveKind,
                    Severity = GenerationDiagnosticSeverity.Warning,
                    Outcome = OutcomeFor(primitive, GenerationPrimitiveKind.Unknown),
                    Message = $"Concept '{definition.Name}' uses unknown or undefined GenerationPrimitiveKind value '{(int)primitive}' and was omitted",
                    Source = FirstSource(representationVariant.Evidence),
                    Subject = definition.Key.Subject
                });
                continue;
            }

            if (TypeOf(representation) is not { } type)
            {
                diagnostics.Add(new GenerationDiagnostic
                {
                    Code = GenerationDiagnosticCodes.UnsupportedConceptRepresentation,
                    Severity = GenerationDiagnosticSeverity.Warning,
                    Outcome = GenerationDiagnosticOutcome.Unsupported,
                    Message = $"Concept '{definition.Name}' has an invalid or unsupported {representation.Kind} representation and was omitted",
                    Source = FirstSource(representationVariant.Evidence),
                    Subject = definition.Key.Subject
                });
                continue;
            }

            IReadOnlyList<string> values = representation.Kind == ConceptRepresentationKind.Enumeration
                ? [.. representation.EnumerationValues.Select(EnumValue)]
                : [];
            if (values.Any(_ => !IsEnumValue(_)) || values.Distinct(StringComparer.Ordinal).Count() != values.Count)
            {
                diagnostics.Add(new GenerationDiagnostic
                {
                    Code = GenerationDiagnosticCodes.UnsupportedConceptRepresentation,
                    Severity = GenerationDiagnosticSeverity.Warning,
                    Outcome = GenerationDiagnosticOutcome.Unsupported,
                    Message = $"Concept '{definition.Name}' has empty or duplicate enumeration values after Screenplay naming and was omitted",
                    Source = FirstSource(representationVariant.Evidence),
                    Subject = definition.Key.Subject
                });
                continue;
            }

            var attributes = BuildConceptAttributes(graph, definition, diagnostics);
            var validations = BuildConceptValidations(graph, definition, diagnostics);
            concepts.Add(new ConceptSyntax(definition.Name, type, attributes, values, _generated, validations)
            {
                File = FileFrom(definition.File)
            });
            names[definition.Key.Subject.Value] = definition.Name;
        }

        return ([.. concepts], names);
    }

    static ConceptAttributeSyntax[] BuildConceptAttributes(
        ResolvedApplicationGraph graph,
        ArtifactDefinition concept,
        List<GenerationDiagnostic> diagnostics)
    {
        var attributes = new List<ConceptAttributeSyntax>();
        foreach (var resolved in graph.ConceptAttributes
                     .Where(_ => _.Concept == concept.Key.Subject && !_.IsConflicted)
                     .OrderBy(_ => _.Name, StringComparer.Ordinal))
        {
            var definition = resolved.Variants.Single().Definition;
            if (definition.Kind != ConceptAttributeKind.Named)
            {
                diagnostics.Add(new GenerationDiagnostic
                {
                    Code = GenerationDiagnosticCodes.UnsupportedConceptAttributeKind,
                    Severity = GenerationDiagnosticSeverity.Warning,
                    Outcome = OutcomeFor(definition.Kind, ConceptAttributeKind.Unknown),
                    Message = $"Concept '{concept.Name}' has unknown or undefined ConceptAttributeKind value '{(int)definition.Kind}', which was omitted",
                    Source = FirstSource(resolved.Variants.Single().Evidence),
                    Subject = concept.Key.Subject
                });
                continue;
            }

            if (!IsIdentifier(definition.Name))
            {
                diagnostics.Add(new GenerationDiagnostic
                {
                    Code = GenerationDiagnosticCodes.UnsupportedConceptAttribute,
                    Severity = GenerationDiagnosticSeverity.Warning,
                    Outcome = GenerationDiagnosticOutcome.Unsupported,
                    Message = $"Concept '{concept.Name}' has invalid attribute name '{definition.Name}', which was omitted",
                    Source = FirstSource(resolved.Variants.Single().Evidence),
                    Subject = concept.Key.Subject
                });
                continue;
            }

            attributes.Add(new ConceptAttributeSyntax(definition.Name, _generated, definition.Reason));
        }

        return [.. attributes];
    }

    static ValidateSyntax[] BuildConceptValidations(
        ResolvedApplicationGraph graph,
        ArtifactDefinition concept,
        List<GenerationDiagnostic> diagnostics)
    {
        var rules = new List<ValidationRuleSyntax>();
        foreach (var resolved in graph.ConceptValidationRules
                     .Where(_ => _.Concept == concept.Key.Subject)
                     .OrderBy(_ => _.RuleIdentity, StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(resolved.RuleIdentity))
            {
                ReportUnsupportedValidation(
                    concept,
                    resolved.RuleIdentity,
                    "has no rule identity",
                    FirstSource(resolved.Variants.SelectMany(_ => _.Evidence)),
                    diagnostics);
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
                diagnostics.Add(new GenerationDiagnostic
                {
                    Code = GenerationDiagnosticCodes.UnsupportedConceptValidationRuleKind,
                    Severity = GenerationDiagnosticSeverity.Warning,
                    Outcome = OutcomeFor(definition.Kind, ConceptValidationRuleKind.Unknown),
                    Message = $"Concept '{concept.Name}' validation rule '{definition.RuleIdentity}' uses unknown or undefined ConceptValidationRuleKind value '{(int)definition.Kind}' and was omitted",
                    Source = FirstSource(validationVariant.Evidence),
                    Subject = concept.Key.Subject
                });
                continue;
            }

            if (definition.Kind != ConceptValidationRuleKind.NamedPredicate ||
                definition.Predicate is not { } predicate ||
                !IsRuleIdentifier(predicate) ||
                !IsValidImplementationFile(definition.ImplementationFile))
            {
                ReportUnsupportedValidation(
                    concept,
                    definition.RuleIdentity,
                    "has invalid or missing required data",
                    FirstSource(validationVariant.Evidence),
                    diagnostics);
                continue;
            }

            rules.Add(new ValidationRuleSyntax(
                ValidationRuleSyntax.ConceptValue,
                ValidationRuleKind.Rule,
                new PathExpressionSyntax(predicate, _generated),
                definition.Message,
                _generated,
                FileFrom(definition.ImplementationFile)));
        }

        return rules.Count == 0
            ? []
            : [new DeclarativeValidateSyntax(rules, _generated)];
    }

    static void ReportUnsupportedValidation(
        ArtifactDefinition concept,
        string? ruleIdentity,
        string reason,
        SourceRange? source,
        List<GenerationDiagnostic> diagnostics) =>
        diagnostics.Add(new GenerationDiagnostic
        {
            Code = GenerationDiagnosticCodes.UnsupportedConceptValidationRule,
            Severity = GenerationDiagnosticSeverity.Warning,
            Outcome = GenerationDiagnosticOutcome.Unsupported,
            Message = $"Concept '{concept.Name}' validation rule '{ruleIdentity ?? string.Empty}' {reason} and was omitted",
            Source = source,
            Subject = concept.Key.Subject
        });

    static HashSet<string> ReportMissingConceptReferences(
        IEnumerable<ArtifactDefinition> definitions,
        IReadOnlyDictionary<string, string> conceptNames,
        List<GenerationDiagnostic> diagnostics)
    {
        var missing = definitions
            .SelectMany(definition => definition.Properties
                .Where(property => property.Type.Subject is not null && !conceptNames.ContainsKey(property.Type.Subject.Value))
                .Select(property => new { Artifact = definition.Key, property.Type }))
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

        return [.. missing.Select(_ => Canonical.ArtifactKey(_.Artifact))];
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
            .Where(_ => Canonical.ArtifactKey(_.Key) == Canonical.ArtifactKey(key))
            .SelectMany(_ => _.Variants)
            .SelectMany(_ => _.Evidence));

    static SourceRange? SourceForPlacement(ResolvedApplicationGraph graph, ArtifactKey key) =>
        FirstSource(graph.Placements
            .Where(_ => Canonical.ArtifactKey(_.Artifact) == Canonical.ArtifactKey(key))
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
        List<GenerationDiagnostic> diagnostics)
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

        return new(name, [], [.. root.Children.Values.Select(_ => _.Build(context, diagnostics))], _generated);
    }

    static SliceSyntax BuildSlice(
        string name,
        GenerationSliceKind kind,
        IReadOnlyList<PlacedArtifact> artifacts,
        LoweringContext context,
        List<GenerationDiagnostic> diagnostics)
    {
        var commands = artifacts
            .Select(_ => _.Definition)
            .Where(_ => _.Key.Kind == ArtifactKind.Command)
            .OrderBy(_ => _.Name, StringComparer.Ordinal)
            .Select(_ => BuildCommand(_, context))
            .ToArray();
        var events = artifacts
            .Select(_ => _.Definition)
            .Where(_ => _.Key.Kind == ArtifactKind.Event)
            .OrderBy(_ => _.Name, StringComparer.Ordinal)
            .Select(_ => BuildEvent(_, context))
            .ToArray();
        var queries = artifacts
            .Select(_ => _.Definition)
            .Where(_ => _.Key.Kind == ArtifactKind.Query)
            .OrderBy(_ => _.Name, StringComparer.Ordinal)
            .Select(_ => BuildQuery(_, context, diagnostics))
            .Where(_ => _ is not null)
            .Cast<QuerySyntax>()
            .ToArray();
        var readModels = artifacts
            .Select(_ => _.Definition)
            .Where(_ => _.Key.Kind == ArtifactKind.ReadModel)
            .OrderBy(_ => _.Name, StringComparer.Ordinal)
            .Select(_ => BuildReadModel(_, context))
            .ToArray();
        var reducers = artifacts
            .Select(_ => _.Definition)
            .Where(_ => _.Key.Kind == ArtifactKind.Reducer)
            .OrderBy(_ => _.Name, StringComparer.Ordinal)
            .Select(_ => BuildReducer(_, context, diagnostics))
            .Where(_ => _ is not null)
            .Cast<ReducerSyntax>()
            .ToArray();

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
            [],
            _generated,
            ReadModels: readModels,
            Reducers: reducers);
    }

    static CommandSyntax BuildCommand(ArtifactDefinition definition, LoweringContext context)
    {
        var productionRelationships = context.RelationshipsFrom(definition.Key.Subject, RelationshipKind.Produces);
        var hasImperativeProduction = productionRelationships.Any(_ => _.Key.Discriminator == "imperative");
        var produces = productionRelationships
            .Where(_ => !hasImperativeProduction && _.Key.Discriminator != "imperative")
            .Select(_ => context.ArtifactName(_.Key.Target, ArtifactKind.Event))
            .Where(_ => _ is not null)
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Select(_ => new ProducesSyntax(_, null, [], _generated))
            .ToArray();
        var reads = context.RelationshipsFrom(definition.Key.Subject, RelationshipKind.Reads)
            .Select(_ => new
            {
                Name = context.ArtifactName(_.Key.Target, ArtifactKind.ReadModel),
                By = _.SourceMember
            })
            .Where(_ => _.Name is not null)
            .Select(_ => new ReadsSyntax(_.Name!, _.By, _generated))
            .ToArray();
        var handler = (produces.Length == 0 || hasImperativeProduction) && definition.File is not null
            ? new HandlerSyntax(FileFrom(definition.File), null, _generated)
            : null;

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
        List<GenerationDiagnostic> diagnostics)
    {
        var returns = context.RelationshipsFrom(definition.Key.Subject, RelationshipKind.Returns);
        var returnType = returns.Length == 1
            ? context.ArtifactName(returns[0].Key.Target, ArtifactKind.ReadModel)
            : null;
        if (returnType is null)
        {
            diagnostics.Add(new GenerationDiagnostic
            {
                Code = GenerationDiagnosticCodes.IncompleteArtifact,
                Severity = GenerationDiagnosticSeverity.Warning,
                Outcome = GenerationDiagnosticOutcome.Unknown,
                Message = $"Query '{definition.Name}' was omitted because it does not return exactly one known read model",
                Subject = definition.Key.Subject
            });
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
        List<GenerationDiagnostic> diagnostics)
    {
        var builds = context.RelationshipsFrom(definition.Key.Subject, RelationshipKind.Builds);
        var consumes = context.RelationshipsFrom(definition.Key.Subject, RelationshipKind.Consumes);
        var readModel = builds.Length == 1
            ? context.ArtifactName(builds[0].Key.Target, ArtifactKind.ReadModel)
            : null;
        if (readModel is null || consumes.Length == 0)
        {
            diagnostics.Add(new GenerationDiagnostic
            {
                Code = GenerationDiagnosticCodes.IncompleteArtifact,
                Severity = GenerationDiagnosticSeverity.Warning,
                Outcome = GenerationDiagnosticOutcome.Unknown,
                Message = $"Reducer '{definition.Name}' was omitted because it does not identify exactly one read model and at least one consumed event",
                Subject = definition.Key.Subject
            });
            return null;
        }

        var rules = consumes
            .Select(_ => context.ArtifactName(_.Key.Target, ArtifactKind.Event))
            .Where(_ => _ is not null)
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Select(_ => new ReducerRuleSyntax(_, FileFrom(definition.File), null, _generated))
            .ToArray();
        if (rules.Length == 0)
        {
            diagnostics.Add(new GenerationDiagnostic
            {
                Code = GenerationDiagnosticCodes.IncompleteArtifact,
                Severity = GenerationDiagnosticSeverity.Warning,
                Outcome = GenerationDiagnosticOutcome.Unknown,
                Message = $"Reducer '{definition.Name}' was omitted because none of its consumed event subjects resolve to event artifacts",
                Subject = definition.Key.Subject
            });
            return null;
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

        public FeatureSyntax Build(LoweringContext context, List<GenerationDiagnostic> diagnostics)
        {
            var slices = _slices
                .OrderBy(_ => _.Key, StringComparer.Ordinal)
                .Select(_ => BuildSliceGroup(_.Key, _.Value, context, diagnostics))
                .Where(_ => _ is not null)
                .Cast<SliceSyntax>()
                .ToArray();

            return new(
                Name,
                [.. Children.Values.Select(_ => _.Build(context, diagnostics))],
                slices,
                _generated);
        }

        static SliceSyntax? BuildSliceGroup(
            string name,
            IReadOnlyList<PlacedArtifact> artifacts,
            LoweringContext context,
            List<GenerationDiagnostic> diagnostics)
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
                diagnostics.Add(new GenerationDiagnostic
                {
                    Code = GenerationDiagnosticCodes.UnsupportedSliceKind,
                    Severity = GenerationDiagnosticSeverity.Warning,
                    Outcome = OutcomeFor(unsupportedKind, GenerationSliceKind.Unknown),
                    Message = $"Slice '{name}' uses unknown or undefined GenerationSliceKind value '{(int)unsupportedKind}' and was omitted",
                    Source = firstArtifact.Source,
                    Subject = firstArtifact.Definition.Key.Subject
                });
                return null;
            }

            if (kinds.Length > 1)
            {
                var firstArtifact = artifacts
                    .OrderBy(_ => Canonical.Artifact(_.Definition), StringComparer.Ordinal)
                    .First();
                diagnostics.Add(new GenerationDiagnostic
                {
                    Code = GenerationDiagnosticCodes.ConflictingSliceKind,
                    Severity = GenerationDiagnosticSeverity.Error,
                    Outcome = GenerationDiagnosticOutcome.Conflict,
                    Message = $"Slice '{name}' was assigned incompatible kinds: {string.Join(", ", kinds)}",
                    Source = firstArtifact.Source,
                    Subject = firstArtifact.Definition.Key.Subject
                });
                return null;
            }

            return BuildSlice(name, kinds[0], artifacts, context, diagnostics);
        }
    }
}
