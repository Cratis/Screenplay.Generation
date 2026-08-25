// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceStructures.when_creating;

public class with_an_unknown_project_role : given.a_compilation
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
            Role = Enum.Parse<DotNetProjectRole>("Unknown"),
            Compilation = compilation,
            AuthoredSyntaxTrees = compilation.SyntaxTrees.ToHashSet()
        };

        _snapshot = DotNetSourceStructures.Create(new DotNetAnalysisContext([project]));
    }

    [Fact] void should_fail_closed() => _snapshot.IsSuccess.ShouldBeFalse();
    [Fact] void should_contribute_no_structure() => _snapshot.Structures.ShouldBeEmpty();
    [Fact] void should_report_the_project_role() => _snapshot.Diagnostics.Single().Code.ShouldEqual(DotNetSourceStructureDiagnosticCodes.UnsupportedProjectRole);
}
