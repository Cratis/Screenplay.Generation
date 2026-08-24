// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceStructures.when_creating;

public class without_a_source_context : given.a_compilation
{
    DotNetSourceStructureSnapshot _snapshot = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/physical-checkout/Banking/Source/Accounts/Register/Register.cs",
            "namespace Banking.Accounts.Register; public record Register;"));
        var project = new DotNetProjectCompilation
        {
            Name = "Banking",
            Compilation = compilation,
            AuthoredSyntaxTrees = compilation.SyntaxTrees.ToHashSet()
        };

        _snapshot = DotNetSourceStructures.Create(new DotNetAnalysisContext([project]));
    }

    [Fact] void should_fail_closed() => _snapshot.IsSuccess.ShouldBeFalse();
    [Fact] void should_contribute_no_structure() => _snapshot.Structures.ShouldBeEmpty();
    [Fact] void should_report_the_missing_context() => _snapshot.Diagnostics.Single().Code.ShouldEqual(DotNetSourceStructureDiagnosticCodes.MissingSourceContext);
    [Fact] void should_not_expose_the_physical_checkout() => _snapshot.Diagnostics.Single().Message.Contains("physical-checkout", StringComparison.Ordinal).ShouldBeFalse();
}
