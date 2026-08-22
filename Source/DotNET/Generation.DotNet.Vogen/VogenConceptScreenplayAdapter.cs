// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Screenplay.Generation.DotNet.Vogen;

/// <summary>
/// Discovers Vogen value-object declarations as neutral Screenplay concept facts.
/// </summary>
/// <remarks>
/// Recognition uses Roslyn metadata names and authored attribute applications only. Generated members can corroborate
/// a declaration but never originate concept, identity, validation, or representation facts.
/// </remarks>
public sealed class VogenConceptScreenplayAdapter : IDotNetScreenplayAdapter
{
    const string AdapterId = "vogen";
    const string AdapterVersion = "1.0.0";

    /// <inheritdoc/>
    public AdapterIdentity Identity { get; } = new() { Id = AdapterId, Version = AdapterVersion };

    /// <inheritdoc/>
    public bool CanAnalyze(DotNetAnalysisContext context) =>
        context.Projects.Any(project => DeclarationsIn(project).Any());

    /// <inheritdoc/>
    public AdapterContribution Analyze(DotNetAnalysisContext context, DotNetAdapterOptions options)
    {
        _ = options;
        var facts = new List<GenerationFact>();
        var diagnostics = new List<GenerationDiagnostic>();

        foreach (var project in context.Projects)
        {
            var defaultAttribute = AuthoredAttribute(
                project.Compilation.Assembly,
                VogenMetadataNames.DefaultsAttribute);

            foreach (var declaration in DeclarationsIn(project))
            {
                AddConcept(project, declaration.Type, declaration.Attribute, defaultAttribute, Identity, facts, diagnostics);
            }
        }

        return new()
        {
            Adapter = Identity,
            Facts = facts,
            Diagnostics = diagnostics
        };
    }

    static void AddConcept(
        DotNetProjectCompilation project,
        INamedTypeSymbol type,
        AttributeData attribute,
        AttributeData? defaultAttribute,
        AdapterIdentity identity,
        List<GenerationFact> facts,
        List<GenerationDiagnostic> diagnostics)
    {
        var subject = project.SubjectForType(type);
        var conceptEvidence = DotNetSource.EvidenceFor(
            attribute,
            identity,
            EvidenceStrength.Exact,
            project.SourceRoot,
            $"The authored type has the exact '{MetadataName(attribute)}' attribute");
        facts.Add(new ArtifactFact
        {
            Id = FactIdFor("concept", subject),
            Subject = subject,
            Evidence = conceptEvidence,
            Definition = new ArtifactDefinition
            {
                Key = new ArtifactKey { Subject = subject, Kind = ArtifactKind.Concept },
                Name = type.Name,
                File = conceptEvidence.Source?.Path
            }
        });

        var backing = BackingType(project.Compilation, attribute, defaultAttribute);
        var primitive = PrimitiveFor(backing.Type);
        if (primitive is not null)
        {
            var representationEvidence = DotNetSource.EvidenceFor(
                backing.Evidence,
                identity,
                EvidenceStrength.Exact,
                project.SourceRoot,
                $"Vogen configures '{DisplayName(backing.Type)}' as the backing type");
            facts.Add(new ConceptRepresentationFact
            {
                Id = FactIdFor("concept-representation", subject),
                Subject = subject,
                Evidence = representationEvidence,
                Definition = new ConceptRepresentationDefinition
                {
                    Concept = subject,
                    Kind = ConceptRepresentationKind.Primitive,
                    Primitive = primitive
                }
            });

            return;
        }

        diagnostics.Add(new GenerationDiagnostic
        {
            Code = VogenGenerationDiagnosticCodes.UnsupportedBackingType,
            Severity = GenerationDiagnosticSeverity.Warning,
            Message = $"Vogen concept '{type.Name}' uses unsupported backing type '{DisplayName(backing.Type)}'; no concept representation was contributed",
            Source = DotNetSource.EvidenceFor(
                backing.Evidence,
                identity,
                EvidenceStrength.Exact,
                project.SourceRoot).Source,
            Subject = subject
        });
    }

    static IEnumerable<VogenDeclaration> DeclarationsIn(DotNetProjectCompilation project) =>
        new DotNetArtifactCatalog(project.Compilation).AuthoredTypes
            .Where(DotNetSource.HasAuthoredPartialDeclaration)
            .Select(type => new { Type = type, Attribute = ValueObjectAttribute(type) })
            .Where(_ => _.Attribute is not null)
            .Select(_ => new VogenDeclaration(_.Type, _.Attribute!));

    static AttributeData? ValueObjectAttribute(INamedTypeSymbol type) =>
        DotNetSource.AuthoredAttributesOf(type)
            .FirstOrDefault(_ =>
                string.Equals(MetadataName(_), VogenMetadataNames.ValueObjectAttribute, StringComparison.Ordinal) ||
                string.Equals(MetadataName(_), VogenMetadataNames.GenericValueObjectAttribute, StringComparison.Ordinal));

    static AttributeData? AuthoredAttribute(ISymbol symbol, string metadataName) =>
        DotNetSource.AuthoredAttributesOf(symbol)
            .FirstOrDefault(_ => string.Equals(MetadataName(_), metadataName, StringComparison.Ordinal));

    static string? MetadataName(AttributeData attribute) =>
        attribute.AttributeClass is null ? null : DotNetSubjectIds.MetadataName(attribute.AttributeClass);

    static VogenBackingType BackingType(
        Compilation compilation,
        AttributeData valueObjectAttribute,
        AttributeData? defaultAttribute)
    {
        if (MetadataName(valueObjectAttribute) == VogenMetadataNames.GenericValueObjectAttribute &&
            valueObjectAttribute.AttributeClass?.TypeArguments is [var genericBacking])
        {
            return new(genericBacking, valueObjectAttribute);
        }

        var localBacking = TypeConstructorArgument(valueObjectAttribute);
        if (localBacking is not null)
        {
            return new(localBacking, valueObjectAttribute);
        }

        var defaultBacking = defaultAttribute is null ? null : TypeConstructorArgument(defaultAttribute);
        if (defaultBacking is not null)
        {
            return new(defaultBacking, defaultAttribute!);
        }

        return new(compilation.GetSpecialType(SpecialType.System_Int32), valueObjectAttribute);
    }

    static ITypeSymbol? TypeConstructorArgument(AttributeData attribute) =>
        attribute.ConstructorArguments.FirstOrDefault().Value as ITypeSymbol;

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

    static FactId FactIdFor(string kind, SubjectId subject) => new()
    {
        Value = $"{AdapterId}:{kind}:{subject.Value}"
    };

    static string DisplayName(ITypeSymbol type) =>
        type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", string.Empty, StringComparison.Ordinal);

    sealed record VogenDeclaration(INamedTypeSymbol Type, AttributeData Attribute);

    sealed record VogenBackingType(ITypeSymbol Type, AttributeData Evidence);
}
