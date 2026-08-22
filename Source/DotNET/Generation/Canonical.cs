// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class Canonical
{
    const char Separator = '\u001f';

    public static string Artifact(ArtifactDefinition definition)
    {
        var properties = string.Join(
            Separator,
            definition.Properties.Select(_ => Join(
                _.Name,
                _.Type.Name,
                _.Type.Subject?.Value,
                _.Type.IsCollection.ToString(),
                _.Type.IsOptional.ToString(),
                _.IsIdentifier.ToString())));

        return Join(
            ArtifactKey(definition.Key),
            definition.Name,
            definition.Description,
            definition.File,
            properties);
    }

    public static string ArtifactKey(ArtifactKey key) => Join(key.Subject.Value, key.Kind.ToString());

    public static string ConceptRepresentation(ConceptRepresentationDefinition definition) =>
        Join(
            definition.Concept.Value,
            definition.Kind.ToString(),
            definition.Primitive?.ToString(),
            string.Concat(definition.EnumerationValues.Select(_ => $"{_.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)}:{_}")));

    public static string Placement(ArtifactPlacement placement) =>
        Join(
            placement.Module,
            string.Join(Separator, placement.Features),
            placement.Slice,
            placement.SliceKind.ToString());

    public static string Relationship(RelationshipDefinition definition) =>
        Join(
            RelationshipKey(definition.Key),
            definition.SourceMember,
            definition.TargetMember,
            definition.IsCollection.ToString(),
            definition.IsOptional.ToString());

    public static string RelationshipKey(RelationshipKey key) =>
        Join(key.Kind.ToString(), key.Source.Value, key.Target.Value, key.Discriminator);

    public static string Evidence(Evidence evidence) =>
        Join(
            evidence.Adapter.Id,
            evidence.Adapter.Version,
            evidence.Strength.ToString(),
            evidence.Source?.Path,
            evidence.Source?.StartLine.ToString(),
            evidence.Source?.StartColumn.ToString(),
            evidence.Source?.EndLine.ToString(),
            evidence.Source?.EndColumn.ToString(),
            evidence.Explanation);

    public static string Diagnostic(GenerationDiagnostic diagnostic) =>
        Join(
            ((int)diagnostic.Severity).ToString("D2", System.Globalization.CultureInfo.InvariantCulture),
            diagnostic.Code,
            diagnostic.Source?.Path,
            diagnostic.Source?.StartLine.ToString(),
            diagnostic.Source?.StartColumn.ToString(),
            diagnostic.Subject?.Value,
            diagnostic.Message);

    static string Join(params string?[] values) => string.Join(Separator, values.Select(_ => _ ?? string.Empty));
}
