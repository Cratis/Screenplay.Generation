// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation.when_binding_concept_only_type_uses;

public class with_unqualified_backend_scope_collisions : given.scoped_type_uses
{
    GenerationFact[] _facts;
    GenerationDerivationSnapshot _result;

    void Establish()
    {
        var facts = Facts(null);
        var observation = facts.OfType<ArtifactMemberTypeUseFact>().Single();
        _facts =
        [
            .. facts,
            new TypeUseBindingPolicyFact
            {
                Id = Id("concept-only"), Subject = observation.Subject, Evidence = observation.Evidence,
                Definition = new() { TypeUse = observation.Id, Policy = TypeUseBindingPolicyKind.ConceptOnlyWithUnboundFallback }
            }
        ];
    }

    void Because() => _result = Derive(_facts);

    [Fact] void should_not_select_a_backend_occurrence() => _result.Facts.ShouldBeEmpty();
    [Fact] void should_keep_the_ambiguous_scope_error() => _result.Diagnostics.Single().Code.ShouldEqual(GenerationDiagnosticCodes.AmbiguousBackendScopeOwnership);
    [Fact] void should_keep_the_conflicting_outcome() => _result.Diagnostics.Single().Outcome.ShouldEqual(GenerationDiagnosticOutcome.Conflict);
    [Fact] void should_locate_the_collision() => _result.Diagnostics.Single().Source.ShouldEqual(Evidence().Source);
    [Fact] void should_link_both_occurrences() => _result.Diagnostics.Single().Facts.Intersect([Id("first-model"), Id("second-model")]).Count().ShouldEqual(2);
}
