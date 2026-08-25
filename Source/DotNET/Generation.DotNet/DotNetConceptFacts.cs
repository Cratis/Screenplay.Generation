// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Screenplay.Generation.DotNet;

/// <summary>
/// Emits neutral concept facts from explicitly declared .NET concept nominations.
/// </summary>
/// <remarks>
/// Callers must nominate concepts from an authored declaration surface such as an attribute or registration API.
/// Structural wrapper-shape inference is not sufficient evidence.
/// </remarks>
public static class DotNetConceptFacts
{
    /// <summary>
    /// Emits a concept artifact and, when supported, its primitive representation using one declaration evidence source.
    /// </summary>
    /// <param name="wrapper">The source-declared concept wrapper type.</param>
    /// <param name="backing">The explicitly declared primitive backing type.</param>
    /// <param name="subject">The exact project-qualified wrapper subject.</param>
    /// <param name="evidence">The evidence establishing both concept nomination and backing representation.</param>
    /// <returns>The concept artifact followed by its representation when the backing type is supported.</returns>
    public static IReadOnlyList<GenerationFact> Emit(
        INamedTypeSymbol wrapper,
        ITypeSymbol backing,
        SubjectId subject,
        Evidence evidence) =>
        Emit(wrapper, backing, subject, evidence, evidence);

    /// <summary>
    /// Emits a concept artifact and, when supported, its primitive representation using independent evidence sources.
    /// </summary>
    /// <param name="wrapper">The source-declared concept wrapper type.</param>
    /// <param name="backing">The explicitly declared primitive backing type.</param>
    /// <param name="subject">The exact project-qualified wrapper subject.</param>
    /// <param name="conceptEvidence">The evidence establishing concept nomination.</param>
    /// <param name="representationEvidence">The evidence establishing the backing representation.</param>
    /// <returns>The concept artifact followed by its representation when the backing type is supported.</returns>
    public static IReadOnlyList<GenerationFact> Emit(
        INamedTypeSymbol wrapper,
        ITypeSymbol backing,
        SubjectId subject,
        Evidence conceptEvidence,
        Evidence representationEvidence)
    {
        var concept = new ArtifactFact
        {
            Id = FactIdFor(conceptEvidence.Adapter.Id, "concept", subject),
            Subject = subject,
            Evidence = conceptEvidence,
            Definition = new ArtifactDefinition
            {
                Key = new ArtifactKey { Subject = subject, Kind = ArtifactKind.Concept },
                Name = wrapper.Name,
                File = conceptEvidence.Source?.Path
            }
        };
        var primitive = PrimitiveFor(backing);
        if (primitive is null)
        {
            return [concept];
        }

        return
        [
            concept,
            new ConceptRepresentationFact
            {
                Id = FactIdFor(representationEvidence.Adapter.Id, "concept-representation", subject),
                Subject = subject,
                Evidence = representationEvidence,
                Definition = new ConceptRepresentationDefinition
                {
                    Concept = subject,
                    Kind = ConceptRepresentationKind.Primitive,
                    Primitive = primitive
                }
            }
        ];
    }

    static GenerationPrimitiveKind? PrimitiveFor(ITypeSymbol type) => type.SpecialType switch
    {
        SpecialType.System_String => GenerationPrimitiveKind.Text,
        SpecialType.System_Boolean => GenerationPrimitiveKind.Boolean,
        SpecialType.System_Byte or
        SpecialType.System_SByte or
        SpecialType.System_Int16 or
        SpecialType.System_UInt16 or
        SpecialType.System_Int32 or
        SpecialType.System_UInt32 or
        SpecialType.System_Int64 or
        SpecialType.System_UInt64 => GenerationPrimitiveKind.WholeNumber,
        SpecialType.System_Decimal or
        SpecialType.System_Double or
        SpecialType.System_Single => GenerationPrimitiveKind.Number,
        _ => NamedPrimitiveFor(type)
    };

    static GenerationPrimitiveKind? NamedPrimitiveFor(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol named || named.IsGenericType)
        {
            return null;
        }

        return DotNetSubjectIds.MetadataName(named) switch
        {
            "System.Guid" => GenerationPrimitiveKind.Uuid,
            "System.DateOnly" => GenerationPrimitiveKind.Date,
            "System.DateTime" or "System.DateTimeOffset" => GenerationPrimitiveKind.DateTime,
            _ => null
        };
    }

    static FactId FactIdFor(string prefix, string kind, SubjectId subject) => new()
    {
        Value = $"{prefix}:{kind}:{subject.Value}"
    };
}
