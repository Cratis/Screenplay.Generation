// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission.given;

public class a_numbered_enumeration : a_contribution
{
    protected static ConceptRepresentationFact Representation(IReadOnlyList<int>? numbers, ConceptRepresentationKind kind = ConceptRepresentationKind.Enumeration) => new()
    {
        Id = Id("representation"),
        Subject = ArtifactSubject,
        Evidence = Evidence(),
        Definition = new ConceptRepresentationDefinition
        {
            Concept = ArtifactSubject,
            Kind = kind,
            Primitive = kind == ConceptRepresentationKind.Primitive ? GenerationPrimitiveKind.WholeNumber : null,
            EnumerationValues = kind == ConceptRepresentationKind.Enumeration ? ["Open", "Closed"] : [],
            EnumerationNumbers = numbers
        }
    };
}
