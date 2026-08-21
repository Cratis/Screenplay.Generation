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
        var context = new LoweringContext(graph);
        var placements = graph.Placements
            .Where(_ => !_.IsConflicted)
            .ToDictionary(_ => Canonical.ArtifactKey(_.Artifact), _ => _.EffectiveVariants.Single().Placement, StringComparer.Ordinal);
        var artifacts = graph.Artifacts
            .Where(_ => !_.IsConflicted)
            .Select(_ => _.Variants.Single().Definition)
            .Where(_ => placements.ContainsKey(Canonical.ArtifactKey(_.Key)))
            .Select(_ => new PlacedArtifact(_, placements[Canonical.ArtifactKey(_.Key)]))
            .OrderBy(_ => Canonical.Artifact(_.Definition), StringComparer.Ordinal)
            .ToArray();

        foreach (var unsupported in artifacts.Where(_ => !CanLower(_.Definition.Key.Kind)))
        {
            diagnostics.Add(new GenerationDiagnostic
            {
                Code = GenerationDiagnosticCodes.UnsupportedArtifact,
                Severity = GenerationDiagnosticSeverity.Warning,
                Message = $"The recognized {unsupported.Definition.Key.Kind} artifact '{unsupported.Definition.Name}' cannot yet be represented by the Screenplay lowerer and was omitted",
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
                [],
                [],
                modules,
                _generated,
                new DomainSyntax(domain, _generated)),
            Diagnostics = [.. diagnostics.OrderBy(Canonical.Diagnostic, StringComparer.Ordinal)]
        };
    }

    static bool CanLower(ArtifactKind kind) => kind is ArtifactKind.Command or ArtifactKind.Event or ArtifactKind.Query or ArtifactKind.ReadModel or ArtifactKind.Reducer;

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

        return new(name, [], root.Children.Values.Select(_ => _.Build(context, diagnostics)), _generated);
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
            .Select(BuildEvent)
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
            .Select(BuildReadModel)
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
            definition.Properties.Select(BuildProperty),
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
                Message = $"Query '{definition.Name}' was omitted because it does not return exactly one known read model",
                Subject = definition.Key.Subject
            });
            return null;
        }

        var identifyingProperty = definition.Properties.FirstOrDefault(_ => _.IsIdentifier);
        var by = identifyingProperty is null
            ? null
            : new QueryParameterSyntax(identifyingProperty.Name, BuildType(identifyingProperty.Type), _generated);
        var filters = definition.Properties
            .Where(_ => _ != identifyingProperty)
            .Select(_ => new QueryParameterSyntax(_.Name, BuildType(_.Type), _generated))
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

    static EventSyntax BuildEvent(ArtifactDefinition definition) => new(
        definition.Name,
        definition.Properties.Select(BuildProperty),
        _generated)
    {
        File = FileFrom(definition.File)
    };

    static ReadModelSyntax BuildReadModel(ArtifactDefinition definition) => new(
        definition.Name,
        definition.Properties.Select(BuildProperty),
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
                Message = $"Reducer '{definition.Name}' was omitted because none of its consumed event subjects resolve to event artifacts",
                Subject = definition.Key.Subject
            });
            return null;
        }

        return new(definition.Name, readModel, rules, _generated, definition.Description);
    }

    static PropertySyntax BuildProperty(PropertyDefinition property) => new(
        property.Name,
        BuildType(property.Type),
        _generated,
        property.IsIdentifier);

    static TypeRefSyntax BuildType(TypeReferenceDefinition type) => new(
        type.Name,
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
        _ => SliceType.StateChange
    };

    sealed class LoweringContext(ResolvedApplicationGraph graph)
    {
        readonly IReadOnlyList<RelationshipDefinition> _relationships =
        [
            .. graph.Relationships
                .Where(_ => !_.IsConflicted)
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
    }

    sealed record PlacedArtifact(ArtifactDefinition Definition, ArtifactPlacement Placement);

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
                .ToArray();

            return new(
                Name,
                Children.Values.Select(_ => _.Build(context, diagnostics)),
                slices,
                _generated);
        }

        static SliceSyntax BuildSliceGroup(
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
            if (kinds.Length > 1)
            {
                diagnostics.Add(new GenerationDiagnostic
                {
                    Code = GenerationDiagnosticCodes.ConflictingSliceKind,
                    Severity = GenerationDiagnosticSeverity.Error,
                    Message = $"Slice '{name}' was assigned incompatible kinds: {string.Join(", ", kinds)}"
                });
            }

            return BuildSlice(name, kinds[0], artifacts, context, diagnostics);
        }
    }
}
