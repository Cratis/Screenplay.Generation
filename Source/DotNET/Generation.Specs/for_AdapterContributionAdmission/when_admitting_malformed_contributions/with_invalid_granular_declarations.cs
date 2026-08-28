// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission.when_admitting_malformed_contributions;

public class with_invalid_granular_declarations : given.a_contribution
{
    AdapterContributionAdmissionResult _invalid = null!;
    AdapterContributionAdmissionResult _undefinedShape = null!;

    void Because()
    {
        var facts = EveryFact();
        var declaration = (ArtifactDeclarationFact)facts[2];
        facts[2] = declaration with
        {
            Definition = declaration.Definition with { Name = " " }
        };
        var member = (ArtifactMemberDeclarationFact)facts[3];
        facts[3] = member with
        {
            Definition = member.Definition with { DeclarationOrder = -1 }
        };
        var typeUse = (ArtifactMemberTypeUseFact)facts[4];
        facts[4] = typeUse with
        {
            Definition = typeUse.Definition with
            {
                Type = typeUse.Definition.Type with
                {
                    Name = " ",
                    ObservedTypeSubject = new SubjectId { Value = "CustomerCode" },
                    Shape = [TypeUseShapeKind.Named, TypeUseShapeKind.Optional]
                }
            }
        };
        var binding = (TypeUseBindingFact)facts[5];
        facts[5] = binding with
        {
            Definition = binding.Definition with
            {
                Target = binding.Definition.Target with { Kind = ArtifactKind.Unknown }
            }
        };
        var role = (ArtifactMemberRoleFact)facts[6];
        facts[6] = role with
        {
            Definition = role.Definition with { Role = ArtifactMemberRoleKind.Unknown }
        };
        _invalid = Admit(contribution: Contribution(facts));

        var undefinedFacts = EveryFact();
        var undefinedTypeUse = (ArtifactMemberTypeUseFact)undefinedFacts[4];
        undefinedFacts[4] = undefinedTypeUse with
        {
            Definition = undefinedTypeUse.Definition with
            {
                Type = undefinedTypeUse.Definition.Type with { Shape = [(TypeUseShapeKind)731] }
            }
        };
        _undefinedShape = Admit(contribution: Contribution(undefinedFacts));
    }

    [Fact] void should_reject_the_complete_invalid_contribution() => _invalid.Snapshot.ShouldBeNull();
    [Fact] void should_reject_blank_declaration_and_type_names() => _invalid.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(AdapterContributionAdmissionDiagnosticCode.MissingRequiredValue);
    [Fact] void should_reject_negative_member_order() => _invalid.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(AdapterContributionAdmissionDiagnosticCode.InvalidDeclarationOrder);
    [Fact] void should_reject_malformed_observed_type_subjects() => _invalid.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(AdapterContributionAdmissionDiagnosticCode.InvalidSubject);
    [Fact] void should_reject_nonterminal_named_type_shapes() => _invalid.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(AdapterContributionAdmissionDiagnosticCode.InvalidTypeUseShape);
    [Fact] void should_reject_unknown_target_and_member_roles() => _invalid.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(AdapterContributionAdmissionDiagnosticCode.UnknownEnumValue);
    [Fact] void should_reject_undefined_type_shape_nodes() => _undefinedShape.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(AdapterContributionAdmissionDiagnosticCode.UndefinedEnumValue);
}
