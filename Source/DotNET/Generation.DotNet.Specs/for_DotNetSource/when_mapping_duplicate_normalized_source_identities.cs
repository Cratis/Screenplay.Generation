// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSource;

public class when_mapping_duplicate_normalized_source_identities : given.a_compilation
{
    Exception? _exception;

    void Because()
    {
        var compilation = CompilationFrom(
            new SourceFile("/checkout/Common/Café.cs", "namespace First; public record Order;"),
            new SourceFile("/checkout/common/cafe-combining.cs", "namespace Second; public record Order;"));
        var trees = compilation.SyntaxTrees.ToArray();

        _exception = Catch.Exception(() => DotNetSourcePaths.Create(
            "Orders",
            new DotNetSourcePathPolicy
            {
                DisplayRoot = DotNetSourceDisplayRoot.Project,
                CasePolicy = Enum.Parse<DotNetSourcePathCasePolicy>("InvariantLowercase")
            },
            [
                Document(trees[0], "Common/Café.cs"),
                Document(trees[1], "common/Cafe\u0301.cs")
            ]));
    }

    [Fact] void should_reject_identities_duplicated_after_folding_and_nfc_normalization() => _exception.ShouldBeOfExactType<DuplicateDotNetSourceIdentity>();

    static DotNetSourceDocument Document(SyntaxTree tree, string path) => new()
    {
        SyntaxTree = tree,
        ProjectRelativePath = path,
        WorkspaceRelativePath = path
    };
}
