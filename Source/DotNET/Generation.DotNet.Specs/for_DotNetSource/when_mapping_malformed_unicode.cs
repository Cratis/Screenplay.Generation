// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSource;

public class when_mapping_malformed_unicode : given.a_compilation
{
    Exception? _pathException;
    Exception? _projectException;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile("/checkout/Order.cs", "public record Order;"));
        var tree = compilation.SyntaxTrees.Single();
        var policy = new DotNetSourcePathPolicy
        {
            DisplayRoot = DotNetSourceDisplayRoot.Project,
            CasePolicy = Enum.Parse<DotNetSourcePathCasePolicy>("InvariantLowercase")
        };
        _pathException = Catch.Exception(() => DotNetSourcePaths.Create(
            "Orders",
            policy,
            [
                new DotNetSourceDocument
                {
                    SyntaxTree = tree,
                    ProjectRelativePath = "\ud800.cs",
                    WorkspaceRelativePath = "Order.cs"
                }
            ]));
        _projectException = Catch.Exception(() => DotNetSourcePaths.Create(
            "Orders\ud800",
            policy,
            [
                new DotNetSourceDocument
                {
                    SyntaxTree = tree,
                    ProjectRelativePath = "Order.cs",
                    WorkspaceRelativePath = "Order.cs"
                }
            ]));
    }

    [Fact] void should_report_a_typed_source_path_failure() => _pathException.ShouldBeOfExactType<InvalidDotNetSourcePath>();
    [Fact] void should_report_a_typed_project_identity_failure() => _projectException.ShouldBeOfExactType<InvalidDotNetProjectIdentity>();
}
