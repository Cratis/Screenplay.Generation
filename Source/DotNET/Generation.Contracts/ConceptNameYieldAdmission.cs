// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class ConceptNameYieldAdmission
{
    public static ConceptNameYieldDefinition? Freeze(ConceptNameYieldDefinition? marker, string path, AdapterContributionAdmissionContext context)
    {
        if (marker is null)
        {
            return null;
        }

        if (marker.Diagnostic is null)
        {
            context.Missing($"{path}.Diagnostic");
        }

        return new ConceptNameYieldDefinition
        {
            Diagnostic = AdapterContributionFreezer.FreezeMethodDiagnostic(
                marker.Diagnostic ?? new GenerationDiagnostic { Code = string.Empty, Severity = GenerationDiagnosticSeverity.Unknown, Message = string.Empty },
                $"{path}.Diagnostic",
                context)
        };
    }

    public static void Validate(IReadOnlyList<GenerationFact> facts, string adapter, ISourceAuthorityValidator? authority, AdapterContributionAdmissionContext context)
    {
        var ids = facts.Select(fact => fact.Id.Value).ToHashSet(StringComparer.Ordinal);
        foreach (var declaration in facts.OfType<ArtifactDeclarationFact>().Where(fact => fact.Definition.NameConflictYield is not null))
        {
            var diagnostic = declaration.Definition.NameConflictYield!.Diagnostic;
            if (declaration.Definition.Artifact.Kind != ArtifactKind.Concept || diagnostic.Subject != declaration.Subject || !diagnostic.Facts.Contains(declaration.Id))
            {
                context.Add(
                    AdapterContributionAdmissionDiagnosticCode.OwnershipMismatch,
                    $"Contribution.Facts[{declaration.Id}].Definition.NameConflictYield",
                    "Name-conflict yielding belongs to this concept declaration and its diagnostic must identify the declaration and subject",
                    declaration.Id,
                    declaration.Subject);
            }

            AdapterContributionAdmissionValidator.ValidateMethodDiagnostic((adapter, ids, authority), diagnostic, context);
        }
    }
}
