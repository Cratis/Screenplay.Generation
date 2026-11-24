// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class DeclarationInputMemberFreezer
{
    public static DeclarationInputMemberDeclarationDefinition Declaration(DeclarationInputMemberDeclarationDefinition? definition, string path, AdapterContributionAdmissionContext context)
    {
        Missing(definition, path, context);
        return new()
        {
            Member = Member(definition?.Member, $"{path}.Member", context),
            DeclarationOrder = definition?.DeclarationOrder ?? -1
        };
    }

    public static DeclarationInputMemberTypeUseDefinition TypeUse(DeclarationInputMemberTypeUseDefinition? definition, string path, AdapterContributionAdmissionContext context)
    {
        Missing(definition, path, context);
        return new()
        {
            Member = Member(definition?.Member, $"{path}.Member", context),
            Type = GranularFactFreezer.FreezeTypeUse(definition?.Type, $"{path}.Type", context)
        };
    }

    public static EntryPointInputTransferDefinition Transfer(EntryPointInputTransferDefinition? definition, string path, AdapterContributionAdmissionContext context)
    {
        Missing(definition, path, context);
        return new()
        {
            Method = GranularFactFreezer.FreezeSubject(definition?.Method, $"{path}.Method", context),
            Artifact = GranularFactFreezer.FreezeArtifactKey(definition?.Artifact, $"{path}.Artifact", context),
            Kind = definition?.Kind ?? RelationshipKind.Unknown
        };
    }

    public static MethodDeclarationExecutionDefinition Execution(MethodDeclarationExecutionDefinition? definition, string path, AdapterContributionAdmissionContext context)
    {
        Missing(definition, path, context);
        return new()
        {
            Method = GranularFactFreezer.FreezeSubject(definition?.Method, $"{path}.Method", context),
            Declaration = GranularFactFreezer.FreezeSubject(definition?.Declaration, $"{path}.Declaration", context),
            SourceScope = BackendScopeFreezer.Key(definition?.SourceScope, $"{path}.SourceScope", context),
            TargetScope = BackendScopeFreezer.Key(definition?.TargetScope, $"{path}.TargetScope", context),
            ExecutionOrder = definition?.ExecutionOrder
        };
    }

    public static DeclarationMemberKey Member(DeclarationMemberKey? key, string path, AdapterContributionAdmissionContext context)
    {
        Missing(key, path, context);
        return new()
        {
            Declaration = GranularFactFreezer.FreezeSubject(key?.Declaration, $"{path}.Declaration", context),
            Scope = BackendScopeFreezer.Key(key?.Scope, $"{path}.Scope", context),
            Name = key?.Name ?? string.Empty
        };
    }

    static void Missing(object? value, string path, AdapterContributionAdmissionContext context)
    {
        if (value is null)
        {
            context.Missing(path);
        }
    }
}
