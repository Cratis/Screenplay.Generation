// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.Vogen.for_VogenConceptScreenplayAdapter;

public class when_probing_without_safe_applicable_source_mapping : given.a_vogen_compilation
{
    AdapterProbeResult _applicable = null!;
    AdapterProbeResult _empty = null!;

    void Because()
    {
        var applicableCompilation = CompilationFrom(
            "Concepts",
            new SourceFile(
                "/workspace/Concepts/Code.cs",
                """
                namespace Concepts;
                [Vogen.ValueObject<string>]
                public partial struct Code;
                """));
        var emptyCompilation = CompilationFrom(
            "Empty",
            new SourceFile("/workspace/Empty/Code.cs", "namespace Empty; public partial struct Code;"));
        var adapter = new VogenConceptScreenplayAdapter();
        _applicable = adapter.Probe(new DotNetAnalysisContext([Project("Concepts.Project", applicableCompilation)]));
        _empty = adapter.Probe(new DotNetAnalysisContext([Project("Empty.Project", emptyCompilation)]));
    }

    [Fact] void should_block_applicable_vogen_source_without_stable_mapping() => _applicable.ShouldBeOfExactType<AdapterProbeBlocked>();
    [Fact] void should_report_the_stable_unsafe_mapping_diagnostic() => ((AdapterProbeBlocked)_applicable).Diagnostics.Single().Code.ShouldEqual(VogenGenerationDiagnosticCodes.UnsafeSourceMapping);
    [Fact] void should_remain_not_applicable_without_an_authored_vogen_declaration() => _empty.ShouldBeOfExactType<AdapterProbeNotApplicable>();
}
