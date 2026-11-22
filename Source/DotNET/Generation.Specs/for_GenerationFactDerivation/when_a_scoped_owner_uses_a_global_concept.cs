// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_a_scoped_owner_uses_a_global_concept : given.scoped_type_uses
{
    GenerationDerivationSnapshot _result;

    void Because() => _result = Derive([.. Facts(null).Where(fact => fact is not ArtifactFact), Artifact(Key(ModelSubject, ArtifactKind.Concept, null), "global-concept", "Customer")]);

    [Fact] void should_keep_the_concept_global_instead_of_copying_the_owner_scope() => ((TypeUseBindingFact)_result.Facts.Single().Fact).Definition.Target.Scope.ShouldBeNull();
    [Fact] void should_bind_the_global_concept_role() => ((TypeUseBindingFact)_result.Facts.Single().Fact).Definition.Target.Kind.ShouldEqual(ArtifactKind.Concept);
    [Fact] void should_report_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
}
