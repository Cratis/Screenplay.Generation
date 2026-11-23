// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation;

static class ConceptNameReservations
{
    public static ImmutableArray<Reservation> Find(ImmutableArray<GenerationFact> facts)
    {
        // Resolve exact subjects, never names, to distinguish an unbound name from a concept's own uses.
        var preview = TypeUseBindingDerivation.Derive([.. facts.Select(fact => new GenerationFactRecord { Fact = fact })]);
        var bindings = facts.OfType<TypeUseBindingFact>().Concat(preview.Facts.Select(record => record.Fact).OfType<TypeUseBindingFact>())
            .Where(fact => fact.Definition.Target.Kind == ArtifactKind.Concept)
            .Select(fact => fact.Definition.Member).ToHashSet();
        var conceptSubjects = facts.Select(ConceptSubject).OfType<SubjectId>().ToHashSet();
        return
        [
            .. facts.OfType<ArtifactMemberTypeUseFact>()
                .Where(fact => !bindings.Contains(fact.Definition.Member))
                .Select(fact => new Reservation(fact.Definition.Type.Name, fact)),
            .. facts.OfType<ArtifactFact>().SelectMany(fact => fact.Definition.Properties
                .Where(property => property.Type.Subject is not { } subject || !conceptSubjects.Contains(subject))
                .Select(property => new Reservation(property.Type.Name, fact)))
        ];
    }

    static SubjectId? ConceptSubject(GenerationFact fact) => fact switch
    {
        ArtifactDeclarationFact declaration when declaration.Definition.Artifact.Kind == ArtifactKind.Concept => declaration.Subject,
        ArtifactFact artifact when artifact.Definition.Key.Kind == ArtifactKind.Concept => artifact.Subject,
        _ => null
    };

    internal sealed record Reservation(string Name, GenerationFact Fact);
}
