// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_an_observed_subject_has_conflicting_declarations : given.a_derivation
{
    GenerationDerivationSnapshot _roles = null!;
    GenerationDerivationSnapshot _variants = null!;

    void Because()
    {
        var concept = Concept(ConceptSubject, "concept-target");
        var composite = concept with
        {
            Id = new FactId { Value = "concepts:composite-target" },
            Definition = concept.Definition with
            {
                Key = concept.Definition.Key with { Kind = ArtifactKind.CompositeType }
            }
        };
        var alternate = concept with
        {
            Id = new FactId { Value = "concepts:alternate-target" },
            Definition = concept.Definition with { File = "Concepts/AlternateCustomerCode.cs" }
        };
        var common = new GenerationFact[]
        {
            CommandDeclaration(),
            MemberDeclaration("customerCode", 0, "member"),
            TypeUse("customerCode", ConceptSubject, "type-use")
        };
        _roles = Derive([.. common, concept, composite]);
        _variants = Derive([.. common, concept, alternate]);
    }

    [Fact] void should_not_choose_one_artifact_role() => _roles.Facts.ShouldBeEmpty();
    [Fact] void should_report_incompatible_target_roles() => Codes(_roles).ShouldContain(GenerationDiagnosticCodes.ConflictingTypeUseTarget);
    [Fact] void should_not_choose_one_declaration_variant() => _variants.Facts.ShouldBeEmpty();
    [Fact] void should_report_incompatible_target_declarations() => Codes(_variants).ShouldContain(GenerationDiagnosticCodes.ConflictingTypeUseDeclaration);

    static IEnumerable<string> Codes(GenerationDerivationSnapshot snapshot) =>
        snapshot.Diagnostics.Select(diagnostic => diagnostic.Code);
}
