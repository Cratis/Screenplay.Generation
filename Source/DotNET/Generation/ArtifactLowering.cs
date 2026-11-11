// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using static Cratis.Screenplay.Generation.LoweringSupport;

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Lowers placed artifacts into the Screenplay commands, events, queries, read models and reducers of a slice.
/// </summary>
internal static class ArtifactLowering
{
    public static CommandSyntax BuildCommand(ArtifactDefinition definition, LoweringContext context)
    {
        var productionRelationships = context.RelationshipsFrom(definition.Key.Subject, RelationshipKind.Produces);
        var imperativeProductionRelationships = productionRelationships
            .Where(_ => _.Key.Discriminator == "imperative")
            .ToArray();
        var hasImperativeProduction = imperativeProductionRelationships.Length > 0;
        var loweredProductionRelationships = productionRelationships
            .Where(_ => !hasImperativeProduction && _.Key.Discriminator != "imperative")
            .Select(_ => (Relationship: _, Name: context.ArtifactName(_.Key.Target, ArtifactKind.Event)))
            .Where(_ => _.Name is not null)
            .ToArray();
        var produces = loweredProductionRelationships
            .Select(_ => _.Name!)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Select(_ => new ProducesSyntax(_, null, [], Generated))
            .ToArray();
        var loweredReadRelationships = context.RelationshipsFrom(definition.Key.Subject, RelationshipKind.Reads)
            .Select(_ => (Relationship: _, Name: context.ArtifactName(_.Key.Target, ArtifactKind.ReadModel)))
            .Where(_ => _.Name is not null)
            .ToArray();
        var reads = loweredReadRelationships
            .Select(_ => new ReadsSyntax(_.Name!, _.Relationship.SourceMember, Generated))
            .ToArray();
        MarkLowered(
            context,
            [.. loweredProductionRelationships.Select(_ => _.Relationship), .. loweredReadRelationships.Select(_ => _.Relationship)]);

        var handler = (produces.Length == 0 || hasImperativeProduction) && definition.File is not null
            ? new HandlerSyntax(FileFrom(definition.File), null, Generated)
            : null;
        if (handler is not null)
        {
            MarkLowered(context, imperativeProductionRelationships);
        }

        return new(
            definition.Name,
            definition.Properties.Select(_ => BuildProperty(_, context)),
            null,
            [],
            produces,
            handler,
            Generated,
            Description: definition.Description,
            Reads: reads);
    }

    public static QuerySyntax? BuildQuery(ArtifactDefinition definition, LoweringContext context)
    {
        var returns = context.RelationshipsFrom(definition.Key.Subject, RelationshipKind.Returns);
        var returnType = returns.Length == 1
            ? context.ArtifactName(returns[0].Key.Target, ArtifactKind.ReadModel)
            : null;
        if (returnType is null)
        {
            Omit(context, definition, $"Query '{definition.Name}' was omitted because it does not return exactly one known read model");
            return null;
        }

        var identifyingProperty = definition.Properties.FirstOrDefault(_ => _.IsIdentifier);
        var by = identifyingProperty is null
            ? null
            : new QueryParameterSyntax(identifyingProperty.Name, BuildType(identifyingProperty.Type, context), Generated);
        var filters = definition.Properties
            .Where(_ => _ != identifyingProperty)
            .Select(_ => new QueryParameterSyntax(_.Name, BuildType(_.Type, context), Generated))
            .ToArray();
        var relationship = returns[0];
        context.Sink.Coverage.Lowered(GenerationFactSemanticKey.Relationship(relationship));

        return new(
            definition.Name,
            new TypeRefSyntax(returnType, relationship.IsCollection, relationship.IsOptional, Generated),
            by,
            filters,
            null,
            Generated,
            definition.Description,
            definition.File is null ? null : new PerformerSyntax(FileFrom(definition.File), null, Generated));
    }

    public static EventSyntax BuildEvent(ArtifactDefinition definition, LoweringContext context) => new(
        definition.Name,
        definition.Properties.Select(_ => BuildProperty(_, context)),
        Generated)
    {
        File = FileFrom(definition.File)
    };

    public static ReadModelSyntax BuildReadModel(ArtifactDefinition definition, LoweringContext context) => new(
        definition.Name,
        definition.Properties.Select(_ => BuildProperty(_, context)),
        Generated,
        definition.Description)
    {
        File = FileFrom(definition.File)
    };

    public static ReducerSyntax? BuildReducer(ArtifactDefinition definition, LoweringContext context)
    {
        var builds = context.RelationshipsFrom(definition.Key.Subject, RelationshipKind.Builds);
        var consumes = context.RelationshipsFrom(definition.Key.Subject, RelationshipKind.Consumes);
        var readModel = builds.Length == 1
            ? context.ArtifactName(builds[0].Key.Target, ArtifactKind.ReadModel)
            : null;
        if (readModel is null || consumes.Length == 0)
        {
            Omit(context, definition, $"Reducer '{definition.Name}' was omitted because it does not identify exactly one read model and at least one consumed event");
            return null;
        }

        var loweredConsumes = consumes
            .Where(_ => context.ArtifactName(_.Key.Target, ArtifactKind.Event) is not null)
            .ToArray();
        var rules = loweredConsumes
            .Select(_ => context.ArtifactName(_.Key.Target, ArtifactKind.Event)!)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .Select(_ => new ReducerRuleSyntax(_, FileFrom(definition.File), null, Generated))
            .ToArray();
        if (rules.Length == 0)
        {
            Omit(context, definition, $"Reducer '{definition.Name}' was omitted because none of its consumed event subjects resolve to event artifacts");
            return null;
        }

        MarkLowered(context, [builds[0], .. loweredConsumes]);

        return new(definition.Name, readModel, rules, Generated, definition.Description);
    }

    static void Omit(LoweringContext context, ArtifactDefinition definition, string message) =>
        context.Sink.Omit(
            Warning(GenerationDiagnosticCodes.IncompleteArtifact, GenerationDiagnosticOutcome.Unknown, message) with
            {
                Subject = definition.Key.Subject
            },
            GenerationFactSemanticKey.Artifact(definition));

    static void MarkLowered(LoweringContext context, IEnumerable<RelationshipDefinition> relationships)
    {
        foreach (var relationship in relationships)
        {
            context.Sink.Coverage.Lowered(GenerationFactSemanticKey.Relationship(relationship));
        }
    }

    static PropertySyntax BuildProperty(PropertyDefinition property, LoweringContext context) => new(
        property.Name,
        BuildType(property.Type, context),
        Generated,
        property.IsIdentifier);

    static TypeRefSyntax BuildType(TypeReferenceDefinition type, LoweringContext context) => new(
        context.TypeName(type),
        type.IsCollection,
        type.IsOptional,
        Generated);
}
