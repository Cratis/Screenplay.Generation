// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.Vogen.for_VogenConceptScreenplayAdapter;

public class when_vogen_behavior_cannot_be_represented : given.a_vogen_compilation
{
    AdapterContribution _contribution = null!;

    void Because()
    {
        var compilation = CompilationFrom(
            "Concepts",
            new SourceFile(
                "/workspace/Concepts/SpecialValues.cs",
                """
                namespace Concepts;
                [Vogen.Instance("Unknown", "?")]
                [Vogen.Instance("NotApplicable", "N/A")]
                [Vogen.ValueObject<string>]
                public partial struct CustomerCode
                {
                    private static string NormalizeInput(string value) => value.Trim().ToUpperInvariant();
                }
                [Vogen.ValueObject<System.Guid>]
                public partial struct OrderId;
                """));

        _contribution = Analyze(Project("Concepts.Project", compilation));
    }

    [Fact] void should_not_treat_normalization_as_validation() => _contribution.Facts.OfType<ConceptValidationRuleFact>().ShouldBeEmpty();
    [Fact] void should_report_normalization_loss() => _contribution.Diagnostics.Count(_ => _.Code == VogenGenerationDiagnosticCodes.InputNormalizationNotRepresented).ShouldEqual(1);
    [Fact] void should_anchor_normalization_loss_at_the_authored_method() => _contribution.Diagnostics.Single(_ => _.Code == VogenGenerationDiagnosticCodes.InputNormalizationNotRepresented).Source!.StartLine.ShouldEqual(7);
    [Fact] void should_report_each_named_instance_loss() => _contribution.Diagnostics.Count(_ => _.Code == VogenGenerationDiagnosticCodes.NamedInstanceNotRepresented).ShouldEqual(2);
    [Fact] void should_preserve_named_instance_evidence_order() => _contribution.Diagnostics.Where(_ => _.Code == VogenGenerationDiagnosticCodes.NamedInstanceNotRepresented).Select(_ => _.Source!.StartLine).ShouldEqual([2, 3]);
    [Fact] void should_identify_each_named_instance() => _contribution.Diagnostics.Where(_ => _.Code == VogenGenerationDiagnosticCodes.NamedInstanceNotRepresented).Select(_ => _.Message).ShouldContainOnly(
        "Vogen concept 'CustomerCode' declares named instance 'Unknown'; Screenplay generation does not treat named instances as optional values or defaults and no concept fact was contributed for it",
        "Vogen concept 'CustomerCode' declares named instance 'NotApplicable'; Screenplay generation does not treat named instances as optional values or defaults and no concept fact was contributed for it");
    [Fact] void should_emit_only_warning_loss_diagnostics() => _contribution.Diagnostics.All(_ => _.Severity == GenerationDiagnosticSeverity.Warning).ShouldBeTrue();
    [Fact] void should_keep_loss_diagnostics_on_the_exact_concept() => _contribution.Diagnostics.All(_ => _.Subject == ConceptNamed(_contribution, "CustomerCode").Subject).ShouldBeTrue();
    [Fact] void should_not_treat_named_instances_as_concept_properties() => ConceptNamed(_contribution, "CustomerCode").Definition.Properties.ShouldBeEmpty();
    [Fact] void should_not_infer_identity_from_guid_or_id_naming() => _contribution.Facts.OfType<ArtifactFact>().SelectMany(_ => _.Definition.Properties).Any(_ => _.IsIdentifier).ShouldBeFalse();
}
