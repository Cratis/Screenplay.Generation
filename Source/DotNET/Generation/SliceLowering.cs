// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using static Cratis.Screenplay.Generation.LoweringSupport;

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Lowers placed artifacts into modules, features and slices.
/// </summary>
internal static class SliceLowering
{
    public static ModuleSyntax BuildModule(string name, IEnumerable<PlacedArtifact> artifacts, LoweringContext context)
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

        return new(name, [], [.. root.Children.Values.Select(_ => _.Build(context))], Generated);
    }

    static SliceSyntax BuildSlice(
        string name,
        GenerationSliceKind kind,
        IReadOnlyList<PlacedArtifact> artifacts,
        LoweringContext context) => new(
            SliceTypeFrom(kind),
            name,
            LowerEach(artifacts, ArtifactKind.Event, _ => ArtifactLowering.BuildEvent(_, context), context),
            LowerEach(artifacts, ArtifactKind.Command, _ => ArtifactLowering.BuildCommand(_, context), context),
            LowerEach(artifacts, ArtifactKind.Query, _ => ArtifactLowering.BuildQuery(_, context), context),
            [],
            [],
            [],
            [],
            [],
            context.SpecificationsFor(artifacts[0].Placement),
            Generated,
            ReadModels: LowerEach(artifacts, ArtifactKind.ReadModel, _ => ArtifactLowering.BuildReadModel(_, context), context),
            Reducers: LowerEach(artifacts, ArtifactKind.Reducer, _ => ArtifactLowering.BuildReducer(_, context), context));

    static T[] LowerEach<T>(
        IReadOnlyList<PlacedArtifact> artifacts,
        ArtifactKind kind,
        Func<ArtifactDefinition, T?> build,
        LoweringContext context)
        where T : class
    {
        var lowered = new List<T>();
        foreach (var artifact in artifacts
                     .Where(_ => _.Definition.Key.Kind == kind)
                     .OrderBy(_ => _.Definition.Name, StringComparer.Ordinal))
        {
            if (build(artifact.Definition) is not { } syntax)
            {
                continue;
            }

            lowered.Add(syntax);
            context.Sink.Coverage.Lowered(GenerationFactSemanticKey.Artifact(artifact.Definition));
            context.Sink.Coverage.Lowered(GenerationFactSemanticKey.Placement(artifact.Definition.Key, artifact.Placement));
        }

        return [.. lowered];
    }

    static SliceSyntax? BuildSliceGroup(string name, IReadOnlyList<PlacedArtifact> artifacts, LoweringContext context)
    {
        var kinds = artifacts
            .Select(_ => _.Placement.SliceKind)
            .Distinct()
            .Order()
            .ToArray();
        if (KindProblem(name, kinds) is not { } problem)
        {
            return BuildSlice(name, kinds[0], artifacts, context);
        }

        OmitSlice(SliceDiagnostic(problem, artifacts), artifacts, context.Sink);
        return null;
    }

    static GenerationDiagnostic? KindProblem(string name, GenerationSliceKind[] kinds)
    {
        if (kinds.Where(IsUnsupportedKind).Select(_ => (GenerationSliceKind?)_).FirstOrDefault() is { } unsupportedKind)
        {
            return Warning(
                GenerationDiagnosticCodes.UnsupportedSliceKind,
                OutcomeFor(unsupportedKind, GenerationSliceKind.Unknown),
                $"Slice '{name}' uses unknown or undefined GenerationSliceKind value '{(int)unsupportedKind}' and was omitted");
        }

        return kinds.Length > 1
            ? Warning(
                GenerationDiagnosticCodes.ConflictingSliceKind,
                GenerationDiagnosticOutcome.Conflict,
                $"Slice '{name}' was assigned incompatible kinds: {string.Join(", ", kinds)}") with
            {
                Severity = GenerationDiagnosticSeverity.Error
            }
            : null;
    }

    static bool IsUnsupportedKind(GenerationSliceKind kind) => kind == GenerationSliceKind.Unknown || !Enum.IsDefined(kind);

    static void OmitSlice(GenerationDiagnostic diagnostic, IReadOnlyList<PlacedArtifact> artifacts, LoweringSink sink)
    {
        var isConflict = diagnostic.Outcome == GenerationDiagnosticOutcome.Conflict;
        sink.Diagnostics.Add(diagnostic);
        foreach (var artifact in artifacts)
        {
            var placement = GenerationFactSemanticKey.Placement(artifact.Definition.Key, artifact.Placement);
            sink.Coverage.Omitted(GenerationFactSemanticKey.Artifact(artifact.Definition), diagnostic);
            if (isConflict)
            {
                sink.Coverage.Conflicted(placement, diagnostic);
            }
            else
            {
                sink.Coverage.Omitted(placement, diagnostic);
            }
        }
    }

    static GenerationDiagnostic SliceDiagnostic(GenerationDiagnostic diagnostic, IReadOnlyList<PlacedArtifact> artifacts)
    {
        var firstArtifact = artifacts
            .OrderBy(_ => Canonical.Artifact(_.Definition), StringComparer.Ordinal)
            .First();
        return diagnostic with { Source = firstArtifact.Source, Subject = firstArtifact.Definition.Key.Subject };
    }

    static SliceType SliceTypeFrom(GenerationSliceKind kind) => kind switch
    {
        GenerationSliceKind.StateChange => SliceType.StateChange,
        GenerationSliceKind.StateView => SliceType.StateView,
        GenerationSliceKind.Automation => SliceType.Automation,
        GenerationSliceKind.Translate => SliceType.Translate,
        _ => throw new InvalidOperationException("Slice kinds must be validated before lowering")
    };

    sealed class FeatureNode(string name)
    {
        readonly Dictionary<string, List<PlacedArtifact>> _slices = [with(StringComparer.Ordinal)];

        public string Name { get; } = name;

        public SortedDictionary<string, FeatureNode> Children { get; } = [with(StringComparer.Ordinal)];

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

        public FeatureSyntax Build(LoweringContext context)
        {
            var slices = _slices
                .OrderBy(_ => _.Key, StringComparer.Ordinal)
                .Select(_ => BuildSliceGroup(_.Key, _.Value, context))
                .Where(_ => _ is not null)
                .Cast<SliceSyntax>()
                .ToArray();

            return new(
                Name,
                [.. Children.Values.Select(_ => _.Build(context))],
                slices,
                Generated);
        }
    }
}
