// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_lowering_concepts_to_fixed_source : given.a_lowering_fixture
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        var uuid = Concept("OrderId", GenerationPrimitiveKind.Uuid);
        var text = Concept("AccountNumber", GenerationPrimitiveKind.Text, "Concepts/Account.Number.cs");
        var whole = Concept("Quantity", GenerationPrimitiveKind.WholeNumber);
        var number = Concept("Amount", GenerationPrimitiveKind.Number);
        var boolean = Concept("IsActive", GenerationPrimitiveKind.Boolean);
        var date = Concept("OpenedOn", GenerationPrimitiveKind.Date);
        var dateTime = Concept("OpenedAt", GenerationPrimitiveKind.DateTime);
        var status = SubjectOf("Concepts.Status");
        var statusEvidence = EvidenceAt("Concepts/Status.cs", 2);
        GenerationFact[] enumeration =
        [
            new ArtifactFact
            {
                Id = new FactId { Value = "concept:Status" },
                Subject = status,
                Definition = new ArtifactDefinition
                {
                    Key = new ArtifactKey { Subject = status, Kind = ArtifactKind.Concept },
                    Name = "Status",
                    File = "Concepts\\Status.cs"
                },
                Evidence = statusEvidence
            },
            new ConceptRepresentationFact
            {
                Id = new FactId { Value = "concept-representation:Status" },
                Subject = status,
                Definition = new ConceptRepresentationDefinition
                {
                    Concept = status,
                    Kind = ConceptRepresentationKind.Enumeration,
                    EnumerationValues = ["Open", "Closed", "OnHold"]
                },
                Evidence = statusEvidence
            }
        ];
        GenerationFact[] attributes =
        [
            new ConceptAttributeFact
            {
                Id = new FactId { Value = "attribute:later" },
                Subject = text.Subject,
                Definition = new ConceptAttributeDefinition { Concept = text.Subject, Name = "sensitive", Reason = "Customer identifier" },
                Evidence = EvidenceAt("Concepts/AccountNumber.cs", 4)
            },
            new ConceptAttributeFact
            {
                Id = new FactId { Value = "attribute:earlier" },
                Subject = text.Subject,
                Definition = new ConceptAttributeDefinition { Concept = text.Subject, Name = "pii" },
                Evidence = EvidenceAt("Concepts/AccountNumber.cs", 5)
            }
        ];
        GenerationFact[] rules =
        [
            Validation("rule-b", text.Subject, "b-format", "BeValidFormat", "Format is wrong", "Concepts\\Rules.cs"),
            Validation("rule-a", text.Subject, "a-length", "HaveLength")
        ];

        _result = Generator.Generate(
            [Contribution([.. uuid.Facts, .. text.Facts, .. whole.Facts, .. number.Facts, .. boolean.Facts, .. date.Facts, .. dateTime.Facts, .. enumeration, .. attributes, .. rules])],
            new ScreenplayGenerationOptions { Domain = "Banking" });
    }

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();

    [Fact]
    void should_emit_the_complete_fixed_source() => _result.Source.ReplaceLineEndings("\n").ShouldEqual(
        """
        domain Banking

        concept AccountNumber : String pii secret
          file Concepts/Account.Number.cs
          secret reason "Customer identifier"
          validate
            rule HaveLength
            rule BeValidFormat message "Format is wrong"
              file Concepts/Rules.cs

        concept Amount : Decimal
          file Concepts/Amount.cs

        concept IsActive : Bool
          file Concepts/IsActive.cs

        concept OpenedAt : DateTime
          file Concepts/OpenedAt.cs

        concept OpenedOn : Date
          file Concepts/OpenedOn.cs

        concept OrderId : Uuid
          file Concepts/OrderId.cs

        concept Quantity : Int
          file Concepts/Quantity.cs

        concept Status : Enum
          file Concepts/Status.cs
          open
          closed
          onHold

        """.ReplaceLineEndings("\n"));

    [Fact] void should_report_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
}
