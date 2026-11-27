// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>Validates source-neutral projection shapes, independently of fact ownership and evidence authority.</summary>
public static class TypeUseCompatibilityProjectionValidation
{
    /// <summary>Checks that supplemental node evidence is complete and agrees with the exact terminal.</summary>
    /// <param name="type">The exact observation.</param>
    /// <returns>Whether its shape, identity and supplied node evidence are valid.</returns>
    public static bool ValidObservation(TypeUseDefinition? type)
    {
        if (type is null)
        {
            return false;
        }

        return ValidHeader(type) && CompleteNodes(type);
    }

    /// <summary>Checks a versioned printable view against the exact source-node evidence.</summary>
    /// <param name="projection">The explicit projection.</param>
    /// <param name="observation">The original exact observation.</param>
    /// <returns>Whether the view has the supported profile, stopping node, flags and identity.</returns>
    public static bool Valid(TypeUseCompatibilityProjectionDefinition? projection, TypeUseDefinition? observation) =>
        SupportedProfile(projection) && ValidPair(projection!.Type, observation) && MatchesSelection(projection, observation!);

    /// <summary>Checks whether a member type use carries jagged-array view data: a request, collection origins, or both.</summary>
    /// <param name="definition">The member type use.</param>
    /// <returns>Whether any view data is present.</returns>
    public static bool HasLegacyJaggedArrayData(ArtifactMemberTypeUseDefinition? definition) =>
        definition is { UseLegacyJaggedArrayReference: true } or { CollectionOrigins: not null };

    /// <summary>Checks that any collection origins and any jagged-array view request on a member type use are exact and consistent.</summary>
    /// <param name="definition">The member type use.</param>
    /// <returns>Whether the data is absent, or complete, aligned and, for a request, proves an array of arrays on a compact observation.</returns>
    public static bool ValidLegacyJaggedArrayData(ArtifactMemberTypeUseDefinition? definition)
    {
        if (definition is null)
        {
            return false;
        }

        if (!HasLegacyJaggedArrayData(definition))
        {
            return true;
        }

        return ValidObservation(definition.Type) && definition.CollectionOrigins is { } origins && AlignedOrigins(definition.Type, origins) &&
            (!definition.UseLegacyJaggedArrayReference || ArrayOfArrays(definition.Type, origins));
    }

    static bool AlignedOrigins(TypeUseDefinition type, IReadOnlyList<TypeUseCollectionOriginKind> origins) =>
        origins.Count == type.Shape.Count(kind => kind == TypeUseShapeKind.Collection) &&
        origins.All(origin => origin != TypeUseCollectionOriginKind.Unknown && Enum.IsDefined(origin));

    static bool ArrayOfArrays(TypeUseDefinition type, IReadOnlyList<TypeUseCollectionOriginKind> origins) =>
        type.Nodes is null && origins.Count >= 2 &&
        origins[0] == TypeUseCollectionOriginKind.Array && origins[1] == TypeUseCollectionOriginKind.Array &&
        type.Shape.Where(kind => kind != TypeUseShapeKind.Optional).Take(2).SequenceEqual([TypeUseShapeKind.Collection, TypeUseShapeKind.Collection]);

    static bool SupportedProfile(TypeUseCompatibilityProjectionDefinition? projection) =>
        projection is { Version: 1, Profile: TypeUseCompatibilityProfile.OneLayerCollection };

    static bool ValidPair(TypeUseDefinition? view, TypeUseDefinition? exact) => ValidObservation(exact) && ValidObservation(view);

    static bool ValidHeader(TypeUseDefinition type) => ValidShape(type.Shape) && ValidNode(Terminal(type));

    static bool CompleteNodes(TypeUseDefinition type) => type.Nodes is not { } nodes ||
        (nodes.Count == type.Shape.Count && nodes.All(ValidNode) && nodes[^1] == Terminal(type));

    static bool MatchesSelection(TypeUseCompatibilityProjectionDefinition projection, TypeUseDefinition exact)
    {
        var selection = Selection(exact.Shape);
        var node = SourceNode(exact, selection.Index);
        return projection.SourceNodeIndex == selection.Index && node is not null && MatchesView(projection.Type, exact, node, selection);
    }

    static (int Index, bool Optional, bool Collection) Selection(IReadOnlyList<TypeUseShapeKind> shape)
    {
        var optional = shape[0] == TypeUseShapeKind.Optional;
        var index = optional ? 1 : 0;
        var collection = shape[index] == TypeUseShapeKind.Collection;
        return (index + (collection ? 1 : 0), optional, collection);
    }

    static bool MatchesView(TypeUseDefinition view, TypeUseDefinition exact, TypeUseNodeObservation node, (int Index, bool Optional, bool Collection) selection) =>
        SameIdentity(view, node) && view.Shape.SequenceEqual(PrintableShape(selection.Optional, selection.Collection)) && CompatibleProjectedNodes(view, exact, selection.Index, node);

    static bool CompatibleProjectedNodes(TypeUseDefinition view, TypeUseDefinition exact, int index, TypeUseNodeObservation node) =>
        view.Nodes is not { } nodes || (exact.Nodes is { } source && nodes.SequenceEqual(source.Take(index).Append(node)));

    static bool ValidShape(IReadOnlyList<TypeUseShapeKind>? shape)
    {
        if (shape is not { Count: > 0 })
        {
            return false;
        }

        return shape[^1] == TypeUseShapeKind.Named && shape.Take(shape.Count - 1).All(IsWrapper);
    }

    static bool IsWrapper(TypeUseShapeKind kind) => kind is TypeUseShapeKind.Optional or TypeUseShapeKind.Collection;

    static TypeUseNodeObservation? SourceNode(TypeUseDefinition type, int index)
    {
        if (type.Nodes is { } nodes)
        {
            return nodes[index];
        }

        return index == type.Shape.Count - 1 ? Terminal(type) : null;
    }

    static TypeUseNodeObservation Terminal(TypeUseDefinition type) => new() { Name = type.Name, Subject = type.ObservedTypeSubject, Scope = type.ObservedScope };

    static bool SameIdentity(TypeUseDefinition type, TypeUseNodeObservation node) =>
        type.Name == node.Name && type.ObservedTypeSubject == node.Subject && type.ObservedScope == node.Scope;

    static IReadOnlyList<TypeUseShapeKind> PrintableShape(bool optional, bool collection) =>
    [
        .. optional ? new[] { TypeUseShapeKind.Optional } : [],
        .. collection ? new[] { TypeUseShapeKind.Collection } : [],
        TypeUseShapeKind.Named
    ];

    static bool ValidNode(TypeUseNodeObservation? node) => node is not null && ValidNodeIdentity(node);

    static bool ValidNodeIdentity(TypeUseNodeObservation node) =>
        AdapterContributionText.IsNormalized(node.Name, false) && ValidSubject(node.Subject) && ValidScope(node.Scope, node.Subject);

    static bool ValidSubject(SubjectId? subject) => subject is null || (NormalizedSubject(subject) && AbsoluteSubject(subject));

    static bool NormalizedSubject(SubjectId subject) => AdapterContributionText.IsNormalized(subject.Value, false) && !subject.Value.Contains('\\');

    static bool AbsoluteSubject(SubjectId subject) => Uri.TryCreate(subject.Value, UriKind.Absolute, out _) && !subject.Value.Split('/').Any(IsRelativeSegment);

    static bool IsRelativeSegment(string segment) => string.Equals(segment, ".", StringComparison.Ordinal) || string.Equals(segment, "..", StringComparison.Ordinal);

    static bool ValidScope(BackendScopeKey? scope, SubjectId? subject) => scope is null || (ValidScopeIdentity(scope, subject) && ValidModelName(scope.EventModelName));

    static bool ValidScopeIdentity(BackendScopeKey scope, SubjectId? subject) => subject is not null && scope.Store is not null && ValidSubject(scope.Store);

    static bool ValidModelName(string? name) => name is null || AdapterContributionText.IsNormalized(name, false);
}
