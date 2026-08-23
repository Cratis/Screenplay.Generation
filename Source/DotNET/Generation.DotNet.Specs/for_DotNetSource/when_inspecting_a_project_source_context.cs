// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSource;

public class when_inspecting_a_project_source_context : given.a_compilation
{
    DotNetProjectSourceContext _context = null!;
    DotNetSourcePathPolicy _suppliedPolicy = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile("/checkout/Order.cs", "public record Order;"));
        var tree = compilation.SyntaxTrees.Single();
        _suppliedPolicy = new DotNetSourcePathPolicy
        {
            DisplayRoot = DotNetSourceDisplayRoot.Project,
            CasePolicy = DotNetSourcePathCasePolicy.Ordinal
        };
        _context = DotNetSourcePaths.Create(
            "Orders",
            _suppliedPolicy,
            [
                new DotNetSourceDocument
                {
                    SyntaxTree = tree,
                    ProjectRelativePath = "Order.cs",
                    WorkspaceRelativePath = "Order.cs"
                }
            ]);
    }

    [Fact] void should_only_be_constructible_by_the_factory() => typeof(DotNetProjectSourceContext).GetConstructors().ShouldBeEmpty();
    [Fact] void should_not_expose_property_replacement() => typeof(DotNetProjectSourceContext).GetProperties().All(_ => _.SetMethod is null).ShouldBeTrue();
    [Fact] void should_not_support_record_with_replacement() => typeof(DotNetProjectSourceContext).GetMethod("<Clone>$", BindingFlags.Instance | BindingFlags.NonPublic).ShouldBeNull();
    [Fact] void should_snapshot_the_policy() => ReferenceEquals(_suppliedPolicy, _context.Policy).ShouldBeFalse();
    [Fact] void should_expose_a_read_only_file_snapshot() => ((IDictionary)_context.Files).IsReadOnly.ShouldBeTrue();
    [Fact] void should_leave_source_identity_authority_out_of_the_project_compilation() => typeof(DotNetProjectCompilation).GetProperty("ProjectIdentity").ShouldBeNull();
}
