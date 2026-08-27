// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAdapterRunner;

public class when_requiring_available_project_references : given.a_runner_context
{
    AdapterRunRecord _withReference = null!;
    AdapterRunRecord _withoutReferencedProject = null!;

    void Because()
    {
        var referencedCompilation = CSharpCompilation.Create("Referenced");
        var referencingCompilation = CSharpCompilation.Create(
            "Referencing",
            references: [referencedCompilation.ToMetadataReference()]);
        var referenced = Project("Referenced.Project", referencedCompilation);
        var referencing = Project("Referencing.Project", referencingCompilation);
        var descriptor = Descriptor(
            "project-reference",
            hostCapabilities: [AdapterHostCapability.ProjectReferences]);
        _withReference = DotNetAdapterRunner.Run(
            [DotNetAdapterRegistration.For(new ModernAdapter(descriptor))],
            new DotNetAnalysisContext([referencing, referenced]),
            Options).Adapters.Single();
        _withoutReferencedProject = DotNetAdapterRunner.Run(
            [DotNetAdapterRegistration.For(new ModernAdapter(descriptor))],
            new DotNetAnalysisContext([referencing]),
            Options).Adapters.Single();
    }

    [Fact] void should_run_when_the_referenced_source_project_is_available() => _withReference.Disposition.ShouldEqual(AdapterRunDisposition.Admitted);
    [Fact] void should_block_when_the_referenced_source_project_is_absent() => _withoutReferencedProject.Disposition.ShouldEqual(AdapterRunDisposition.Blocked);

    static DotNetProjectCompilation Project(string name, CSharpCompilation compilation) => new()
    {
        Name = name,
        Compilation = compilation,
        AuthoredSyntaxTrees = compilation.SyntaxTrees.ToHashSet()
    };
}
