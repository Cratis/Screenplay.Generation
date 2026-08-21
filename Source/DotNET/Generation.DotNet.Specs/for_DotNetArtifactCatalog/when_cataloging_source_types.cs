// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetArtifactCatalog;

public class when_cataloging_source_types : given.a_compilation
{
    DotNetArtifactCatalog _catalog = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/Banking/Events.cs",
            """
            namespace Banking;
            public record AccountOpened(System.Guid AccountId);
            public class Outer
            {
                public class Inner;
            }
            """));

        _catalog = new(compilation);
    }

    [Fact] void should_include_every_source_type() => _catalog.Types.Select(DotNetSubjectIds.MetadataName).ShouldContainOnly("Banking.AccountOpened", "Banking.Outer", "Banking.Outer+Inner");
    [Fact] void should_order_types_by_metadata_name() => _catalog.Types.Select(DotNetSubjectIds.MetadataName).ShouldEqual(["Banking.AccountOpened", "Banking.Outer", "Banking.Outer+Inner"]);
    [Fact] void should_not_include_referenced_framework_types() => _catalog.Types.ShouldNotContain(_ => _.ContainingAssembly.Name == "System.Runtime");
    [Fact] void should_create_a_stable_subject_id() => DotNetSubjectIds.ForType(_catalog.Types.Single(_ => _.Name == "Inner")).Value.ShouldEqual("dotnet://Banking/Banking.Outer+Inner");
}
