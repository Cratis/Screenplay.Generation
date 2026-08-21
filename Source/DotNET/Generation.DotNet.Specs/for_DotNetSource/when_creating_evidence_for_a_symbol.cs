// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSource;

public class when_creating_evidence_for_a_symbol : given.a_compilation
{
    Evidence _evidence = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/Banking/Account.cs",
            """
            namespace Banking;
            public record Account(System.Guid Id);
            """));
        var type = TypeNamed(compilation, "Banking.Account");

        _evidence = DotNetSource.EvidenceFor(
            type,
            new AdapterIdentity { Id = "marten", Version = "1.0.0" },
            EvidenceStrength.Exact,
            "/workspace",
            "The document is explicitly registered");
    }

    [Fact] void should_keep_the_adapter() => _evidence.Adapter.Id.ShouldEqual("marten");
    [Fact] void should_keep_the_strength() => _evidence.Strength.ShouldEqual(EvidenceStrength.Exact);
    [Fact] void should_make_the_path_relative() => _evidence.Source!.Path.ShouldEqual("Banking/Account.cs");
    [Fact] void should_use_one_based_positions() => _evidence.Source!.StartLine.ShouldEqual(2);
    [Fact] void should_keep_the_explanation() => _evidence.Explanation.ShouldEqual("The document is explicitly registered");
}
