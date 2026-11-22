// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_a_referenced_scope_has_conflicting_proof : given.scoped_type_uses
{
    GenerationDerivationSnapshot _result;

    void Because()
    {
        var conflicting = Scope(First, "conflict");
        _result = Derive([.. Facts(Second), conflicting with { Definition = conflicting.Definition with { Name = "Other store metadata" } }]);
    }

    [Fact] void should_not_derive_from_invalid_scope_ownership() => _result.Facts.ShouldBeEmpty();
    [Fact] void should_link_the_conflicting_scope_proofs() => _result.Diagnostics.Single().Code.ShouldEqual(GenerationDiagnosticCodes.ConflictingBackendScope);
}
