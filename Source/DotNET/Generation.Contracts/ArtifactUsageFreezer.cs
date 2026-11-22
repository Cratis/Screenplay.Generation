// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class ArtifactUsageFreezer
{
    public static ArtifactUsageDeclarationDefinition Declaration(ArtifactUsageDeclarationDefinition? definition, string path, AdapterContributionAdmissionContext context)
    {
        MissingDefinition(definition, path, context);
        return new ArtifactUsageDeclarationDefinition
        {
            Declaration = GranularFactFreezer.FreezeSubject(definition?.Declaration, $"{path}.Declaration", context),
            Artifact = GranularFactFreezer.FreezeArtifactKey(definition?.Artifact, $"{path}.Artifact", context)
        };
    }

    public static EntryPointDefinition EntryPoint(EntryPointDefinition? definition, string path, AdapterContributionAdmissionContext context)
    {
        MissingDefinition(definition, path, context);
        return new EntryPointDefinition
        {
            Method = GranularFactFreezer.FreezeSubject(definition?.Method, $"{path}.Method", context),
            Artifact = GranularFactFreezer.FreezeArtifactKey(definition?.Artifact, $"{path}.Artifact", context)
        };
    }

    public static MethodArtifactUsageDefinition Usage(MethodArtifactUsageDefinition? definition, string path, AdapterContributionAdmissionContext context)
    {
        MissingDefinition(definition, path, context);
        return new MethodArtifactUsageDefinition
        {
            Method = GranularFactFreezer.FreezeSubject(definition?.Method, $"{path}.Method", context),
            Declaration = GranularFactFreezer.FreezeSubject(definition?.Declaration, $"{path}.Declaration", context),
            Kind = definition?.Kind ?? RelationshipKind.Unknown,
            SourceScope = BackendScopeFreezer.Key(definition?.SourceScope, $"{path}.SourceScope", context),
            TargetScope = BackendScopeFreezer.Key(definition?.TargetScope, $"{path}.TargetScope", context)
        };
    }

    static void MissingDefinition(object? definition, string path, AdapterContributionAdmissionContext context)
    {
        if (definition is null)
        {
            context.Missing(path);
        }
    }
}
