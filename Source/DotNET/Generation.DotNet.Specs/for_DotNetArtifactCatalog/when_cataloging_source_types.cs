// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetArtifactCatalog;

public class when_cataloging_source_types : given.a_compilation
{
    DotNetArtifactCatalog _catalog = null!;

    void Because()
    {
        var compilation = CompilationFrom(
            new SourceFile(
                "/workspace/Banking/Events.cs",
                """
                namespace Banking;
                public record AccountOpened(System.Guid AccountId);
                public partial class Account;
                public class Outer
                {
                    public class Inner;
                }
                """),
            new SourceFile(
                "/workspace/Banking/GeneratedOnly.g.cs",
                """
                namespace Banking;
                public partial class GeneratedOnly;
                public partial class Account;
                """));

        _catalog = new(compilation);
    }

    [Fact] void should_include_every_source_type() => _catalog.Types.Select(DotNetSubjectIds.MetadataName).ShouldContainOnly("Banking.Account", "Banking.AccountOpened", "Banking.GeneratedOnly", "Banking.Outer", "Banking.Outer+Inner");
    [Fact] void should_order_types_by_metadata_name() => _catalog.Types.Select(DotNetSubjectIds.MetadataName).ShouldEqual(["Banking.Account", "Banking.AccountOpened", "Banking.GeneratedOnly", "Banking.Outer", "Banking.Outer+Inner"]);
    [Fact] void should_not_include_referenced_framework_types() => _catalog.Types.ShouldNotContain(_ => _.ContainingAssembly.Name == "System.Runtime");
    [Fact] void should_only_include_types_with_authored_declarations_in_the_authored_catalog() => _catalog.AuthoredTypes.Select(DotNetSubjectIds.MetadataName).ShouldContainOnly("Banking.Account", "Banking.AccountOpened", "Banking.Outer", "Banking.Outer+Inner");
    [Fact] void should_qualify_a_subject_with_its_project() => DotNetSubjectIds.ForType(_catalog.Types.Single(_ => _.Name == "Inner"), "Banking.Project").Value.ShouldEqual("dotnet://Banking.Project/Banking/Banking.Outer+Inner");
}
