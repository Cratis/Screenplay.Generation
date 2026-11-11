// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_deriving_every_type_use_binding_outcome : given.a_derivation
{
    string _result = null!;

    void Because()
    {
        var other = new SubjectId { Value = "dotnet://Ordering/Concepts.OtherCustomerCode" };
        var readModel = new ArtifactKey { Subject = ConceptSubject, Kind = ArtifactKind.ReadModel };
        var legacy = new ArtifactFact
        {
            Id = new FactId { Value = "application:legacy" },
            Subject = CommandSubject,
            Evidence = CommandDeclaration().Evidence,
            Definition = new ArtifactDefinition
            {
                Key = Command,
                Name = "RegisterCustomer",
                File = "Commands/RegisterCustomer.cs",
                Properties = [LegacyProperty("customerCode", ArtifactKind.Concept)]
            }
        };
        var legacyConflict = legacy with
        {
            Id = new FactId { Value = "application:legacy-b" },
            Definition = legacy.Definition with
            {
                Properties = [LegacyProperty("customerCode", ArtifactKind.ReadModel)]
            }
        };
        var renamedDeclaration = CommandDeclaration("command-renamed") with
        {
            Definition = CommandDeclaration().Definition with { Name = "RegisterClient" }
        };
        var kindConflict = Concept(ConceptSubject, "target-kind") with
        {
            Definition = Concept(ConceptSubject, "target-kind").Definition with { Key = readModel }
        };

        var cases = new (string Name, GenerationFact[] Facts)[]
        {
            ("success", [CommandDeclaration(), MemberDeclaration("customerCode", 0, "member"), Concept(ConceptSubject, "customer-code"), TypeUse("customerCode", ConceptSubject, "use")]),
            ("legacy-target", [legacy, Concept(ConceptSubject, "customer-code"), kindConflict, TypeUse("customerCode", ConceptSubject, "use")]),
            ("legacy-conflict", [legacy, legacyConflict, Concept(ConceptSubject, "customer-code"), kindConflict, TypeUse("customerCode", ConceptSubject, "use")]),
            ("primitive", [CommandDeclaration(), MemberDeclaration("customerCode", 0, "member"), TypeUse("customerCode", null, "use")]),
            ("conflicting-type-use", [CommandDeclaration(), MemberDeclaration("customerCode", 0, "member"), Concept(ConceptSubject, "customer-code"), Concept(other, "other-code"), TypeUse("customerCode", ConceptSubject, "use-a"), TypeUse("customerCode", other, "use-b")]),
            ("missing-owner", [Concept(ConceptSubject, "target"), TypeUse("customerCode", ConceptSubject, "use")]),
            ("conflicting-owner", [CommandDeclaration(), renamedDeclaration, MemberDeclaration("customerCode", 0, "member"), Concept(ConceptSubject, "customer-code"), TypeUse("customerCode", ConceptSubject, "use")]),
            ("missing-member", [CommandDeclaration(), Concept(ConceptSubject, "target"), TypeUse("customerCode", ConceptSubject, "use")]),
            ("conflicting-order", [CommandDeclaration(), MemberDeclaration("customerCode", 0, "member-a"), MemberDeclaration("customerCode", 1, "member-b"), Concept(ConceptSubject, "customer-code"), TypeUse("customerCode", ConceptSubject, "use")]),
            ("missing-target", [CommandDeclaration(), MemberDeclaration("customerCode", 0, "member"), TypeUse("customerCode", ConceptSubject, "use")]),
            ("conflicting-target-kinds", [CommandDeclaration(), MemberDeclaration("customerCode", 0, "member"), Concept(ConceptSubject, "customer-code"), kindConflict, TypeUse("customerCode", ConceptSubject, "use")]),
            ("conflicting-target-declarations", [CommandDeclaration(), MemberDeclaration("customerCode", 0, "member"), Concept(ConceptSubject, "customer-code"), Concept(ConceptSubject, "customer-code-b", "OtherName"), TypeUse("customerCode", ConceptSubject, "use")])
        };
        _result = string.Join('\n', cases.Select(_ => Render(_.Name, Derive(_.Facts))));
    }

    [Fact] void should_render_the_fixed_outcome_of_every_case() => _result.ShouldEqual(Expected);

    const string Expected = """
            ## success
            fact generation:derive:type-use-binding:1:0064006F0074006E00650074003A002F002F004F00720064006500720069006E0067002F0043006F006D006D0061006E00640073002E005200650067006900730074006500720043007500730074006F006D00650072:3:0063007500730074006F006D006500720043006F00640065:0064006F0074006E00650074003A002F002F004F00720064006500720069006E0067002F0043006F006E00630065007000740073002E0043007500730074006F006D006500720043006F00640065:1 | dotnet://Ordering/Commands.RegisterCustomer | dotnet://Ordering/Commands.RegisterCustomer.customerCode -> dotnet://Ordering/Concepts.CustomerCode/Concept
              producer cratis.screenplay.type-use-binding 1.0.0
              inputs application:command, application:member, application:use, concepts:customer-code
              evidence application@Commands/RegisterCustomer.cs:1:1-1:20, application@Commands/RegisterCustomer.cs:2:1-2:20, application@Commands/RegisterCustomer.cs:10:1-10:20, concepts@Concepts/customer-code.cs:1:1-1:20
            ## legacy-target
            fact generation:derive:type-use-binding:1:0064006F0074006E00650074003A002F002F004F00720064006500720069006E0067002F0043006F006D006D0061006E00640073002E005200650067006900730074006500720043007500730074006F006D00650072:3:0063007500730074006F006D006500720043006F00640065:0064006F0074006E00650074003A002F002F004F00720064006500720069006E0067002F0043006F006E00630065007000740073002E0043007500730074006F006D006500720043006F00640065:1 | dotnet://Ordering/Commands.RegisterCustomer | dotnet://Ordering/Commands.RegisterCustomer.customerCode -> dotnet://Ordering/Concepts.CustomerCode/Concept
              producer cratis.screenplay.type-use-binding 1.0.0
              inputs application:legacy, application:use, concepts:customer-code
              evidence application@Commands/RegisterCustomer.cs:1:1-1:20, application@Commands/RegisterCustomer.cs:10:1-10:20, concepts@Concepts/customer-code.cs:1:1-1:20
            ## legacy-conflict
            diagnostic GEN0047 | Error | Conflict | dotnet://Ordering/Commands.RegisterCustomer | Commands/RegisterCustomer.cs:1:1-1:20 | application:legacy, application:legacy-b, application:use | Artifact member 'customerCode' retains incompatible exact legacy target roles. Input facts: 'application:legacy', 'application:legacy-b', 'application:use'
            ## primitive
            ## conflicting-type-use
            diagnostic GEN0046 | Error | Conflict | dotnet://Ordering/Commands.RegisterCustomer | Commands/RegisterCustomer.cs:10:1-10:20 | application:use-a, application:use-b | Incompatible exact type uses were asserted for member 'customerCode'. Input facts: 'application:use-a', 'application:use-b'
            ## missing-owner
            diagnostic GEN0043 | Error | Unknown | dotnet://Ordering/Commands.RegisterCustomer | Commands/RegisterCustomer.cs:10:1-10:20 | application:use | Artifact owner 'dotnet://Ordering/Commands.RegisterCustomer' was not declared for member 'customerCode'. Input facts: 'application:use'
            ## conflicting-owner
            diagnostic GEN0048 | Error | Conflict | dotnet://Ordering/Commands.RegisterCustomer | Commands/RegisterCustomer.cs:1:1-1:20 | application:command, application:command-renamed, application:use | Incompatible declarations were asserted for artifact owner 'dotnet://Ordering/Commands.RegisterCustomer'. Input facts: 'application:command', 'application:command-renamed', 'application:use'
            ## missing-member
            diagnostic GEN0044 | Error | Unknown | dotnet://Ordering/Commands.RegisterCustomer | Commands/RegisterCustomer.cs:1:1-1:20 | application:command, application:use | Member 'customerCode' was not declared on artifact owner 'dotnet://Ordering/Commands.RegisterCustomer'. Input facts: 'application:command', 'application:use'
            ## conflicting-order
            diagnostic GEN0048 | Error | Conflict | dotnet://Ordering/Commands.RegisterCustomer | Commands/RegisterCustomer.cs:2:1-2:20 | application:member-a, application:member-b, application:use | Incompatible declaration orders were asserted for member 'customerCode'. Input facts: 'application:member-a', 'application:member-b', 'application:use'
            ## missing-target
            diagnostic GEN0045 | Error | Unknown | dotnet://Ordering/Commands.RegisterCustomer | Commands/RegisterCustomer.cs:10:1-10:20 | application:use | Observed type subject 'dotnet://Ordering/Concepts.CustomerCode' has no declared artifact target. Input facts: 'application:use'
            ## conflicting-target-kinds
            diagnostic GEN0047 | Error | Conflict | dotnet://Ordering/Commands.RegisterCustomer | Commands/RegisterCustomer.cs:10:1-10:20 | application:use, concepts:customer-code, concepts:target-kind | Observed type subject 'dotnet://Ordering/Concepts.CustomerCode' declares incompatible artifact targets. Input facts: 'application:use', 'concepts:customer-code', 'concepts:target-kind'
            ## conflicting-target-declarations
            diagnostic GEN0048 | Error | Conflict | dotnet://Ordering/Commands.RegisterCustomer | Commands/RegisterCustomer.cs:10:1-10:20 | application:use, concepts:customer-code, concepts:customer-code-b | Observed type subject 'dotnet://Ordering/Concepts.CustomerCode' has incompatible declarations. Input facts: 'application:use', 'concepts:customer-code', 'concepts:customer-code-b'
            """;

    static PropertyDefinition LegacyProperty(string name, ArtifactKind kind) => new()
    {
        Name = name,
        Type = new TypeReferenceDefinition { Name = "CustomerCode", Subject = ConceptSubject, TargetArtifactKind = kind }
    };

    static string Render(string name, GenerationDerivationSnapshot snapshot)
    {
        var lines = new List<string> { $"## {name}" };
        foreach (var record in snapshot.Facts)
        {
            var fact = (TypeUseBindingFact)record.Fact;
            lines.Add($"fact {fact.Id.Value} | {fact.Subject.Value} | {fact.Definition.Member.Artifact.Subject.Value}.{fact.Definition.Member.Name} -> {fact.Definition.Target.Subject.Value}/{fact.Definition.Target.Kind}");
            lines.Add($"  producer {record.Lineage.Producer.Id} {record.Lineage.Producer.Version}");
            lines.Add($"  inputs {string.Join(", ", record.Lineage.Inputs.Select(_ => _.Value))}");
            lines.Add($"  evidence {string.Join(", ", record.Lineage.Evidence.Select(_ => $"{_.Adapter.Id}@{_.Source?.Path}:{_.Source?.StartLine}:{_.Source?.StartColumn}-{_.Source?.EndLine}:{_.Source?.EndColumn}"))}");
        }

        foreach (var diagnostic in snapshot.Diagnostics)
        {
            lines.Add($"diagnostic {diagnostic.Code} | {diagnostic.Severity} | {diagnostic.Outcome} | {diagnostic.Subject?.Value} | {diagnostic.Source?.Path}:{diagnostic.Source?.StartLine}:{diagnostic.Source?.StartColumn}-{diagnostic.Source?.EndLine}:{diagnostic.Source?.EndColumn} | {string.Join(", ", diagnostic.Facts.Select(_ => _.Value))} | {diagnostic.Message}");
        }

        return string.Join('\n', lines);
    }
}
