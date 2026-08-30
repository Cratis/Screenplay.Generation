// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Generation;

static class AdapterContributionAdmissionValidator
{
    public static void Validate(
        FrozenAdapterContributionInput input,
        ISourceAuthorityValidator? sourceAuthorityValidator,
        AdapterContributionAdmissionContext context)
    {
        ValidateDescriptor(input.Descriptor, context);
        ValidateProducer(input, context);
        ValidateDuplicateFactIds(input.Facts, context);

        foreach (var fact in input.Facts)
        {
            ValidateFact(input.Descriptor, fact, sourceAuthorityValidator, context);
        }

        var factIds = input.Facts.Select(fact => fact.Id.Value).ToHashSet(StringComparer.Ordinal);
        for (var index = 0; index < input.Diagnostics.Length; index++)
        {
            ValidateDiagnostic(
                input.Descriptor.Identity.Id,
                factIds,
                input.Diagnostics[index],
                index,
                sourceAuthorityValidator,
                context);
        }
    }

    public static void ValidateSubject(
        SubjectId subject,
        string path,
        FactId? fact,
        AdapterContributionAdmissionContext context)
    {
        var value = subject.Value;
        if (!AdapterContributionText.IsNormalized(value, false) ||
            value.Contains('\\') ||
            !Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            string.IsNullOrWhiteSpace(uri.Scheme) ||
            HasAuthoredDotPathSegment(value))
        {
            context.Add(
                AdapterContributionAdmissionDiagnosticCode.InvalidSubject,
                path,
                $"Subject '{value}' must be a normalized absolute stable URI without whitespace, control characters, backslashes, or dot path segments",
                fact,
                subject);
        }
    }

    public static void ValidateRequiredText(
        string? value,
        string path,
        FactId fact,
        SubjectId subject,
        AdapterContributionAdmissionContext context)
    {
        if (!AdapterContributionText.IsRequired(value))
        {
            context.Add(
                AdapterContributionAdmissionDiagnosticCode.MissingRequiredValue,
                path,
                $"{path} is required",
                fact,
                subject);
        }
    }

    public static void ValidateArtifactKey(
        ArtifactKey key,
        string path,
        FactId fact,
        AdapterContributionAdmissionContext context)
    {
        ValidateSubject(key.Subject, $"{path}.Subject", fact, context);
        context.Enum(key.Kind, ArtifactKind.Unknown, $"{path}.Kind", fact, key.Subject);
    }

    public static void ValidateType(
        TypeReferenceDefinition type,
        string path,
        FactId fact,
        SubjectId subject,
        AdapterContributionAdmissionContext context)
    {
        ValidateRequiredText(type.Name, $"{path}.Name", fact, subject, context);
        if (type.Subject is not null)
        {
            ValidateSubject(type.Subject, $"{path}.Subject", fact, context);
        }

        if (type.TargetArtifactKind is { } targetArtifactKind)
        {
            context.Enum(targetArtifactKind, ArtifactKind.Unknown, $"{path}.TargetArtifactKind", fact, subject);
            if (type.Subject is null)
            {
                context.Add(
                    AdapterContributionAdmissionDiagnosticCode.InvalidKindOperand,
                    $"{path}.TargetArtifactKind",
                    "A target artifact kind requires an exact target subject",
                    fact,
                    subject);
            }
        }
    }

    internal static void ValidateDescriptor(
        AdapterDescriptor descriptor,
        AdapterContributionAdmissionContext context)
    {
        if (!IsIdentityPart(descriptor.Identity.Id) || descriptor.Identity.Id.Contains(':', StringComparison.Ordinal))
        {
            context.Add(
                AdapterContributionAdmissionDiagnosticCode.InvalidDescriptorIdentity,
                "Descriptor.Identity.Id",
                "Descriptor.Identity.Id must be normalized, contain no whitespace or control characters, and contain no ':' separator");
        }

        if (!IsIdentityPart(descriptor.Identity.Version))
        {
            context.Add(
                AdapterContributionAdmissionDiagnosticCode.InvalidDescriptorIdentity,
                "Descriptor.Identity.Version",
                "Descriptor.Identity.Version must be normalized and contain no whitespace or control characters");
        }

        context.Enum(descriptor.SourceLanguage, AdapterSourceLanguage.Unknown, "Descriptor.SourceLanguage");
        context.Enum(descriptor.Category, AdapterCategory.Unknown, "Descriptor.Category");
        for (var index = 0; index < descriptor.RequiredHostCapabilities.Length; index++)
        {
            context.Enum(
                descriptor.RequiredHostCapabilities[index],
                AdapterHostCapability.Unknown,
                $"Descriptor.RequiredHostCapabilities[{index}]");
        }

        for (var index = 0; index < descriptor.RequiredApiCapabilities.Length; index++)
        {
            var capability = descriptor.RequiredApiCapabilities[index];
            if (!AdapterContributionText.IsNormalized(capability.Id, false))
            {
                context.Add(
                    AdapterContributionAdmissionDiagnosticCode.InvalidApiCapability,
                    $"Descriptor.RequiredApiCapabilities[{index}]",
                    "Required API capability identities must be nonempty, normalized, and contain no whitespace or control characters");
            }
        }

        foreach (var duplicate in descriptor.RequiredApiCapabilities
                     .GroupBy(capability => capability.Id, StringComparer.Ordinal)
                     .Where(group => group.Count() > 1)
                     .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            context.Add(
                AdapterContributionAdmissionDiagnosticCode.DuplicateApiCapability,
                "Descriptor.RequiredApiCapabilities",
                $"Required API capability '{duplicate.Key}' occurs {duplicate.Count()} times");
        }

        for (var index = 0; index < descriptor.EmittedFactCapabilities.Length; index++)
        {
            context.Enum(
                descriptor.EmittedFactCapabilities[index],
                GenerationFactCapability.Unknown,
                $"Descriptor.EmittedFactCapabilities[{index}]");
        }

        var range = descriptor.CompatibleGenerationVersions;
        if (range.MinimumInclusive is null ||
            (range.MaximumExclusive is not null && range.MaximumExclusive <= range.MinimumInclusive))
        {
            context.Add(
                AdapterContributionAdmissionDiagnosticCode.InvalidGenerationVersionRange,
                "Descriptor.CompatibleGenerationVersions",
                "Descriptor.CompatibleGenerationVersions must have a minimum and an optional greater exclusive maximum");
        }
    }

    static void ValidateProducer(
        FrozenAdapterContributionInput input,
        AdapterContributionAdmissionContext context)
    {
        if (input.ContributionAdapter != input.Descriptor.Identity)
        {
            context.Add(
                AdapterContributionAdmissionDiagnosticCode.ContributionAdapterMismatch,
                "Contribution.Adapter",
                $"Contribution adapter '{input.ContributionAdapter.Id}@{input.ContributionAdapter.Version}' does not equal descriptor adapter '{input.Descriptor.Identity.Id}@{input.Descriptor.Identity.Version}'");
        }
    }

    static void ValidateDuplicateFactIds(
        IEnumerable<GenerationFact> facts,
        AdapterContributionAdmissionContext context)
    {
        foreach (var duplicate in facts
                     .GroupBy(fact => fact.Id.Value, StringComparer.Ordinal)
                     .Where(group => group.Count() > 1)
                     .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            var first = duplicate.First();
            context.Add(
                AdapterContributionAdmissionDiagnosticCode.DuplicateFactId,
                "Contribution.Facts",
                $"Fact identity '{duplicate.Key}' occurs {duplicate.Count()} times",
                first.Id,
                first.Subject);
        }
    }

    static void ValidateFact(
        AdapterDescriptor descriptor,
        GenerationFact fact,
        ISourceAuthorityValidator? sourceAuthorityValidator,
        AdapterContributionAdmissionContext context)
    {
        var factPath = $"Contribution.Facts[{fact.Id.Value}]";
        ValidateFactId(descriptor.Identity.Id, fact, factPath, context);
        ValidateSubject(fact.Subject, $"{factPath}.Subject", fact.Id, context);

        if (fact.Evidence.Adapter != descriptor.Identity)
        {
            context.Add(
                AdapterContributionAdmissionDiagnosticCode.EvidenceAdapterMismatch,
                $"{factPath}.Evidence.Adapter",
                $"Fact evidence adapter '{fact.Evidence.Adapter.Id}@{fact.Evidence.Adapter.Version}' does not equal descriptor adapter '{descriptor.Identity.Id}@{descriptor.Identity.Version}'",
                fact.Id,
                fact.Subject);
        }

        context.Enum(
            fact.Evidence.Strength,
            EvidenceStrength.Unknown,
            $"{factPath}.Evidence.Strength",
            fact.Id,
            fact.Subject);
        if (fact.Evidence.Source is not null)
        {
            ValidateSource(
                fact.Evidence.Source,
                $"{factPath}.Evidence.Source",
                fact.Id,
                fact.Subject,
                sourceAuthorityValidator,
                context);
        }

        var capability = CapabilityFor(fact);
        if (!descriptor.EmittedFactCapabilities.Contains(capability))
        {
            context.Add(
                AdapterContributionAdmissionDiagnosticCode.UndeclaredFactCapability,
                factPath,
                $"Descriptor '{descriptor.Identity.Id}' does not declare emitted fact capability '{capability}'",
                fact.Id,
                fact.Subject);
        }

        AdapterFactAdmissionValidator.Validate(fact, factPath, context);
    }

    static void ValidateFactId(
        string adapterId,
        GenerationFact fact,
        string path,
        AdapterContributionAdmissionContext context)
    {
        var value = fact.Id.Value;
        if (!AdapterContributionText.IsNormalized(value, false))
        {
            context.Add(
                AdapterContributionAdmissionDiagnosticCode.InvalidFactId,
                $"{path}.Id",
                "Fact identity must be nonempty, normalized, and contain no whitespace or control characters",
                fact.Id,
                fact.Subject);
            return;
        }

        var prefix = $"{adapterId}:";
        if (!value.StartsWith(prefix, StringComparison.Ordinal) || value.Length == prefix.Length)
        {
            context.Add(
                AdapterContributionAdmissionDiagnosticCode.UnscopedFactId,
                $"{path}.Id",
                $"Fact identity '{value}' must be scoped beneath producer '{prefix}' and is never rewritten during admission",
                fact.Id,
                fact.Subject);
        }
    }

    static void ValidateDiagnostic(
        string adapterId,
        HashSet<string> admittedFactIds,
        GenerationDiagnostic diagnostic,
        int index,
        ISourceAuthorityValidator? sourceAuthorityValidator,
        AdapterContributionAdmissionContext context)
    {
        var path = $"Contribution.Diagnostics[{index}]";
        if (!AdapterContributionText.IsRequired(diagnostic.Code))
        {
            context.Add(
                AdapterContributionAdmissionDiagnosticCode.InvalidContributionDiagnostic,
                $"{path}.Code",
                "Contribution diagnostic code is required");
        }

        if (!AdapterContributionText.IsRequired(diagnostic.Message))
        {
            context.Add(
                AdapterContributionAdmissionDiagnosticCode.InvalidContributionDiagnostic,
                $"{path}.Message",
                "Contribution diagnostic message is required");
        }

        context.Enum(diagnostic.Severity, GenerationDiagnosticSeverity.Unknown, $"{path}.Severity");
        if (diagnostic.Outcome is { } outcome && !Enum.IsDefined(outcome))
        {
            context.Add(
                AdapterContributionAdmissionDiagnosticCode.UndefinedEnumValue,
                $"{path}.Outcome",
                $"{path}.Outcome contains undefined {nameof(GenerationDiagnosticOutcome)} value '{(int)outcome}'");
        }

        var linkedFactIds = new HashSet<string>(StringComparer.Ordinal);
        var factPrefix = $"{adapterId}:";
        for (var factIndex = 0; factIndex < diagnostic.Facts.Length; factIndex++)
        {
            var linkedFact = diagnostic.Facts[factIndex];
            var factPath = $"{path}.Facts[{factIndex}]";
            if (!AdapterContributionText.IsNormalized(linkedFact.Value, false) ||
                linkedFact.Value.Length == factPrefix.Length ||
                !linkedFact.Value.StartsWith(factPrefix, StringComparison.Ordinal))
            {
                context.Add(
                    AdapterContributionAdmissionDiagnosticCode.InvalidContributionDiagnostic,
                    factPath,
                    $"Contribution diagnostic fact identity '{linkedFact.Value}' must be normalized and scoped beneath producer '{adapterId}:'");
            }
            else if (!admittedFactIds.Contains(linkedFact.Value))
            {
                context.Add(
                    AdapterContributionAdmissionDiagnosticCode.InvalidContributionDiagnostic,
                    factPath,
                    $"Contribution diagnostic references fact '{linkedFact.Value}' that is not part of the contribution");
            }

            if (!linkedFactIds.Add(linkedFact.Value))
            {
                context.Add(
                    AdapterContributionAdmissionDiagnosticCode.InvalidContributionDiagnostic,
                    factPath,
                    $"Contribution diagnostic references fact '{linkedFact.Value}' more than once");
            }
        }

        if (diagnostic.Subject is not null)
        {
            ValidateSubject(diagnostic.Subject, $"{path}.Subject", null, context);
        }

        if (diagnostic.Source is not null)
        {
            ValidateSource(diagnostic.Source, $"{path}.Source", null, diagnostic.Subject, sourceAuthorityValidator, context);
        }
    }

    static void ValidateSource(
        SourceRange source,
        string path,
        FactId? fact,
        SubjectId? subject,
        ISourceAuthorityValidator? sourceAuthorityValidator,
        AdapterContributionAdmissionContext context)
    {
        var isOrdered = source.EndLine > source.StartLine ||
                        (source.EndLine == source.StartLine && source.EndColumn >= source.StartColumn);
        if (!IsPortableRelativePath(source.Path) ||
            source.StartLine < 1 ||
            source.StartColumn < 1 ||
            source.EndLine < 1 ||
            source.EndColumn < 1 ||
            !isOrdered ||
            (source.FileIdentity is not null && !IsFileIdentity(source.FileIdentity)))
        {
            context.Add(
                AdapterContributionAdmissionDiagnosticCode.InvalidSourceRange,
                path,
                $"{path} must identify a normalized portable relative path without rooted, backslash, empty, or dot segments and an ordered positive 1-based range",
                fact,
                subject,
                source);
            return;
        }

        if (sourceAuthorityValidator is null)
        {
            context.Add(
                AdapterContributionAdmissionDiagnosticCode.SourceAuthorityRequired,
                path,
                $"Source range '{source.Path}:{source.StartLine}:{source.StartColumn}' requires a host authority validator",
                fact,
                subject,
                source);
        }
        else if (!sourceAuthorityValidator.IsAuthoritative(source))
        {
            context.Add(
                AdapterContributionAdmissionDiagnosticCode.SourceNotAuthoritative,
                path,
                $"Source range '{source.Path}:{source.StartLine}:{source.StartColumn}' is not authoritative authored source",
                fact,
                subject,
                source);
        }
    }

    static bool IsIdentityPart(string? value) =>
        AdapterContributionText.IsNormalized(value, false);

    static bool IsNormalizedPath(string? value)
    {
        if (!AdapterContributionText.IsNormalized(value, true))
        {
            return false;
        }

        var normalizedPath = value!;
        try
        {
            return string.Equals(normalizedPath, normalizedPath.Normalize(NormalizationForm.FormC), StringComparison.Ordinal);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    static bool IsFileIdentity(SourceFileIdentity identity) =>
        AdapterContributionText.IsNormalized(identity.Project, true) &&
        IsPortableRelativePath(identity.Path);

    static bool IsPortableRelativePath(string? value)
    {
        if (!IsNormalizedPath(value))
        {
            return false;
        }

        var path = value!;
        if (path[0] == '/' ||
            path.Contains('\\') ||
            IsDriveRooted(path))
        {
            return false;
        }

        var segments = path.Split('/');
        for (var index = 0; index < segments.Length; index++)
        {
            if (string.IsNullOrEmpty(segments[index]) ||
                !TryDecodeSegment(segments[index], out var decoded) ||
                string.Equals(decoded, ".", StringComparison.Ordinal) ||
                string.Equals(decoded, "..", StringComparison.Ordinal) ||
                decoded.Contains('/') ||
                decoded.Contains('\\') ||
                (index == 0 && IsDriveRooted(decoded)))
            {
                return false;
            }
        }

        return true;
    }

    static bool TryDecodeSegment(string segment, out string decoded)
    {
        decoded = segment;
        try
        {
            while (true)
            {
                var unescaped = Uri.UnescapeDataString(decoded);
                if (string.Equals(unescaped, decoded, StringComparison.Ordinal))
                {
                    return true;
                }

                decoded = unescaped;
            }
        }
        catch (UriFormatException)
        {
            return false;
        }
    }

    static bool IsDriveRooted(string path) =>
        path.Length >= 2 && char.IsAsciiLetter(path[0]) && path[1] == ':';

    static bool HasAuthoredDotPathSegment(string value)
    {
        var schemeSeparator = value.IndexOf(':', StringComparison.Ordinal);
        var pathStart = schemeSeparator + 1;
        if (value.AsSpan(pathStart).StartsWith("//", StringComparison.Ordinal))
        {
            pathStart = value.IndexOf('/', pathStart + 2);
            if (pathStart < 0)
            {
                return false;
            }
        }

        var pathEnd = value.IndexOfAny(['?', '#'], pathStart);
        var path = pathEnd < 0 ? value[pathStart..] : value[pathStart..pathEnd];
        return path.Split('/').Any(IsDotSegment);
    }

    static bool IsDotSegment(string segment)
    {
        var unescaped = Uri.UnescapeDataString(segment);
        return string.Equals(unescaped, ".", StringComparison.Ordinal) || string.Equals(unescaped, "..", StringComparison.Ordinal);
    }

    static GenerationFactCapability CapabilityFor(GenerationFact fact) => fact switch
    {
        ArtifactFact => GenerationFactCapability.Artifact,
        ArtifactPlacementFact => GenerationFactCapability.ArtifactPlacement,
        RelationshipFact => GenerationFactCapability.Relationship,
        ConceptRepresentationFact => GenerationFactCapability.ConceptRepresentation,
        ConceptAttributeFact => GenerationFactCapability.ConceptAttribute,
        ConceptValidationRuleFact => GenerationFactCapability.ConceptValidationRule,
        SpecificationScenarioFact => GenerationFactCapability.SpecificationScenario,
        SpecificationStepFact => GenerationFactCapability.SpecificationStep,
        SpecificationValueFact => GenerationFactCapability.SpecificationValue,
        ArtifactDeclarationFact => GenerationFactCapability.ArtifactDeclaration,
        ArtifactMemberDeclarationFact => GenerationFactCapability.ArtifactMemberDeclaration,
        ArtifactMemberTypeUseFact => GenerationFactCapability.ArtifactMemberTypeUse,
        TypeUseBindingFact => GenerationFactCapability.TypeUseBinding,
        ArtifactMemberRoleFact => GenerationFactCapability.ArtifactMemberRole,
        _ => GenerationFactCapability.Unknown
    };
}
