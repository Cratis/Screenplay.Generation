// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Screenplay.Generation.DotNet.Vogen;

/// <summary>
/// Discovers Vogen value-object declarations as neutral Screenplay concept facts.
/// </summary>
/// <remarks>
/// Recognition uses Roslyn metadata names and authoritative authored-source evidence. Generated members can corroborate
/// a declaration but never originate concept, identity, validation, normalization, named-instance, or representation evidence.
/// </remarks>
public sealed class VogenConceptScreenplayAdapter : IDotNetScreenplayAdapter
{
    const string AdapterId = "vogen";
    const string AdapterVersion = "1.0.0";
    const string ValidationPredicate = "Validate";
    const string ValidationRuleIdentity = "vogen.validate";

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
                project.AuthoredSyntaxTrees,
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
            project,
            EvidenceStrength.Exact,
            $"The authored type has the exact '{MetadataName(attribute)}' attribute");
        var backing = BackingType(project.Compilation, attribute, defaultAttribute);
        var representationEvidence = DotNetSource.EvidenceFor(
            backing.Evidence,
            identity,
            project,
            EvidenceStrength.Exact,
            $"Vogen configures '{DisplayName(backing.Type)}' as the backing type");
        var conceptFacts = DotNetConceptFacts.Emit(
            type,
            backing.Type,
            subject,
            conceptEvidence,
            representationEvidence);
        facts.AddRange(conceptFacts);
        if (!conceptFacts.OfType<ConceptRepresentationFact>().Any())
        {
            diagnostics.Add(new GenerationDiagnostic
            {
                Code = VogenGenerationDiagnosticCodes.UnsupportedBackingType,
                Severity = GenerationDiagnosticSeverity.Warning,
                Outcome = GenerationDiagnosticOutcome.Unsupported,
                Message = $"Vogen concept '{type.Name}' uses unsupported backing type '{DisplayName(backing.Type)}'; no concept representation was contributed",
                Source = representationEvidence.Source,
                Subject = subject
            });
        }

        var attributedDeclaration = DeclarationContaining(type, attribute);
        AddValidation(project, type, attributedDeclaration, backing.Type, subject, identity, facts);
        AddNormalizationDiagnostic(project, type, attributedDeclaration, backing.Type, subject, identity, diagnostics);
        AddNamedInstanceDiagnostics(project, type, subject, identity, diagnostics);
    }

    static void AddValidation(
        DotNetProjectCompilation project,
        INamedTypeSymbol type,
        SyntaxReference attributedDeclaration,
        ITypeSymbol backingType,
        SubjectId subject,
        AdapterIdentity identity,
        List<GenerationFact> facts)
    {
        var validationType = project.Compilation.GetTypeByMetadataName(VogenMetadataNames.Validation);
        if (validationType is null)
        {
            return;
        }

        var validation = ExactAuthoredMethod(
            project,
            type,
            attributedDeclaration,
            ValidationPredicate,
            backingType,
            validationType);
        if (validation is null)
        {
            return;
        }

        var evidence = EvidenceFor(
            project,
            validation,
            identity,
            $"The authored Vogen declaration has the exact static '{ValidationPredicate}({DisplayName(backingType)})' validation hook");
        facts.Add(new ConceptValidationRuleFact
        {
            Id = FactIdFor($"concept-validation:{ValidationRuleIdentity}", subject),
            Subject = subject,
            Evidence = evidence,
            Definition = new ConceptValidationRuleDefinition
            {
                Concept = subject,
                RuleIdentity = ValidationRuleIdentity,
                Kind = ConceptValidationRuleKind.NamedPredicate,
                Predicate = ValidationPredicate,
                Message = ConstantInvalidMessage(project.Compilation, validation),
                ImplementationFile = evidence.Source?.Path
            }
        });
    }

    static void AddNormalizationDiagnostic(
        DotNetProjectCompilation project,
        INamedTypeSymbol type,
        SyntaxReference attributedDeclaration,
        ITypeSymbol backingType,
        SubjectId subject,
        AdapterIdentity identity,
        List<GenerationDiagnostic> diagnostics)
    {
        var normalization = ExactAuthoredMethod(
            project,
            type,
            attributedDeclaration,
            "NormalizeInput",
            backingType,
            backingType);
        if (normalization is null)
        {
            return;
        }

        diagnostics.Add(new GenerationDiagnostic
        {
            Code = VogenGenerationDiagnosticCodes.InputNormalizationNotRepresented,
            Severity = GenerationDiagnosticSeverity.Warning,
            Outcome = GenerationDiagnosticOutcome.Unsupported,
            Message = $"Vogen concept '{type.Name}' normalizes input with authored method 'NormalizeInput'; Screenplay concept validation cannot preserve normalization and no validation fact was contributed for it",
            Source = EvidenceFor(project, normalization, identity).Source,
            Subject = subject
        });
    }

    static void AddNamedInstanceDiagnostics(
        DotNetProjectCompilation project,
        INamedTypeSymbol type,
        SubjectId subject,
        AdapterIdentity identity,
        List<GenerationDiagnostic> diagnostics)
    {
        foreach (var attribute in DotNetSource.AuthoredAttributesOf(type, project.AuthoredSyntaxTrees)
                     .Where(_ => string.Equals(MetadataName(_), VogenMetadataNames.InstanceAttribute, StringComparison.Ordinal)))
        {
            var name = attribute.ConstructorArguments.FirstOrDefault().Value as string;
            var displayName = string.IsNullOrWhiteSpace(name) ? "an unnamed instance" : $"named instance '{name}'";
            diagnostics.Add(new GenerationDiagnostic
            {
                Code = VogenGenerationDiagnosticCodes.NamedInstanceNotRepresented,
                Severity = GenerationDiagnosticSeverity.Warning,
                Outcome = GenerationDiagnosticOutcome.Unsupported,
                Message = $"Vogen concept '{type.Name}' declares {displayName}; Screenplay generation does not treat named instances as optional values or defaults and no concept fact was contributed for it",
                Source = DotNetSource.EvidenceFor(
                    attribute,
                    identity,
                    project,
                    EvidenceStrength.Exact).Source,
                Subject = subject
            });
        }
    }

    static IEnumerable<VogenDeclaration> DeclarationsIn(DotNetProjectCompilation project) =>
        new DotNetArtifactCatalog(project.Compilation).Types
            .Where(type =>
                DotNetSource.HasAuthoredDeclaration(type, project.AuthoredSyntaxTrees) &&
                DotNetSource.HasAuthoredPartialDeclaration(type, project.AuthoredSyntaxTrees))
            .Select(type => new
            {
                Type = type,
                Attribute = ValueObjectAttribute(type, project.AuthoredSyntaxTrees)
            })
            .Where(_ => _.Attribute is not null)
            .Select(_ => new VogenDeclaration(_.Type, _.Attribute!));

    static AttributeData? ValueObjectAttribute(
        INamedTypeSymbol type,
        IReadOnlySet<SyntaxTree> authoredSyntaxTrees) =>
        DotNetSource.AuthoredAttributesOf(type, authoredSyntaxTrees)
            .FirstOrDefault(_ =>
                string.Equals(MetadataName(_), VogenMetadataNames.ValueObjectAttribute, StringComparison.Ordinal) ||
                string.Equals(MetadataName(_), VogenMetadataNames.GenericValueObjectAttribute, StringComparison.Ordinal));

    static AttributeData? AuthoredAttribute(
        ISymbol symbol,
        IReadOnlySet<SyntaxTree> authoredSyntaxTrees,
        string metadataName) =>
        DotNetSource.AuthoredAttributesOf(symbol, authoredSyntaxTrees)
            .FirstOrDefault(_ => string.Equals(MetadataName(_), metadataName, StringComparison.Ordinal));

    static SyntaxReference DeclarationContaining(INamedTypeSymbol type, AttributeData attribute)
    {
        var application = attribute.ApplicationSyntaxReference!;
        return type.DeclaringSyntaxReferences.Single(reference =>
            reference.SyntaxTree == application.SyntaxTree &&
            reference.Span.Contains(application.Span));
    }

    static AuthoredMethod? ExactAuthoredMethod(
        DotNetProjectCompilation project,
        INamedTypeSymbol type,
        SyntaxReference attributedDeclaration,
        string name,
        ITypeSymbol parameterType,
        ITypeSymbol returnType) =>
        type.GetMembers(name)
            .OfType<IMethodSymbol>()
            .Where(method =>
                method.MethodKind == MethodKind.Ordinary &&
                method.IsStatic &&
                !method.IsGenericMethod &&
                !method.ReturnsByRef &&
                !method.ReturnsByRefReadonly &&
                SymbolEqualityComparer.Default.Equals(method.ReturnType, returnType) &&
                method.Parameters is [var parameter] &&
                parameter.RefKind == RefKind.None &&
                SymbolEqualityComparer.Default.Equals(parameter.Type, parameterType))
            .SelectMany(method => DotNetSource.AuthoredDeclarationsOf(method, project.AuthoredSyntaxTrees)
                .Where(reference =>
                    reference.SyntaxTree == attributedDeclaration.SyntaxTree &&
                    attributedDeclaration.Span.Contains(reference.Span) &&
                    project.Compilation.GetSemanticModel(reference.SyntaxTree).GetOperation(reference.GetSyntax()) is not null)
                .Select(reference => new AuthoredMethod(method, reference)))
            .OrderBy(_ => _.Reference.SyntaxTree.FilePath, StringComparer.Ordinal)
            .ThenBy(_ => _.Reference.Span.Start)
            .FirstOrDefault();

    static Evidence EvidenceFor(
        DotNetProjectCompilation project,
        AuthoredMethod method,
        AdapterIdentity identity,
        string? explanation = null) => new()
        {
            Adapter = identity,
            Strength = EvidenceStrength.Exact,
            Source = DotNetSource.RangeForProject(method.Reference.GetSyntax().GetLocation(), project),
            Explanation = explanation
        };

    static string? ConstantInvalidMessage(Compilation compilation, AuthoredMethod validation)
    {
        var operation = compilation.GetSemanticModel(validation.Reference.SyntaxTree).GetOperation(validation.Reference.GetSyntax());
        if (operation is null)
        {
            return null;
        }

        var invalidInvocations = OperationsIn(operation)
            .OfType<IInvocationOperation>()
            .Where(invocation =>
                invocation.TargetMethod.Name == "Invalid" &&
                invocation.TargetMethod.IsStatic &&
                invocation.TargetMethod.ContainingType is { } containingType &&
                string.Equals(DotNetSubjectIds.MetadataName(containingType), VogenMetadataNames.Validation, StringComparison.Ordinal) &&
                IsDirectlyReturnedFromValidation(invocation))
            .ToArray();
        if (invalidInvocations is not [var invalid])
        {
            return null;
        }

        var argument = invalid.Arguments.SingleOrDefault(_ => _.Parameter?.Ordinal == 0);
        return argument is { IsImplicit: false } &&
               argument.Value.ConstantValue is { HasValue: true, Value: string message } &&
               !string.IsNullOrEmpty(message)
            ? message
            : null;
    }

    static IEnumerable<IOperation> OperationsIn(IOperation root)
    {
        yield return root;
        foreach (var child in root.ChildOperations)
        {
            foreach (var descendant in OperationsIn(child))
            {
                yield return descendant;
            }
        }
    }

    static bool IsDirectlyReturnedFromValidation(IOperation operation)
    {
        var parent = operation.Parent;
        while (parent is IConditionalOperation or IConversionOperation)
        {
            parent = parent.Parent;
        }

        if (parent is not IReturnOperation returned)
        {
            return false;
        }

        for (parent = returned.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent is IAnonymousFunctionOperation or ILocalFunctionOperation)
            {
                return false;
            }
        }

        return true;
    }

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

    static FactId FactIdFor(string kind, SubjectId subject) => new()
    {
        Value = $"{AdapterId}:{kind}:{subject.Value}"
    };

    static string DisplayName(ITypeSymbol type) =>
        type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", string.Empty, StringComparison.Ordinal);

    sealed record AuthoredMethod(IMethodSymbol Method, SyntaxReference Reference);

    sealed record VogenDeclaration(INamedTypeSymbol Type, AttributeData Attribute);

    sealed record VogenBackingType(ITypeSymbol Type, AttributeData Evidence);
}
