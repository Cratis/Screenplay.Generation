// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator.given;

public class co_owned_yielding_concepts : yielding_concepts
{
    protected GenerationFact[] CoOwner(string producer, Func<GenerationDiagnostic, GenerationDiagnostic>? change = null)
    {
        var adapter = new AdapterIdentity { Id = producer, Version = "1.0.0" };
        var facts = Candidate("enum", true, producer: adapter);
        var declaration = (ArtifactDeclarationFact)facts[0];
        var marker = declaration.Definition.NameConflictYield!;
        facts[0] = declaration with
        {
            Definition = declaration.Definition with
            {
                NameConflictYield = marker with { Diagnostic = change is null ? marker.Diagnostic : change(marker.Diagnostic) }
            }
        };
        return facts;
    }

    protected GeneratedScreenplayDefinition GenerateCoOwners(params GenerationFact[][] coOwners) => Generator.Generate(
        Snapshot(
        [
            .. coOwners.Select(facts => Completed(facts[0].Evidence.Adapter, facts)),
            Completed(Adapter, [.. Command(), .. Candidate("vogen", false, true)])
        ]),
        new ScreenplayGenerationOptions { Domain = "Ordering" });
}
