// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class BackendScopeFreezer
{
    public static BackendScopeKey? Key(BackendScopeKey? key, string path, AdapterContributionAdmissionContext context) =>
        key is null ? null : new BackendScopeKey
        {
            Store = GranularFactFreezer.FreezeSubject(key.Store, $"{path}.Store", context),
            EventModelName = key.EventModelName
        };

    public static BackendScopeDefinition Definition(BackendScopeDefinition? definition, string path, AdapterContributionAdmissionContext context)
    {
        if (definition?.Key is null)
        {
            context.Missing(definition is null ? path : $"{path}.Key");
        }

        return new BackendScopeDefinition
        {
            Key = Key(definition?.Key, $"{path}.Key", context) ?? new BackendScopeKey { Store = new SubjectId { Value = string.Empty } },
            Name = definition?.Name
        };
    }
}
