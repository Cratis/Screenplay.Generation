// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.Vogen.for_VogenConceptScreenplayAdapter;

public class when_source_only_looks_like_vogen : given.a_vogen_compilation
{
    AdapterContribution _contribution = null!;
    bool _canAnalyze_without_authored_vogen_evidence;

    void Because()
    {
        var compilation = CompilationFrom(
            "Concepts",
            new SourceFile(
                "/workspace/Concepts/Authored.cs",
                """
                namespace Fake
                {
                    public sealed class ValueObjectAttribute : System.Attribute;
                    public sealed class ValueObjectAttribute<T> : System.Attribute;
                }
                namespace Concepts
                {
                    [Fake.ValueObject]
                    public partial struct FakeNonGeneric;
                    [Fake.ValueObject<System.Guid>]
                    public partial struct FakeGeneric;
                    public partial struct CustomerId
                    {
                        public System.Guid Value { get; }
                        public static CustomerId From(System.Guid value) => default;
                    }
                    [Vogen.ValueObject<System.Guid>]
                    public struct NotPartial;
                    [Vogen.ValueObject<string>]
                    public partial struct RealCode;
                }
                """),
            new SourceFile(
                "/workspace/Concepts/GeneratedOnly.g.cs",
                """
                namespace Concepts;
                [Vogen.ValueObject<System.Guid>]
                public partial struct GeneratedOnly;
                """),
            new SourceFile(
                "/workspace/obj/Concepts/RealCode.g.cs",
                """
                namespace Concepts;
                [System.CodeDom.Compiler.GeneratedCode("Vogen", "8.0.7")]
                public partial struct RealCode
                {
                    public string Value { get; }
                    public static RealCode From(string value) => default;
                }
                """));
        var adapter = new VogenConceptScreenplayAdapter();
        var project = Project("Concepts.Project", compilation);
        _contribution = adapter.Analyze(new DotNetAnalysisContext([project]), new DotNetAdapterOptions());

        var lookalikeCompilation = CompilationFrom(
            "Lookalikes",
            new SourceFile(
                "/workspace/Lookalikes/Code.cs",
                """
                namespace Lookalikes;
                public partial struct CustomerId
                {
                    public System.Guid Value { get; }
                    public static CustomerId From(System.Guid value) => default;
                }
                [Vogen.ValueObject<System.Guid>]
                public struct NotPartial;
                """),
            new SourceFile(
                "/workspace/Lookalikes/Generated.g.cs",
                """
                namespace Lookalikes;
                [Vogen.ValueObject<System.Guid>]
                public partial struct Generated;
                """));
        _canAnalyze_without_authored_vogen_evidence = adapter.CanAnalyze(
            new DotNetAnalysisContext([Project("Lookalikes.Project", lookalikeCompilation)]));
    }

    [Fact] void should_only_discover_the_exact_authored_partial_declaration() => _contribution.Facts.OfType<ArtifactFact>().Select(_ => _.Definition.Name).ShouldContainOnly("RealCode");
    [Fact] void should_ignore_fake_short_name_attributes() => _contribution.Facts.OfType<ArtifactFact>().Any(_ => _.Definition.Name.StartsWith("Fake", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_ignore_generated_only_declarations() => _contribution.Facts.OfType<ArtifactFact>().Any(_ => _.Definition.Name == "GeneratedOnly").ShouldBeFalse();
    [Fact] void should_ignore_non_partial_declarations() => _contribution.Facts.OfType<ArtifactFact>().Any(_ => _.Definition.Name == "NotPartial").ShouldBeFalse();
    [Fact] void should_not_use_generated_members_or_names_as_primary_evidence() => _contribution.Facts.OfType<ArtifactFact>().Any(_ => _.Definition.Name == "CustomerId").ShouldBeFalse();
    [Fact] void should_anchor_the_real_concept_in_authored_source() => ConceptNamed(_contribution, "RealCode").Evidence.Source!.Path.ShouldEqual("Concepts/Authored.cs");
    [Fact] void should_not_recognize_a_context_with_lookalikes_only() => _canAnalyze_without_authored_vogen_evidence.ShouldBeFalse();
}
