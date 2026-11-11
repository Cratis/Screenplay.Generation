// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_deriving_type_use_bindings_for_several_members : given.a_derivation
{
    string _forward = null!;
    string _reversed = null!;

    void Because()
    {
        var other = new SubjectId { Value = "dotnet://Ordering/Concepts.OtherCustomerCode" };
        GenerationFact[] facts =
        [
            CommandDeclaration(),
            MemberDeclaration("alpha", 0, "member-alpha"),
            MemberDeclaration("gamma", 2, "member-gamma"),
            MemberDeclaration("omega", 3, "member-omega"),
            Concept(ConceptSubject, "customer-code"),
            Concept(other, "other-code"),
            TypeUse("alpha", ConceptSubject, "use-alpha"),
            TypeUse("beta", ConceptSubject, "use-beta"),
            TypeUse("gamma", ConceptSubject, "use-gamma"),
            TypeUse("delta", ConceptSubject, "use-delta-a"),
            TypeUse("delta", other, "use-delta-b"),
            TypeUse("omega", ConceptSubject, "use-omega-a", TypeUseShapeKind.Named),
            TypeUse("omega", null, "use-omega-primitive")
        ];
        _forward = Render(DeriveDirectly(facts));
        _reversed = Render(DeriveDirectly([.. facts.Reverse()]));
    }

    [Fact] void should_render_the_fixed_ordered_outcome() => _forward.ShouldEqual(Expected);
    [Fact] void should_render_the_same_for_every_input_order() => _reversed.ShouldEqual(_forward);

    const string Expected = """
            inputs application:command, application:member-alpha, application:member-gamma, application:member-omega, application:use-alpha, application:use-beta, application:use-delta-a, application:use-delta-b, application:use-gamma, application:use-omega-a, application:use-omega-primitive, concepts:customer-code, concepts:other-code
            fact alpha -> dotnet://Ordering/Concepts.CustomerCode/Concept | dotnet://Ordering/Commands.RegisterCustomer | cratis.screenplay.type-use-binding 1.0.0
              inputs application:command, application:member-alpha, application:use-alpha, concepts:customer-code
              evidence application@Commands/RegisterCustomer.cs:1, application@Commands/RegisterCustomer.cs:2, application@Commands/RegisterCustomer.cs:10, concepts@Concepts/customer-code.cs:1
            fact gamma -> dotnet://Ordering/Concepts.CustomerCode/Concept | dotnet://Ordering/Commands.RegisterCustomer | cratis.screenplay.type-use-binding 1.0.0
              inputs application:command, application:member-gamma, application:use-gamma, concepts:customer-code
              evidence application@Commands/RegisterCustomer.cs:1, application@Commands/RegisterCustomer.cs:4, application@Commands/RegisterCustomer.cs:10, concepts@Concepts/customer-code.cs:1
            diagnostic GEN0044 | Error | Unknown | dotnet://Ordering/Commands.RegisterCustomer | Commands/RegisterCustomer.cs:1 | application:command, application:use-beta | Member 'beta' was not declared on artifact owner 'dotnet://Ordering/Commands.RegisterCustomer'. Input facts: 'application:command', 'application:use-beta'
            diagnostic GEN0046 | Error | Conflict | dotnet://Ordering/Commands.RegisterCustomer | Commands/RegisterCustomer.cs:10 | application:use-delta-a, application:use-delta-b | Incompatible exact type uses were asserted for member 'delta'. Input facts: 'application:use-delta-a', 'application:use-delta-b'
            diagnostic GEN0046 | Error | Conflict | dotnet://Ordering/Commands.RegisterCustomer | Commands/RegisterCustomer.cs:10 | application:use-omega-a, application:use-omega-primitive | Incompatible exact type uses were asserted for member 'omega'. Input facts: 'application:use-omega-a', 'application:use-omega-primitive'
            """;

    static (ImmutableArray<FactId> Inputs, ImmutableArray<GenerationFactRecord> Facts, ImmutableArray<GenerationDiagnostic> Diagnostics) DeriveDirectly(GenerationFact[] facts)
    {
        // TypeUseBindingDerivation is internal to the generation assembly; it is exercised directly, before the
        // derivation orchestrator sorts its output.
        var type = typeof(GenerationFactDerivation).Assembly.GetType("Cratis.Screenplay.Generation.TypeUseBindingDerivation")!;
        var records = ImmutableArray.CreateRange(facts.Select(_ => new GenerationFactRecord { Fact = _ }));
        var result = type.GetMethod("Derive")!.Invoke(null, [records])!;
        T Get<T>(string name) => (T)result.GetType().GetProperty(name)!.GetValue(result)!;
        return (Get<ImmutableArray<FactId>>("Inputs"), Get<ImmutableArray<GenerationFactRecord>>("Facts"), Get<ImmutableArray<GenerationDiagnostic>>("Diagnostics"));
    }

    static string Render((ImmutableArray<FactId> Inputs, ImmutableArray<GenerationFactRecord> Facts, ImmutableArray<GenerationDiagnostic> Diagnostics) result)
    {
        var lines = new List<string> { $"inputs {string.Join(", ", result.Inputs.Select(_ => _.Value))}" };
        foreach (var record in result.Facts)
        {
            var fact = (TypeUseBindingFact)record.Fact;
            lines.Add($"fact {fact.Definition.Member.Name} -> {fact.Definition.Target.Subject.Value}/{fact.Definition.Target.Kind} | {fact.Subject.Value} | {record.Lineage.Producer.Id} {record.Lineage.Producer.Version}");
            lines.Add($"  inputs {string.Join(", ", record.Lineage.Inputs.Select(_ => _.Value))}");
            lines.Add($"  evidence {string.Join(", ", record.Lineage.Evidence.Select(_ => $"{_.Adapter.Id}@{_.Source?.Path}:{_.Source?.StartLine}"))}");
        }

        foreach (var diagnostic in result.Diagnostics)
        {
            lines.Add($"diagnostic {diagnostic.Code} | {diagnostic.Severity} | {diagnostic.Outcome} | {diagnostic.Subject?.Value} | {diagnostic.Source?.Path}:{diagnostic.Source?.StartLine} | {string.Join(", ", diagnostic.Facts.Select(_ => _.Value))} | {diagnostic.Message}");
        }

        return string.Join('\n', lines);
    }
}
