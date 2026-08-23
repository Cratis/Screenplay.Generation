// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSource;

public class when_mapping_case_insensitive_source_identity : given.a_compilation
{
    DotNetSourceFile _file = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/checkout/Common/Café.cs",
            "namespace Banking; public record Café;"));
        var tree = compilation.SyntaxTrees.Single();
        var context = DotNetSourcePaths.Create(
            "Banking",
            new DotNetSourcePathPolicy
            {
                DisplayRoot = DotNetSourceDisplayRoot.Project,
                CasePolicy = DotNetSourcePathCasePolicy.InvariantLowercase
            },
            [
                new DotNetSourceDocument
                {
                    SyntaxTree = tree,
                    ProjectRelativePath = "Common/./Cafe\u0301.cs",
                    WorkspaceRelativePath = "apps/Banking/Common/Cafe\u0301.cs"
                }
            ]);

        _file = context.Files[tree];
    }

    [Fact] void should_normalize_identity_to_nfc_and_invariant_lower_case() => _file.Identity.Path.ShouldEqual("common/café.cs");
    [Fact] void should_preserve_normalized_display_casing() => _file.DisplayPath.ShouldEqual("Common/Café.cs");
}
