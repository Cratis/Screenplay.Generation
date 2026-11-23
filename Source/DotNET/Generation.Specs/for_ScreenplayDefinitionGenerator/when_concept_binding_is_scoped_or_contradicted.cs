// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_concept_binding_is_scoped_or_contradicted : given.concept_only_type_uses
{
    GeneratedScreenplayDefinition _wrongScope;
    GeneratedScreenplayDefinition _contradiction;
    GeneratedScreenplayDefinition _generic;
    GeneratedScreenplayDefinition _malformed;
    GeneratedScreenplayDefinition _unsupportedShape;

    void Because()
    {
        var facts = OptedCommand();
        var typeUse = facts.OfType<ArtifactMemberTypeUseFact>().Single();
        var scope = new BackendScopeKey { Store = new() { Value = "dotnet://Ordering/Store" } };
        var scopedType = typeUse with { Definition = typeUse.Definition with { Member = typeUse.Definition.Member with { Artifact = typeUse.Definition.Member.Artifact with { Scope = scope } }, Type = typeUse.Definition.Type with { ObservedScope = scope } } };
        _wrongScope = Generate([.. facts.Where(fact => fact is not ArtifactMemberTypeUseFact).Select(fact => WithScope(fact, scope)), scopedType, .. Candidate("enum", false, true), new BackendScopeFact
        {
            Id = Id("scope"), Subject = scope.Store, Evidence = typeUse.Evidence,
            Definition = new() { Key = scope, Name = "Ordering" }
        }]);
        _contradiction = Generate([.. facts, NonConcept(), new TypeUseBindingFact
        {
            Id = Id("explicit-binding"), Subject = typeUse.Subject, Evidence = typeUse.Evidence,
            Definition = new() { Member = typeUse.Definition.Member, Target = NonConcept().Definition.Artifact }
        }]);
        _generic = Generate(Command());
        _unsupportedShape = Generate(OptedCommand(TypeUseShapeKind.Collection, TypeUseShapeKind.Collection, TypeUseShapeKind.Named));
        var policy = Policy(typeUse);
        _malformed = Generate([.. Command(), policy with { Definition = policy.Definition with { TypeUse = Id("missing") } }]);
    }

    [Fact] void should_keep_the_wrong_scope_owner_as_a_plain_type() => _wrongScope.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_bind_the_global_concept_from_a_scoped_observation() => Member(_wrongScope).Type.Subject.ShouldBeNull();
    [Fact] void should_retain_the_exact_observed_scope() => _wrongScope.AdapterRun.Facts.Select(record => record.Fact).OfType<ArtifactMemberTypeUseFact>().Single().Definition.Type.ObservedScope.ShouldNotBeNull();
    [Fact] void should_report_the_explicit_non_concept_contradiction() => _contradiction.Diagnostics.Any(diagnostic => diagnostic.Outcome == GenerationDiagnosticOutcome.Conflict).ShouldBeTrue();
    [Fact] void should_not_emit_the_contradicted_owner() => _contradiction.Source.ShouldNotContain("command SetPermissions");
    [Fact] void should_not_change_generic_missing_target_behavior() => _generic.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.MissingTypeUseTarget).ShouldBeTrue();
    [Fact] void should_keep_unsupported_shape_diagnostics() => _unsupportedShape.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.UnsupportedTypeUseShape).ShouldBeTrue();
    [Fact] void should_reject_a_malformed_raw_policy() => _malformed.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.InvalidTypeUseBindingPolicy).ShouldBeTrue();
    [Fact] void should_not_let_a_malformed_policy_waive_the_missing_target() => _malformed.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.MissingTypeUseTarget).ShouldBeTrue();

    static GenerationFact WithScope(GenerationFact fact, BackendScopeKey scope) => fact switch
    {
        ArtifactDeclarationFact declaration => declaration with { Definition = declaration.Definition with { Artifact = declaration.Definition.Artifact with { Scope = scope } } },
        ArtifactPlacementFact placement => placement with { Artifact = placement.Artifact with { Scope = scope } },
        ArtifactMemberDeclarationFact member => member with { Definition = member.Definition with { Member = member.Definition.Member with { Artifact = member.Definition.Member.Artifact with { Scope = scope } } } },
        _ => fact
    };
}
