// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAnalysisContext;

public class when_resolving_a_type_owned_by_multiple_projects : given.project_reference_compilations
{
    SubjectId? _firstOrder;
    SubjectId? _reversedOrder;

    void Because()
    {
        var firstCompilation = CompilationFrom(new SourceFile(
                "/first-checkout/Shared/Duplicate.cs",
                "namespace Shared; public record Duplicate;"))
            .WithAssemblyName("Shared.Assembly");
        var secondCompilation = CompilationFrom(new SourceFile(
                "/second-checkout/Shared/Duplicate.cs",
                "namespace Shared; public record Duplicate;"))
            .WithAssemblyName("Shared.Assembly");
        var first = Project("Shared.Project", firstCompilation, "/first-checkout/Shared/Shared.csproj");
        var second = Project("Shared.Project", secondCompilation, "/second-checkout/Shared/Shared.csproj");
        var type = TypeNamed(firstCompilation, "Shared.Duplicate");

        _firstOrder = new DotNetAnalysisContext([first, second]).SubjectForType(type);
        _reversedOrder = new DotNetAnalysisContext([second, first]).SubjectForType(type);
    }

    [Fact] void should_fail_closed_in_the_first_project_order() => _firstOrder.ShouldBeNull();
    [Fact] void should_fail_closed_in_the_reversed_project_order() => _reversedOrder.ShouldBeNull();
}
