// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.Vogen.for_VogenConceptScreenplayAdapter;

public class when_discovering_authored_vogen_declarations : given.a_vogen_compilation
{
    AdapterContribution _contribution = null!;
    bool _canAnalyze;

    void Because()
    {
        var configuredCompilation = CompilationFrom(
            "Banking",
            new SourceFile(
                "/workspace/Banking/Concepts.cs",
                """
                [assembly: Vogen.VogenDefaults(underlyingType: typeof(string))]
                namespace Banking;
                [Vogen.ValueObject<System.Guid>]
                public readonly partial record struct OrderId;
                [Vogen.ValueObject(typeof(decimal))]
                public partial class Money;
                [Vogen.ValueObject]
                public partial struct CustomerCode;
                [Vogen.ValueObject<long>]
                public partial record ReferenceCode;
                """));
        var builtInCompilation = CompilationFrom(
            "Inventory",
            new SourceFile(
                "/workspace/Inventory/StockLevel.cs",
                """
                namespace Inventory;
                [Vogen.ValueObject]
                public readonly partial struct StockLevel;
                """));
        var context = new DotNetAnalysisContext(
        [
            Project("Inventory.Project", builtInCompilation),
            Project("Banking.Project", configuredCompilation)
        ]);
        var adapter = new VogenConceptScreenplayAdapter();
        _canAnalyze = adapter.CanAnalyze(context);
        _contribution = adapter.Analyze(context, new DotNetAdapterOptions());
    }

    [Fact] void should_recognize_the_analysis_context() => _canAnalyze.ShouldBeTrue();
    [Fact] void should_emit_one_concept_for_each_authored_declaration() => _contribution.Facts.OfType<ArtifactFact>().Select(_ => _.Definition.Name).ShouldContainOnly("CustomerCode", "Money", "OrderId", "ReferenceCode", "StockLevel");
    [Fact] void should_emit_only_concept_and_representation_facts() => _contribution.Facts.All(_ => _ is ArtifactFact or ConceptRepresentationFact).ShouldBeTrue();
    [Fact] void should_map_the_generic_record_struct() => RepresentationFor(_contribution, ConceptNamed(_contribution, "OrderId")).Definition.Primitive.ShouldEqual(GenerationPrimitiveKind.Uuid);
    [Fact] void should_map_the_non_generic_class() => RepresentationFor(_contribution, ConceptNamed(_contribution, "Money")).Definition.Primitive.ShouldEqual(GenerationPrimitiveKind.Number);
    [Fact] void should_map_the_generic_record_class() => RepresentationFor(_contribution, ConceptNamed(_contribution, "ReferenceCode")).Definition.Primitive.ShouldEqual(GenerationPrimitiveKind.WholeNumber);
    [Fact] void should_apply_the_assembly_default_to_the_struct() => RepresentationFor(_contribution, ConceptNamed(_contribution, "CustomerCode")).Definition.Primitive.ShouldEqual(GenerationPrimitiveKind.Text);
    [Fact] void should_apply_vogens_builtin_int_default() => RepresentationFor(_contribution, ConceptNamed(_contribution, "StockLevel")).Definition.Primitive.ShouldEqual(GenerationPrimitiveKind.WholeNumber);
    [Fact] void should_anchor_the_concept_at_the_authored_value_object_attribute() => ConceptNamed(_contribution, "CustomerCode").Evidence.Source!.StartLine.ShouldEqual(7);
    [Fact] void should_anchor_the_configured_representation_at_the_assembly_default() => RepresentationFor(_contribution, ConceptNamed(_contribution, "CustomerCode")).Evidence.Source!.StartLine.ShouldEqual(1);
    [Fact] void should_use_exact_evidence() => _contribution.Facts.All(_ => _.Evidence.Strength == EvidenceStrength.Exact && _.Evidence.Source is not null).ShouldBeTrue();
    [Fact] void should_use_the_exact_project_qualified_subject() => ConceptNamed(_contribution, "OrderId").Subject.Value.ShouldEqual("dotnet://Banking.Project/Banking/Banking.OrderId");
    [Fact] void should_preserve_the_authored_file() => ConceptNamed(_contribution, "OrderId").Definition.File.ShouldEqual("Banking/Concepts.cs");
    [Fact] void should_not_infer_identity() => _contribution.Facts.OfType<ArtifactFact>().SelectMany(_ => _.Definition.Properties).Any(_ => _.IsIdentifier).ShouldBeFalse();
    [Fact] void should_not_infer_validation_without_an_authored_hook() => _contribution.Facts.Count.ShouldEqual(10);
}
