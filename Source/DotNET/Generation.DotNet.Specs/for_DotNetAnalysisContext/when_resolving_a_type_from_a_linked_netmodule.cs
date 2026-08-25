// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAnalysisContext;

public class when_resolving_a_type_from_a_linked_netmodule : given.project_reference_compilations
{
    CSharpCompilation _rootCompilation = null!;
    INamedTypeSymbol _linkedType = null!;
    MetadataReference _moduleReference = null!;
    SubjectId? _subject;

    void Because()
    {
        var moduleCompilation = CompilationFrom(new SourceFile(
                "/workspace/Linked/LinkedType.cs",
                "namespace Linked.Contracts; public record LinkedType;"))
            .WithAssemblyName("Linked.Module")
            .WithOptions(new CSharpCompilationOptions(OutputKind.NetModule, nullableContextOptions: NullableContextOptions.Enable));
        using var moduleStream = new MemoryStream();
        moduleCompilation.Emit(moduleStream).Success.ShouldBeTrue();
        _moduleReference = MetadataReference.CreateFromImage(moduleStream.ToArray(), MetadataReferenceProperties.Module);
        _rootCompilation = CompilationFrom(new SourceFile(
                "/workspace/Root/UsesLinkedType.cs",
                "namespace Root; public record UsesLinkedType(Linked.Contracts.LinkedType Value);"))
            .WithAssemblyName("Root.Project")
            .AddReferences(_moduleReference);
        var project = Project("Root", _rootCompilation, "/workspace/Root/Root.csproj");
        _linkedType = TypeNamed(_rootCompilation, "Linked.Contracts.LinkedType");

        _subject = new DotNetAnalysisContext([project]).SubjectForType(_linkedType);
    }

    [Fact] void should_use_a_module_metadata_reference() => _moduleReference.Properties.Kind.ShouldEqual(MetadataImageKind.Module);
    [Fact] void should_report_the_root_assembly_as_the_containing_assembly() => SymbolEqualityComparer.Default.Equals(_linkedType.ContainingAssembly, _rootCompilation.Assembly).ShouldBeTrue();
    [Fact] void should_not_report_the_source_module_as_the_containing_module() => SymbolEqualityComparer.Default.Equals(_linkedType.ContainingModule, _rootCompilation.SourceModule).ShouldBeFalse();
    [Fact] void should_not_treat_the_root_project_as_the_owner() => _subject.ShouldBeNull();
}
