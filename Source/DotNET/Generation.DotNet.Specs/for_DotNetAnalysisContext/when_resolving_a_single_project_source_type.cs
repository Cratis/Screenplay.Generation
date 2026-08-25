// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAnalysisContext;

public class when_resolving_a_single_project_source_type : given.project_reference_compilations
{
    SubjectId _expected = null!;
    SubjectId? _subject;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
                "/workspace/Single/SourceType.cs",
                "namespace Single; public record SourceType;"))
            .WithAssemblyName("Single.Project");
        var project = Project("Single", compilation, "/workspace/Single/Single.csproj");
        var type = TypeNamed(compilation, "Single.SourceType");
        _expected = project.SubjectForType(type);

        _subject = new DotNetAnalysisContext([project]).SubjectForType(type);
    }

    [Fact] void should_resolve_the_project_qualified_subject() => _subject.ShouldEqual(_expected);
}
