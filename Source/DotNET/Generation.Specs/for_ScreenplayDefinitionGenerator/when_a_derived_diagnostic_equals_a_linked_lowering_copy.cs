// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_a_derived_diagnostic_equals_a_linked_lowering_copy : given.snapshot_omissions
{
    GeneratedScreenplayDefinition _baseline = null!;
    GeneratedScreenplayDefinition _result = null!;
    GenerationDiagnostic _original = null!;
    GenerationDiagnostic _derived = null!;

    void Because()
    {
        var declared = DeclaredDocument("declared-1", Document);
        _baseline = GenerateSnapshot(declared);
        _original = Of(_baseline.AdapterRun.Diagnostics, GenerationDiagnosticCodes.UnsupportedArtifact).Single();
        _derived = _original with { Facts = [.. _original.Facts.Append(new FactId { Value = "placement" }).Distinct().OrderBy(link => link.Value, StringComparer.Ordinal)] };
        _result = GenerateSnapshot([.. declared, .. Omission(_derived)]);
    }

    [Fact] void should_publish_the_unlinked_lowering_original() => _result.AdapterRun.Diagnostics.Any(diagnostic => Same(diagnostic, _original)).ShouldBeTrue();
    [Fact] void should_publish_the_derived_diagnostic_linked_to_the_placement() => _result.AdapterRun.Diagnostics.Any(diagnostic => Same(diagnostic, _derived)).ShouldBeTrue();
    [Fact] void should_publish_both_as_distinct_diagnostics() => Of(_result.AdapterRun.Diagnostics, GenerationDiagnosticCodes.UnsupportedArtifact).Length.ShouldEqual(2);

    static bool Same(GenerationDiagnostic actual, GenerationDiagnostic expected) =>
        actual.Code == expected.Code && actual.Message == expected.Message && actual.Facts.Select(link => link.Value).SequenceEqual(expected.Facts.Select(link => link.Value));

    GenerationFact[] Omission(GenerationDiagnostic diagnostic)
    {
        var owner = new ArtifactKey { Subject = SubjectOf("Commands.Observe"), Kind = ArtifactKind.Command };
        var member = new ArtifactMemberKey { Artifact = owner, Name = "value" };
        var evidence = At(3, 1, 60);
        var observed = SubjectOf("Concepts.Observed");
        return
        [
            new ArtifactDeclarationFact { Id = new FactId { Value = "command" }, Subject = owner.Subject, Evidence = evidence, Definition = new ArtifactDeclarationDefinition { Artifact = owner, Name = "Observe" } },
            new ArtifactPlacementFact
            {
                Id = new FactId { Value = "command-placement" }, Subject = owner.Subject, Evidence = evidence, Artifact = owner,
                Placement = new ArtifactPlacement { Module = "Students", Slice = "Observing", SliceKind = GenerationSliceKind.StateChange }
            },
            new ArtifactMemberDeclarationFact { Id = new FactId { Value = "member" }, Subject = owner.Subject, Evidence = evidence, Definition = new ArtifactMemberDeclarationDefinition { Member = member, DeclarationOrder = 0 } },
            new ArtifactMemberTypeUseFact
            {
                Id = new FactId { Value = "type-use" }, Subject = owner.Subject, Evidence = evidence,
                Definition = new ArtifactMemberTypeUseDefinition { Member = member, Type = new TypeUseDefinition { Name = "Int", ObservedTypeSubject = observed, Shape = [TypeUseShapeKind.Named] } }
            },
            new TypeUseTargetOmittedFact
            {
                Id = new FactId { Value = "omission" }, Subject = owner.Subject, Evidence = evidence,
                Definition = new TypeUseTargetOmittedDefinition { Member = member, Diagnostic = diagnostic }
            }
        ];
    }
}
