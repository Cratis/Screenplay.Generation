// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_admitting_concept_name_yielding : given.a_contribution
{
    AdapterContributionAdmissionResult _valid = null!;
    AdapterContributionAdmissionResult _missingDiagnostic = null!;
    AdapterContributionAdmissionResult _foreignProducer = null!;
    AdapterContributionAdmissionResult _foreignDeclaration = null!;
    AdapterContributionAdmissionResult _foreignSubject = null!;
    AdapterContributionAdmissionResult _notConcept = null!;
    AdapterContributionAdmissionResult _missingOutcome = null!;
    AdapterContributionAdmissionResult _conflictingOutcome = null!;
    ArtifactDeclarationFact _declaration = null!;

    void Because()
    {
        _declaration = Declaration();
        _valid = Admit(contribution: Contribution([_declaration]));
        _missingDiagnostic = Admit(contribution: Contribution([_declaration with { Definition = _declaration.Definition with { NameConflictYield = new ConceptNameYieldDefinition { Diagnostic = null! } } }]));
        _foreignProducer = Admit(contribution: Contribution([_declaration with { Evidence = Evidence() with { Adapter = new AdapterIdentity { Id = "foreign", Version = "1.0.0" } } }]));
        _foreignDeclaration = WithDiagnostic(_declaration.Definition.NameConflictYield!.Diagnostic with { Facts = [Id("not-in-this-contribution")] });
        _foreignSubject = WithDiagnostic(_declaration.Definition.NameConflictYield.Diagnostic with { Subject = ExternalSubject });
        _notConcept = Admit(contribution: Contribution([_declaration with { Definition = _declaration.Definition with { Artifact = ArtifactKey(ArtifactSubject, ArtifactKind.Command) } }]));
        _missingOutcome = WithDiagnostic(_declaration.Definition.NameConflictYield.Diagnostic with { Outcome = null });
        _conflictingOutcome = WithDiagnostic(_declaration.Definition.NameConflictYield.Diagnostic with { Outcome = GenerationDiagnosticOutcome.Conflict });
    }

    [Fact] void should_admit_the_producer_owned_marker() => _valid.IsAdmitted.ShouldBeTrue();
    [Fact] void should_freeze_the_marker() => ReferenceEquals(_valid.Snapshot!.Facts.OfType<ArtifactDeclarationFact>().Single().Definition.NameConflictYield, _declaration.Definition.NameConflictYield).ShouldBeFalse();
    [Fact] void should_freeze_the_diagnostic() => ReferenceEquals(_valid.Snapshot!.Facts.OfType<ArtifactDeclarationFact>().Single().Definition.NameConflictYield!.Diagnostic, _declaration.Definition.NameConflictYield!.Diagnostic).ShouldBeFalse();
    [Fact] void should_keep_the_source_location() => _valid.Snapshot!.Facts.OfType<ArtifactDeclarationFact>().Single().Definition.NameConflictYield!.Diagnostic.Source.ShouldEqual(Source());
    [Fact] void should_reject_a_missing_diagnostic() => _missingDiagnostic.IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_another_producers_declaration() => _foreignProducer.IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_a_declaration_outside_the_contribution() => _foreignDeclaration.IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_another_subjects_diagnostic() => _foreignSubject.IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_markers_on_non_concepts() => _notConcept.IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_a_missing_diagnostic_outcome() => _missingOutcome.IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_a_conflicting_diagnostic_outcome() => _conflictingOutcome.IsAdmitted.ShouldBeFalse();
    [Fact] void should_preserve_the_diagnostic_outcome() => _valid.Snapshot!.Facts.OfType<ArtifactDeclarationFact>().Single().Definition.NameConflictYield!.Diagnostic.Outcome.ShouldEqual(GenerationDiagnosticOutcome.Unsupported);

    AdapterContributionAdmissionResult WithDiagnostic(GenerationDiagnostic diagnostic) => Admit(contribution: Contribution(
    [
        _declaration with { Definition = _declaration.Definition with { NameConflictYield = new ConceptNameYieldDefinition { Diagnostic = diagnostic } } }
    ]));

    static ArtifactDeclarationFact Declaration() => new()
    {
        Id = Id("concept"),
        Subject = ArtifactSubject,
        Evidence = Evidence(),
        Definition = new ArtifactDeclarationDefinition
        {
            Artifact = ArtifactKey(ArtifactSubject, ArtifactKind.Concept),
            Name = "Status",
            NameConflictYield = new ConceptNameYieldDefinition
            {
                Diagnostic = new GenerationDiagnostic
                {
                    Code = "example.EnumConceptNameConflict",
                    Severity = GenerationDiagnosticSeverity.Warning,
                    Outcome = GenerationDiagnosticOutcome.Unsupported,
                    Message = "The concept yields when its name conflicts",
                    Subject = ArtifactSubject,
                    Source = Source(),
                    Facts = [Id("concept")]
                }
            }
        }
    };
}
