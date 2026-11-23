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
        if (type is null || !ValidShape(type.Shape) || !ValidNode(new() { Name = type.Name, Subject = type.ObservedTypeSubject, Scope = type.ObservedScope }))
        {
            return false;
        }

        return type.Nodes is not { } nodes ||
            (nodes.Count == type.Shape.Count && nodes.All(ValidNode) &&
             nodes[^1] == new TypeUseNodeObservation { Name = type.Name, Subject = type.ObservedTypeSubject, Scope = type.ObservedScope });
    }

    /// <summary>Checks a versioned printable view against the exact source-node evidence.</summary>
    /// <param name="projection">The explicit projection.</param>
    /// <param name="observation">The original exact observation.</param>
    /// <returns>Whether the view has the supported profile, stopping node, flags and identity.</returns>
    public static bool Valid(TypeUseCompatibilityProjectionDefinition? projection, TypeUseDefinition? observation)
    {
        if (projection is not { Version: 1, Profile: TypeUseCompatibilityProfile.OneLayerCollection } ||
            !ValidObservation(observation) || !ValidObservation(projection.Type))
        {
            return false;
        }

        var shape = observation!.Shape;
        var index = shape[0] == TypeUseShapeKind.Optional ? 1 : 0;
        var optional = index == 1;
        var collection = shape[index] == TypeUseShapeKind.Collection;
        index += collection ? 1 : 0;
        var node = SourceNode(observation, index);
        return projection.SourceNodeIndex == index && node is not null &&
            SameIdentity(projection.Type, node) && projection.Type.Shape.SequenceEqual(PrintableShape(optional, collection)) &&
            CompatibleProjectedNodes(projection.Type, observation, index, node);
    }

    static bool CompatibleProjectedNodes(TypeUseDefinition view, TypeUseDefinition exact, int index, TypeUseNodeObservation node) =>
        view.Nodes is not { } nodes || (exact.Nodes is { } source && nodes.SequenceEqual(source.Take(index).Append(node)));

    static bool ValidShape(IReadOnlyList<TypeUseShapeKind>? shape) => shape is { Count: > 0 } &&
        shape[^1] == TypeUseShapeKind.Named && shape.Take(shape.Count - 1).All(node => node is TypeUseShapeKind.Optional or TypeUseShapeKind.Collection);

    static TypeUseNodeObservation? SourceNode(TypeUseDefinition type, int index)
    {
        if (type.Nodes is { } nodes)
        {
            return nodes[index];
        }

        return index == type.Shape.Count - 1
            ? new() { Name = type.Name, Subject = type.ObservedTypeSubject, Scope = type.ObservedScope }
            : null;
    }

    static bool SameIdentity(TypeUseDefinition type, TypeUseNodeObservation node) =>
        type.Name == node.Name && type.ObservedTypeSubject == node.Subject && type.ObservedScope == node.Scope;

    static IReadOnlyList<TypeUseShapeKind> PrintableShape(bool optional, bool collection) =>
    [
        .. optional ? new[] { TypeUseShapeKind.Optional } : [],
        .. collection ? new[] { TypeUseShapeKind.Collection } : [],
        TypeUseShapeKind.Named
    ];

    static bool ValidNode(TypeUseNodeObservation? node) => node is not null && AdapterContributionText.IsNormalized(node.Name, false) &&
        ValidSubject(node.Subject) && ValidScope(node.Scope, node.Subject);

    static bool ValidSubject(SubjectId? subject) => subject is null ||
        (AdapterContributionText.IsNormalized(subject.Value, false) && !subject.Value.Contains('\\') &&
         Uri.TryCreate(subject.Value, UriKind.Absolute, out _) && !subject.Value.Split('/').Any(segment => string.Equals(segment, ".", StringComparison.Ordinal) || string.Equals(segment, "..", StringComparison.Ordinal)));

    static bool ValidScope(BackendScopeKey? scope, SubjectId? subject) => scope is null ||
        (subject is not null && scope.Store is not null && ValidSubject(scope.Store) &&
         (scope.EventModelName is null || AdapterContributionText.IsNormalized(scope.EventModelName, false)));
}
