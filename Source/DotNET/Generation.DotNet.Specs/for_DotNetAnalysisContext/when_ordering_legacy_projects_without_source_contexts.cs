// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAnalysisContext;

public class when_ordering_legacy_projects_without_source_contexts : DotNet.given.a_compilation
{
    IReadOnlyList<string> _order = null!;

    void Because()
    {
        DotNetProjectCompilation[] projects =
        [
            Project("B", "Assembly", "z/project.csproj"),
            Project("A", "ZAssembly", "a/project.csproj"),
            Project("A", "Assembly", "z/project.csproj"),
            Project("A", "Assembly", "a/project.csproj")
        ];
        _order =
        [
            .. new DotNetAnalysisContext(projects.AsEnumerable().Reverse()).Projects
                .Select(project => $"{project.Name}:{project.Compilation.AssemblyName}:{project.ProjectPath}")
        ];
    }

    [Fact] void should_preserve_name_assembly_and_portable_relative_path_fallback_order() => string.Join('|', _order).ShouldEqual("A:Assembly:a/project.csproj|A:Assembly:z/project.csproj|A:ZAssembly:a/project.csproj|B:Assembly:z/project.csproj");

    static DotNetProjectCompilation Project(string name, string assembly, string path) => new()
    {
        Name = name,
        ProjectPath = path,
        Compilation = CSharpCompilation.Create(assembly),
        AuthoredSyntaxTrees = Enumerable.Empty<SyntaxTree>().ToHashSet()
    };
}
