// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetTypeUseFacts;

public class when_inheriting_a_cross_project_constructed_base : given.a_compilation
{
    IReadOnlyList<GenerationFact> _forward;
    IReadOnlyList<GenerationFact> _reversed;
    SubjectId _subject;

    void Because()
    {
        var library = CompilationFrom(new SourceFile("/workspace/Library.cs", "namespace Shared; public class Code; public class Base<T> { public T[] Values { get; } }")).WithAssemblyName("Library");
        using var stream = new MemoryStream();
        library.Emit(stream).Success.ShouldBeTrue();
        var application = CompilationFrom(new SourceFile("/workspace/Derived.cs", "public class Derived : Shared.Base<Shared.Code>;"))
            .WithAssemblyName("Application")
            .AddReferences(MetadataReference.CreateFromImage(stream.ToArray()));
        var libraryProject = new DotNetProjectCompilation { Name = "Library", Compilation = library, AuthoredSyntaxTrees = library.SyntaxTrees.ToHashSet() };
        var applicationProject = new DotNetProjectCompilation { Name = "Application", Compilation = application, AuthoredSyntaxTrees = application.SyntaxTrees.ToHashSet() };
        _subject = libraryProject.SubjectForType(TypeNamed(library, "Shared.Code"));
        var type = TypeNamed(application, "Derived");
        var artifact = new ArtifactKey { Subject = applicationProject.SubjectForType(type), Kind = ArtifactKind.Event };
        var evidence = new Evidence { Adapter = new AdapterIdentity { Id = "surface", Version = "1.0.0" }, Strength = EvidenceStrength.Exact };
        _forward = DotNetTypeUseFacts.EmitWithSurface(type, artifact, new DotNetAnalysisContext([libraryProject, applicationProject]), evidence, DotNetMemberSurfaceScope.Inherited);
        _reversed = DotNetTypeUseFacts.EmitWithSurface(type, artifact, new DotNetAnalysisContext([applicationProject, libraryProject]), evidence, DotNetMemberSurfaceScope.Inherited);
    }

    [Fact] void should_resolve_metadata_reference_origin_in_the_exact_analyzed_assembly() => _forward.OfType<ArtifactMemberDeclarationFact>().Single().Definition.Member.Name.ShouldEqual("values");
    [Fact] void should_retain_the_substituted_exact_library_type() => _forward.OfType<ArtifactMemberTypeUseFact>().Single().Definition.Type.ObservedTypeSubject.ShouldEqual(_subject);
    [Fact] void should_ignore_project_enumeration_order() => _reversed.Select(fact => fact.Id).ShouldEqual(_forward.Select(fact => fact.Id));
}
