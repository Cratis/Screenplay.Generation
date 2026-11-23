// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_exact_conflicts_have_agreeing_projection_views : given.compatibility_type_uses
{
    GeneratedScreenplayDefinition _wrappers = null!;
    GeneratedScreenplayDefinition _subjects = null!;
    GeneratedScreenplayDefinition _reverse = null!;
    GenerationFact[] _facts = null!;

    void Because()
    {
        _facts = ConflictingFacts(false);
        _wrappers = Generate(_facts);
        _reverse = Generate(_facts.Reverse());
        _subjects = Generate(ConflictingFacts(true));
    }

    [Fact] void should_have_identical_printable_views() => _facts.OfType<TypeUseCompatibilityProjectionFact>().Select(view => Projection(view.Definition.Type)).Distinct().Count().ShouldEqual(1);
    [Fact] void should_reject_distinct_exact_wrapper_orders() => HasExactConflict(_wrappers).ShouldBeTrue();
    [Fact] void should_reject_distinct_exact_terminal_subjects() => HasExactConflict(_subjects).ShouldBeTrue();
    [Fact] void should_not_emit_a_winning_projected_member() => _wrappers.Source.ShouldNotContain("permissions List[]");
    [Fact] void should_keep_both_exact_observations() => _wrappers.AdapterRun!.Facts.Count(record => record.Fact is ArtifactMemberTypeUseFact).ShouldEqual(2);
    [Fact] void should_locate_both_original_assertions() => _wrappers.Diagnostics.Where(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.ConflictingMemberTypeUse).SelectMany(diagnostic => diagnostic.Facts).Distinct().Count().ShouldEqual(2);
    [Fact] void should_keep_reversed_source_equal() => _reverse.Source.ShouldEqual(_wrappers.Source);
    [Fact] void should_keep_reversed_diagnostics_equal() => Projection(_reverse.Diagnostics).ShouldEqual(Projection(_wrappers.Diagnostics));
    [Fact] void should_keep_reversed_lineage_equal() => Projection(_reverse.AdapterRun!.Derivation).ShouldEqual(Projection(_wrappers.AdapterRun!.Derivation));

    GenerationFact[] ConflictingFacts(bool changeSubject)
    {
        var facts = ProjectedCommand();
        var original = facts.OfType<ArtifactMemberTypeUseFact>().Single();
        var type = original.Definition.Type;
        var subject = new SubjectId { Value = "dotnet://Ordering/OtherTerminal" };
        var altered = changeSubject
            ? type with { ObservedTypeSubject = subject, Nodes = [type.Nodes![0], type.Nodes[1], type.Nodes[2] with { Subject = subject }] }
            : type with { Shape = [TypeUseShapeKind.Collection, TypeUseShapeKind.Collection, TypeUseShapeKind.Optional, TypeUseShapeKind.Named], Nodes = [type.Nodes![0], type.Nodes[1], type.Nodes[2], type.Nodes[2]] };
        var alternate = original with { Id = Id("alternate-observation"), Definition = original.Definition with { Type = altered } };
        return [.. facts, alternate, View(alternate)];
    }

    static bool HasExactConflict(GeneratedScreenplayDefinition result) => !result.IsSuccess && result.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.ConflictingMemberTypeUse);
}
