// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Screenplay.Generation;

static class ArtifactMemberRoleSelectionStructure
{
    public static string Group(ArtifactMemberRoleSelectionDefinition definition) => Structural.SemanticKey("role-selection-group", Artifact(definition.Artifact), ((int)definition.Role).ToString(CultureInfo.InvariantCulture));

    public static string Definition(ArtifactMemberRoleSelectionDefinition? definition) => definition is null ? Structural.SemanticKey("invalid-role-selection") : Structural.SemanticKey(
        "role-selection",
        Group(definition),
        definition.Member is null ? "no-member" : Structural.SemanticKey("member", Artifact(definition.Member.Artifact), definition.Member.Name),
        definition.Priority.ToString(CultureInfo.InvariantCulture),
        definition.ObservationOrder.ToString(CultureInfo.InvariantCulture),
        definition.Diagnostics is null ? "invalid-diagnostics" : Structural.SemanticKey("diagnostics", [.. definition.Diagnostics.Select(Diagnostic).Order(StringComparer.Ordinal)]));

    static string Diagnostic(GenerationDiagnostic? diagnostic)
    {
        if (diagnostic is null)
        {
            return Structural.SemanticKey("null-diagnostic");
        }

        if (ArtifactMemberRoleSelectionDiagnosticValidation.CanTraverse(diagnostic))
        {
            return Structural.Diagnostic(diagnostic);
        }

        return Structural.SemanticKey(
            "invalid-role-selection-diagnostic",
            Structural.Diagnostic(diagnostic with { Facts = [], Lineage = null }),
            diagnostic.Facts.IsDefault ? "default-facts" : Structural.SemanticKey("facts", [.. diagnostic.Facts.Select(id => id?.Value)]),
            diagnostic.Lineage is null ? null : "invalid-lineage");
    }

    static string Artifact(ArtifactKey? key) => key is null ? Structural.SemanticKey("missing-artifact") : Structural.SemanticKey("artifact", key.Subject?.Value, ((int)key.Kind).ToString(CultureInfo.InvariantCulture), key.Scope is null ? null : Structural.SemanticKey("scope", key.Scope.Store?.Value, key.Scope.EventModelName));
}
