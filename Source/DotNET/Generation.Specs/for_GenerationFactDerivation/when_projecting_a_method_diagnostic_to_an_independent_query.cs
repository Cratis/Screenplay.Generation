// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_projecting_a_method_diagnostic_to_an_independent_query : given.diagnostic_projection
{
    GenerationDiagnostic _diagnostic;

    void Because()
    {
        var intent = Intent();
        intent = intent with { Definition = intent.Definition with { EntryPointRole = ArtifactKind.Query } };
        _diagnostic = Projected(Derive(Observation(), intent, Support(), Classification(), Entry(artifact: Command with { Kind = ArtifactKind.Query })));
    }

    [Fact] void should_emit_the_adapter_payload() => _diagnostic.Code.ShouldEqual(Payload().Code);
    [Fact] void should_retarget_only_the_subject() => _diagnostic.Subject.ShouldEqual(CommandSubject);
}
